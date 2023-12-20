using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Grok expressions input parser.
/// </summary>
internal sealed partial class GrokInput : IRowsInput
{
    // Sources:
    // https://www.elastic.co/guide/en/logstash/current/plugins-filters-grok.html
    // https://www.elastic.co/guide/en/logstash/8.11/plugins-filters-dissect.html
    // https://github.com/hpcugent/logstash-patterns/blob/master/files/grok-patterns

    [GeneratedRegex(@"%{(\w+):?(\w+)?:?(\w+)?}", RegexOptions.IgnoreCase)]
    private static partial Regex GrokPatternRegex();

    private readonly IRowsInput _grokImpl;

    private readonly Dictionary<string, string> _localPatterns = new(capacity: 24);

    private readonly Dictionary<string, DataType> _userTypesMap = new();
    private readonly Dictionary<string, string> _semanticPatternNameMap = new();
    private Func<VariantValue, VariantValue>?[] _converters = Array.Empty<Func<VariantValue, VariantValue>>();

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public string[] UniqueKey => _grokImpl.UniqueKey;

    /// <inheritdoc />
    public QueryContext QueryContext
    {
        get => _grokImpl.QueryContext;
        set
        {
            _grokImpl.QueryContext = value;
        }
    }

    public GrokInput(Stream stream, string pattern, string? key = null)
    {
        var regex = GrokPatternRegex().Replace(pattern, GrokEvaluation);
        _grokImpl = new RegexpInput(stream, regex, key);

        Columns = _grokImpl.Columns.Select(c => new Column(c)).ToArray();
    }

