using System.Text.RegularExpressions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Addons.Formatters;

/// <summary>
/// Regular expressions input parser.
/// </summary>
internal sealed class RegexpInput : StreamRowsInput
{
    private readonly Regex _regex;
    private readonly VariantValue[] _valuesArray;
    private readonly int[] _targetColumnIndexes;

    /// <inheritdoc />
    public RegexpInput(Stream stream, string pattern, string? flags = null, string? key = null)
        : base(new StreamReader(stream), new StreamRowsInputOptions
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

        // Fill columns.
        var numbers = _regex.GetGroupNumbers().Select(gn => gn.ToString()).ToArray();
        var names = _regex.GetGroupNames();
        var columns = new List<Column>(names.Length);
        _targetColumnIndexes = new int[names.Length];
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
        SetColumns(columns);
        _valuesArray = new VariantValue[Columns.Length];
    }

    /// <inheritdoc />
    protected override void SetDefaultColumns(int columnsCount)
    {
        // Skip because we have already defined this in ctor.
    }

    /// <inheritdoc />
    protected override async Task AnalyzeAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        await RowsIteratorUtils.ResolveColumnsTypesAsync(iterator, cancellationToken: cancellationToken);
        iterator.SeekCacheCursorToHead();
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

                if (match.Groups[i].Success && VariantValue.TryCreateFromString(
                        match.Groups[i].ValueSpan, Columns[colIndex].DataType, out var value))
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
}
