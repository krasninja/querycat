using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using IFunction = QueryCat.Backend.Core.Functions.IFunction;

namespace QueryCat.Backend;

/// <summary>
/// The default static implementation of execution thread.
/// </summary>
public static class Executor
{
    private static readonly Lazy<IExecutionThread> _executionThreadLazy = new(Factory);

    /// <summary>
    /// Execution thread.
    /// </summary>
    public static IExecutionThread Thread => _executionThreadLazy.Value;

    private static IExecutionThread Factory()
    {
        return new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .Create();
    }
}