    private Func<VariantValue, VariantValue>?[] CreateCustomConverters()
    {
        var localConverters = new List<Func<VariantValue, VariantValue>?>();
        foreach (var column in Columns)
        {
            Func<VariantValue, VariantValue>? converter = null;

            // Set type based on user input pattern (like %{DATA:port:int}).
            // It is filled during grok pattern parsing.
            if (_userTypesMap.TryGetValue(column.Name, out var type))
            {
                column.DataType = type;
                converter = v => v.Cast(type);
            }

            // Determine type and converter for some standard
            // grok patterns.
            else if (_semanticPatternNameMap.TryGetValue(column.Name, out var patternName))
            {
                switch (patternName)
                {
                    case "INT":
                    case "MONTHNUM":
                    case "MONTHNUM2":
                    case "MONTHDAY":
                    case "YEAR":
                    case "HOUR":
                    case "MINUTE":
                    case "SECOND":
                    case "BASE16NUM":
                    case "POSINT":
                    case "NONNEGINT":
                        column.DataType = DataType.Integer;
                        converter = v => TryCastVariantValueOrNull(v, DataType.Integer);
                        break;
                    case "BASE16FLOAT":
                        column.DataType = DataType.Float;
                        converter = v => TryCastVariantValueOrNull(v, DataType.Float);
                        break;
                    case "DATE_EU":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "d.M.yyyy", "d/M/yyyy", "d-M-yyyy");
                        break;
                    case "DATE_US":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "M/d/yyyy", "M-d-yyyy");
                        break;
                    case "TIMESTAMP_ISO8601":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "yyyy-MM-ddTHH:mm:sszzzz");
                        break;
                    case "DATE":
                        column.DataType = DataType.Timestamp;
                        converter = v =>
                        {
                            var result = TryParseDateTimeOffsetOrNull(v.AsString,
                                "M/d/yyyy", "M-d-yyyy", "d.M.yyyy", "d/M/yyyy", "d-M-yyyy");
                            if (result.IsNull)
                            {
                                result = TryParseDateTimeOffsetOrNull(v.AsString);
                            }
                            return result;
                        };
                        break;
                    case "DATESTAMP_RFC2822":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "ddd, dd MMM yyyy HH:mm:ss zzz");
                        break;
                    case "HTTPDATE":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "d/MMMM/yyyy':'H':'m':'s zzz");
                        break;
                    case "DATESTAMP_EVENTLOG":
                        column.DataType = DataType.Timestamp;
                        converter = v => TryParseDateTimeOffsetOrNull(v.AsString, "yyyyMMddHHmmss");
                        break;
                    case "DATESTAMP":
                    case "DATESTAMP_RFC822":
                    case "DATESTAMP_OTHER":
                    case "HTTPDERROR_DATE":
                    case "SYSLOGTIMESTAMP":
                        // TODO:
                        break;
                }
            }

            localConverters.Add(converter);
        }

        return localConverters.ToArray();
    }

    private static VariantValue TryParseDateTimeOffsetOrNull(ReadOnlySpan<char> input, params string[] formats)
    {
        if (formats.Length > 0)
        {
            if (DateTimeOffset.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return new VariantValue(dt);
            }
        }
        else
        {
            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return new VariantValue(dt);
            }
        }

        return VariantValue.Null;
    }

    private static VariantValue TryCastVariantValueOrNull(in VariantValue input, DataType targetType)
    {
        if (input.TryCast(targetType, out var val))
        {
            return val;
        }
        return VariantValue.Null;
    }

    private static string FindGlobalPattern(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream("QueryCat.Backend.Formatters.grok-patterns");
        if (stream == null)
        {
            return string.Empty;
        }

        using var streamReader = new StreamReader(stream);

        while (streamReader.ReadLine() is { } line)
        {
            if (line.Length < 2 || line.StartsWith('#'))
            {
                continue;
            }

            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0)
            {
                continue;
            }
            if (line.Substring(0, spaceIndex) == name)
            {
                return line.Substring(spaceIndex + 1);
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Add custom pattern.
    /// </summary>
    /// <param name="semantic">Name.</param>
    /// <param name="pattern">Pattern.</param>
    public void AddPattern(string semantic, string pattern)
    {
        _localPatterns[pattern] = semantic;
    }

    private string FindPattern(string name)
    {
        name = name.ToUpper();
        if (_localPatterns.TryGetValue(name, out var pattern))
        {
            return pattern;
        }
        return FindGlobalPattern(name);
    }

    private string EvalPattern(string patternName)
    {
        var pattern = FindPattern(patternName);
        if (pattern == string.Empty)
        {
            throw new QueryCatException($"Cannot find pattern with name '{patternName}'.");
        }

        var previousEvalPattern = string.Empty;
        while (previousEvalPattern != pattern)
        {
            previousEvalPattern = pattern;
            pattern = GrokPatternRegex().Replace(pattern, match =>
            {
                var localPatternName = match.Groups[1].Value;
                return FindPattern(localPatternName);
            });
        }

        return pattern;
    }

    private string GrokEvaluation(Match match)
    {
        var patternName = match.Groups[1].Value;
        var semantic = match.Groups[2].Value;
        if (string.IsNullOrEmpty(semantic))
        {
            semantic = patternName;
            patternName = "DATA";
        }
        var type = match.Groups[3].ValueSpan;
        if (type.Length > 0)
        {
            AddColumnUserType(semantic, type.ToString());
        }
        var pattern = EvalPattern(patternName);
        _semanticPatternNameMap[semantic] = patternName;
        return semantic.Length > 0 ? $"(?<{semantic}>{pattern})" : $"(?<{patternName}>{pattern})";
    }

    private void AddColumnUserType(ReadOnlySpan<char> name, string type)
    {
        var dataType = type.ToLower() switch
        {
            "int" => DataType.Integer,
            "float" => DataType.Float,
            "datetime" => DataType.Timestamp,
            "str" => DataType.String,
            "string" => DataType.String,
            _ => throw new QueryCatException($"Invalid type '{type}'.")
        };
        _userTypesMap[name.ToString()] = dataType;
    }

    /// <inheritdoc />
    public void Open()
    {
        _converters = CreateCustomConverters();

        _grokImpl.Open();

        // Here is the hack. After original rows input open it tries to determine column types.
        // However, we do not want to do that for columns with converters. So we force them
        // to parse as strings.
        for (var i = 0; i < _converters.Length; i++)
        {
            if (_converters[i] != null)
            {
                _grokImpl.Columns[i].DataType = DataType.String;
            }
        }
        foreach (var column in _grokImpl.Columns)
        {
            if (_userTypesMap.TryGetValue(column.Name, out var type))
            {
                column.DataType = type;
            }
        }
    }

    /// <inheritdoc />
    public void Close() => _grokImpl.Close();

    /// <inheritdoc />
    public void Reset() => _grokImpl.Reset();

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var error = _grokImpl.ReadValue(columnIndex, out value);
        if (error != ErrorCode.OK)
        {
            return error;
        }

        var converter = _converters[columnIndex];
        if (converter != null)
        {
            value = converter.Invoke(value);
        }
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public bool ReadNext() => _grokImpl.ReadNext();

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Grok");
    }
}
