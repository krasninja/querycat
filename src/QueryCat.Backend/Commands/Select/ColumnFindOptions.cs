namespace QueryCat.Backend.Commands.Select;

[Flags]
internal enum ColumnFindOptions
{
    None = 0,

    /// <summary>
    /// Try to find within target list.
    /// </summary>
    IncludeRowsIterators = 1 << 1,

    /// <summary>
    /// Try to find in FROM clause.
    /// </summary>
    IncludeInputSources = 1 << 2,

    /// <summary>
    /// Try to find in CTE.
    /// </summary>
    IncludeCommonTableExpressions = 1 << 3,

    /// <summary>
    /// Default search option.
    /// </summary>
    Default = IncludeRowsIterators | IncludeInputSources | IncludeCommonTableExpressions,
}
