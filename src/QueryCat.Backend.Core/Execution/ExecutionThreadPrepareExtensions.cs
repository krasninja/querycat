using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Extensions for <see cref="IExecutionThreadPrepare" />.
/// </summary>
public static class ExecutionThreadPrepareExtensions
{
    public static Func<IDictionary<string, VariantValue>, CancellationToken, ValueTask<VariantValue>> PrepareWithScope(
        this IExecutionThreadPrepare executionThread,
        string query)
    {
        var prepareFunc = executionThread.Prepare(query);
        return async (parameters, ct) =>
        {
            try
            {
                var scope = executionThread.PushScope();
                foreach (var parameter in parameters)
                {
                    scope.Variables[parameter.Key] = parameter.Value;
                }
                return await prepareFunc.Invoke(ct);
            }
            finally
            {
                executionThread.PopScope();
            }
        };
    }
}
