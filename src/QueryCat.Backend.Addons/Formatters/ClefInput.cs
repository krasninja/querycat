using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Addons.Formatters;

internal sealed class ClefInput : JsonInput
{
    /// <inheritdoc />
    public ClefInput(StreamReader streamReader, bool addFileNameColumn = true, string? jsonPath = null, string? key = null)
        : base(streamReader, addFileNameColumn, jsonPath, key)
    {
    }

    /// <inheritdoc />
    protected override async Task AnalyzeAsync(CacheRowsIterator iterator, CancellationToken cancellationToken = default)
    {
        await base.AnalyzeAsync(iterator, cancellationToken);
        var newColumns = new List<Column>(capacity: Columns.Length);
        foreach (var column in Columns)
        {
            switch (column.Name)
            {
                case "@t":
                    newColumns.Add(new Column("timestamp", DataType.Timestamp, "An ISO 8601 timestamp."));
                    break;
                case "@m":
                    newColumns.Add(new Column("message", DataType.String, "A fully-rendered message describing the event."));
                    break;
                case "@mt":
                    newColumns.Add(new Column("message_template", DataType.String,
                        "Alternative to Message. Specifies a message template over the event’s properties that provides for rendering into a textual description of the event."));
                    break;
                case "@l":
                    newColumns.Add(new Column("level", column.DataType, "An implementation-specific level or severity identifier (string or number)."));
                    break;
                case "@x":
                    newColumns.Add(new Column("exception", DataType.String, "A language-dependent error representation potentially including backtrace."));
                    break;
                case "@i":
                    newColumns.Add(new Column("event_id", column.DataType, "An implementation specific event id, identifying the type of the event (string or number)."));
                    break;
                case "@r":
                    newColumns.Add(new Column("renderings", DataType.String,
                        "If @mt includes tokens with programming-language-specific formatting, an array of pre-rendered values for each such token."));
                    break;
                case "@tr":
                    newColumns.Add(new Column("trace_id", column.DataType, "The id of the trace that was active when the event was created, if any."));
                    break;
                case "@sp":
                    newColumns.Add(new Column("span_id", column.DataType, "The id of the span that was active when the event was created, if any."));
                    break;
                default:
                    newColumns.Add(column);
                    break;
            }
        }
        SetColumns(newColumns);
    }
}
