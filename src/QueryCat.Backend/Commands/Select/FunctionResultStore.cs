using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

/// <summary>
/// The store call the function and saves the arguments that were used to call.
/// If the same arguments would be provided again - the cached result will be returned.
/// </summary>
internal sealed class FunctionResultStore
{
    private readonly IFuncUnit _factory;
    private readonly Dictionary<VariantValueArray, VariantValue> _results = new();
    private bool _firstCall = true;

    private readonly FuncUnitCallInfo _functionCallInfo;
    private readonly VariantValue[] _functionCallInfoResults;

    /// <summary>
    /// Number of cache values.
    /// </summary>
    public int Count => _results.Count;

    public FunctionResultStore(
        IFuncUnit factory,
        FuncUnitCallInfo functionCallInfo)
    {
        _factory = factory;
        _functionCallInfo = functionCallInfo;

        _functionCallInfoResults = new VariantValueArray(size: functionCallInfo.Arguments.Length);
    }

    /// <summary>
    /// Get the current function argument values. Return the cached result or the new one.
    /// </summary>
    /// <param name="thread">Instance of <see cref="IExecutionThread" />.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="VariantValue" /> and cache mark.</returns>
    public async ValueTask<(VariantValue Value, bool Cached)> CallAsync(IExecutionThread thread, CancellationToken cancellationToken = default)
    {
        var args = await InvokeArgumentsDelegatesAsync(thread, cancellationToken);
        if (_results.TryGetValue(args, out var variantValue))
        {
            return (variantValue, true);
        }

        variantValue = await _factory.InvokeAsync(thread, cancellationToken);
        _results.Add(args, variantValue);
        return (variantValue, false);
    }

    /// <summary>
    /// Clear results cache.
    /// </summary>
    public void Clear()
    {
        _results.Clear();
    }

    private async ValueTask<VariantValueArray> InvokeArgumentsDelegatesAsync(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var args = _functionCallInfo.Arguments;
        for (var i = 0; i < _functionCallInfoResults.Length; i++)
        {
            if (!_firstCall
                && (_functionCallInfoResults[i].Type == DataType.Object || _functionCallInfoResults[i].Type == DataType.Dynamic))
            {
                continue;
            }
            _functionCallInfoResults[i] = await args[i].InvokeAsync(thread, cancellationToken);
        }
        _firstCall = false;

        var arr = new VariantValueArray(size: _functionCallInfo.Arguments.Length);
        Array.Copy(_functionCallInfoResults, arr, _functionCallInfoResults.Length);
        return arr;
    }

    /// <inheritdoc />
    public override string ToString() => $"Count = {Count}";
}
