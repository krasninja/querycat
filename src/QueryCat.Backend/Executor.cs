using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend;

/// <summary>
/// The default static implementation of execution thread.
/// </summary>
public static class Executor
{
    private static IExecutionThread? _executionThread;
    private static readonly object _executionThreadLock = new();

    /// <summary>
    /// Execution thread.
    /// </summary>
    public static IExecutionThread Thread
    {
        get
        {
            if (_executionThread != null)
            {
                return _executionThread;
            }
            lock (_executionThreadLock)
            {
                _executionThread = AsyncUtils.RunSync(() => FactoryAsync())!;
            }
            return _executionThread;
        }
    }

    private static async Task<IExecutionThread> FactoryAsync(CancellationToken cancellationToken = default)
    {
        return await new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .WithStandardUriResolvers()
            .CreateAsync(cancellationToken);
    }
}
