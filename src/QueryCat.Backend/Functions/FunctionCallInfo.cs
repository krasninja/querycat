using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Function call information: arguments values, execution scope.
/// </summary>
public sealed class FunctionCallInfo
{
    private readonly VariantValueArray _args;
    private int _argsCursor;
    private readonly VariantValueFunc[] _pushArgs;

    public static FunctionCallInfo Empty { get; } = new();

    /// <summary>
    /// The function manager of the current thread context.
    /// </summary>
    public FunctionsManager? FunctionsManager { get; internal set; }

    public VariantValueArray Arguments => _args;

    public static FunctionCallInfo CreateWithArguments(params VariantValue[] args)
    {
        var callInfo = new FunctionCallInfo();
        foreach (var arg in args)
        {
            callInfo.Push(arg);
        }
        return callInfo;
    }

    public static FunctionCallInfo CreateWithArguments(params object[] args)
    {
        var callInfo = new FunctionCallInfo();
        foreach (var arg in args)
        {
            callInfo.Push(VariantValue.CreateFromObject(arg));
        }
        return callInfo;
    }

    public FunctionCallInfo(params VariantValueFunc[] pushArgs)
    {
        _pushArgs = pushArgs;
        _args = new VariantValueArray(pushArgs.Length);
    }

    public void Push(VariantValue value)
    {
        _argsCursor++;
        if (_argsCursor > _args.Values.Length)
        {
            _args.Resize(_argsCursor);
        }
        _args.Values[_argsCursor - 1] = value;
    }

    public VariantValue GetAt(int position) => _args.Values[position];

    /// <summary>
    /// Clean current arguments stack.
    /// </summary>
    public void Reset() => _argsCursor = 0;

    public void InvokePushArgs(VariantValueFuncData variantValueFuncData)
    {
        for (int i = 0; i < _pushArgs.Length; i++)
        {
            _args.Values[i] = _pushArgs[i].Invoke(variantValueFuncData);
        }
    }

    /// <inheritdoc />
    public override string ToString() => _args.ToString();
}
