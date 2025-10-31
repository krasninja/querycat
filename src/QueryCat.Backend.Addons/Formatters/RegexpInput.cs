using System.Text.RegularExpressions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Regular expressions input parser.
/// </summary>
internal sealed class RegexpInput : StreamRowsInput
{
    private readonly Regex _regex;
    private int[] _targetColumnIndexes = [];
    private VariantValue[] _valuesArray = [];
    private int _virtualColumnsOffset = 0;

    /// <inheritdoc />
    public RegexpInput(Stream stream, string pattern, string? flags = null, string? key = null)
        : base(stream, new StreamRowsInputOptions
    {
        DelimiterStreamReaderOptions = new DelimiterStreamReader.ReaderOptions
        {
            DetectDelimiter = false,
            CompleteOnEndOfLine = true,
            Culture = Application.Culture,
        },
    }, key ?? string.Empty)
    {
        _regex = new Regex(pattern.Replace("\n", string.Empty), StringFunctions.FlagsToRegexOptions(flags));
    }

    /// <inheritdoc />
    protected override ErrorCode ReadValueInternal(int nonVirtualColumnIndex, DataType type, out VariantValue value)
    {
        value = _valuesArray[nonVirtualColumnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ReadNextInternalAsync(CancellationToken cancellationToken)
    {
        var hasData = await base.ReadNextInternalAsync(cancellationToken);
        if (!hasData)
        {
            return false;
        }

        var line = GetInputColumnValue(0);
        var match = _regex.Match(line.ToString());
        while (match.Success)
        {
            for (var i = 0; i < match.Groups.Count; i++)
            {
                var colIndex = _targetColumnIndexes[i];
                if (colIndex == -1)
                {
                    continue;
                }

                var columnType = Columns[colIndex + _virtualColumnsOffset].DataType;
                if (match.Groups[i].Success && VariantValue.TryCreateFromString(
                        match.Groups[i].ValueSpan, columnType, out var value))
                {
                    _valuesArray[colIndex] = value;
                }
                else
                {
                    _valuesArray[colIndex] = VariantValue.Null;
                }
            }
            match = match.NextMatch();
        }
        return true;
    }

    /// <inheritdoc />
    protected override Task<Column[]> InitializeColumnsAsync(IRowsInput input,
        CancellationToken cancellationToken = default)
    {
        var numbers = _regex.GetGroupNumbers().Select(gn => gn.ToString()).ToArray();
        var names = _regex.GetGroupNames();
        _targetColumnIndexes = new int[names.Length];
        var columns = new List<Column>(names.Length);
        Array.Fill(_targetColumnIndexes, -1);
        for (var i = 0; i < names.Length; i++)
        {
            // Select only named groups.
            if (names[i] == numbers[i])
            {
                continue;
            }

            columns.Add(new Column(names[i], DataType.String));
            _targetColumnIndexes[i] = columns.Count - 1;
        }
        _valuesArray = new VariantValue[columns.Count];
        _virtualColumnsOffset = this.GetVirtualColumns().Length;
        return Task.FromResult(columns.ToArray());
    }
}
