using System.Collections.Generic;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Plugins.Client;

internal sealed class ThriftPluginStatistic : ExecutionStatistic
{
    private readonly List<RowErrorInfo> _errorRows = new();

    /// <inheritdoc />
    public override IReadOnlyList<RowErrorInfo> Errors => _errorRows;

    /// <inheritdoc />
    public override void AddError(in RowErrorInfo info)
    {
        _errorRows.Add(info);
    }

    /// <inheritdoc />
    public override string Dump() => string.Empty;
}