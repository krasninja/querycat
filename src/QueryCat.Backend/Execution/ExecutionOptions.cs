using System.Reflection;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Execution;

/// <summary>
/// Execution options.
/// </summary>
public sealed class ExecutionOptions
{
    /// <summary>
    /// Default output if FROM is not specified.
    /// </summary>
    public IRowsOutput DefaultRowsOutput { get; set; } = NullRowsOutput.Instance;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PagingSize { get; set; } = 20;

    /// <summary>
    /// Add row number to output.
    /// </summary>
    public bool AddRowNumberColumn { get; set; } = true;

    public List<Assembly> PluginAssemblies { get; } = new();
}
