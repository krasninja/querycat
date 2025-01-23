using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed partial class DefaultFunctionsManager : IFunctionsManager
{
    private readonly IAstBuilder _astBuilder;
    private readonly List<IUriResolver> _uriResolvers = new();

    private readonly record struct FunctionPreRegistration(
        Delegate Delegate,
        List<string> Signatures,
        string? Description = null);

    private readonly Dictionary<string, List<IFunction>> _functions = new();

    private readonly Dictionary<string, FunctionPreRegistration> _functionsPreRegistration = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultFunctionsManager));

    /// <inheritdoc />
    public FunctionsFactory Factory { get; }

    private static VariantValue EmptyFunction(IExecutionThread thread)
    {
        return VariantValue.Null;
    }

    internal DefaultFunctionsManager(IAstBuilder astBuilder, IEnumerable<IUriResolver>? uriResolvers = null)
    {
        _astBuilder = astBuilder;
        Factory = new DefaultFunctionsFactory(_astBuilder);
        if (uriResolvers != null)
        {
            _uriResolvers.AddRange(uriResolvers);
        }
    }

    #region Registration

    private string PreRegisterFunction(
        string signature,
        Delegate functionDelegate,
        string? functionName = null,
        string? description = null,
        string[]? formatterIds = null)
    {
        functionName ??= GetFunctionName(signature);

        if (_functionsPreRegistration.TryGetValue(functionName, out var preRegistration))
        {
            preRegistration.Signatures.Add(signature);
        }
        else
        {
            var signatures = new List<string> { signature };
            _functionsPreRegistration.Add(functionName,
                new FunctionPreRegistration(functionDelegate, signatures, description));
        }

        // Register as formatter.
        if (formatterIds != null)
        {
            foreach (var formatterId in formatterIds)
            {
                Formatters.FormattersInfo.RegisterFormatter(formatterId,
                    (fm, et, args) => fm.CallFunctionAsync(functionName, et, args));
            }
        }

        return functionName;
    }

    /// <inheritdoc />
    public void RegisterFunction(IFunction function)
    {
        var name = NormalizeName(function.Name);
        if (_functions.TryGetValue(name, out var functions))
        {
            WarnAboutSimilarFunctions(function, out _);
            functions.Add(function);
        }
        else
        {
            _functions.Add(name, [function]);
        }

        foreach (var formatterId in function.Formatters)
        {
            Formatters.FormattersInfo.RegisterFormatter(formatterId,
                (fm, et, args) => fm.CallFunctionAsync(name, et, args));
        }

        LogRegisterFunction(function);
    }

    private void WarnAboutSimilarFunctions(IFunction function, out List<IFunction>? functions)
    {
        if (_functions.TryGetValue(function.Name, out functions))
        {
            foreach (var sameNameFunction in functions)
            {
                if (sameNameFunction.IsSignatureEqual(function))
                {
                    _logger.LogWarning("Possibly similar signature function: {Function}.", function);
                }
            }
        }
    }

    /// <inheritdoc />
    public IFunction? ResolveUri(string uri)
    {
        foreach (var uriResolver in _uriResolvers)
        {
            if (uriResolver.TryResolve(uri, out var functionName)
                && !string.IsNullOrEmpty(functionName))
            {
                return this.FindByName(functionName);
            }
        }

        return null;
    }

    internal void AddUriResolver(IUriResolver uriResolver) => _uriResolvers.Add(uriResolver);

    #endregion

    /// <inheritdoc />
    public bool TryFindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes,
        out IFunction[] functions)
    {
        functions = [];
        name = NormalizeName(name);

        if (!_functions.TryGetValue(name, out var outFunctions))
        {
            if (!TryFindAggregateByName(name, out _))
            {
                return false;
            }
            if (!_functions.TryGetValue(name, out outFunctions))
            {
                return false;
            }
        }

        if (functionArgumentsTypes == null)
        {
            functions = outFunctions.ToArray();
            return true;
        }

        foreach (var func in outFunctions)
        {
            if (func.MatchesToArguments(functionArgumentsTypes))
            {
                functions = [func];
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
    {
        name = NormalizeName(name);
        if (_functions.TryGetValue(name, out var functions)
            && functions.Count > 0)
        {
            var value = (VariantValue)functions[0].Delegate.DynamicInvoke(NullExecutionThread.Instance)!;
            aggregateFunction = value.As<IAggregateFunction>();
            return true;
        }

        aggregateFunction = null;
        return false;
    }

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Functions enumerable.</returns>
    public IEnumerable<IFunction> GetFunctions()
    {
        foreach (var function in _functions.Values.SelectMany(f => f))
        {
            yield return function;
        }

        foreach (var functionItem in _functionsPreRegistration)
        {
            if (TryFindByName(functionItem.Key, null, out var functions))
            {
                foreach (var function in functions)
                {
                    yield return function;
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> CallFunctionAsync(
        IFunction function,
        IExecutionThread executionThread,
        FunctionCallArguments callArguments,
        CancellationToken cancellationToken = default)
    {
        var positionalIndex = 0;

        var frame = executionThread.Stack.CreateFrame();
        foreach (var argument in function.Arguments)
        {
            if (callArguments.Positional.Count >= positionalIndex + 1)
            {
                frame.Push(callArguments.Positional[positionalIndex++]);
                continue;
            }

            if (callArguments.Named.TryGetValue(argument.Name, out var value))
            {
                frame.Push(value);
            }
            else
            {
                frame.Push(argument.DefaultValue);
            }
        }

        var result = await FunctionCaller.CallAsync(function.Delegate, executionThread, cancellationToken);
        frame.Dispose();
        return result;
    }

    private static string NormalizeName(string target) => target.ToUpperInvariant();

    private static string GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.InvariantCulture);
        if (indexOfLeftParen < 0)
        {
            return NormalizeName(signature);
        }
        return NormalizeName(signature[..indexOfLeftParen]);
    }

    [LoggerMessage(LogLevel.Debug, "Register function: {Function}.")]
    private partial void LogRegisterFunction(IFunction function);

    [LoggerMessage(LogLevel.Debug, "Register aggregate: {Function}.")]
    private partial void LogRegisterAggregate(IFunction function);
}
