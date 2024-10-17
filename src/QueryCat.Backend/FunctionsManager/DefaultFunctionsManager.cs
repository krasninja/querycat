using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed partial class DefaultFunctionsManager : IFunctionsManager
{
    private readonly IAstBuilder _astBuilder;
    private readonly List<IUriResolver> _uriResolvers = new();

    private readonly record struct FunctionPreRegistration(
        FunctionDelegate Delegate,
        List<string> Signatures,
        string? Description = null);

    private readonly Dictionary<string, List<Function>> _functions = new();
    private readonly Dictionary<string, IAggregateFunction> _aggregateFunctions = new();

    private readonly Dictionary<string, FunctionPreRegistration> _functionsPreRegistration = new();
    private readonly List<Action<DefaultFunctionsManager>> _registerFunctions = [];
    private int _registerFunctionsLastIndex;

    private readonly List<Action<DefaultFunctionsManager>> _registerAggregateFunctions = [];
    private int _registerAggregateFunctionsLastIndex;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultFunctionsManager));

    private static VariantValue EmptyFunction(IExecutionThread thread)
    {
        return VariantValue.Null;
    }

    internal DefaultFunctionsManager(IAstBuilder astBuilder, IEnumerable<IUriResolver>? uriResolvers = null)
    {
        _astBuilder = astBuilder;
        if (uriResolvers != null)
        {
            _uriResolvers.AddRange(uriResolvers);
        }
    }

    #region Registration

    /// <inheritdoc />
    public void RegisterFactory(Action<IFunctionsManager> registerFunction, bool postpone = true)
    {
        if (postpone)
        {
            _registerFunctions.Add(registerFunction);
        }
        else
        {
            registerFunction.Invoke(this);
        }
    }

    private string PreRegisterFunction(
        string signature,
        FunctionDelegate functionDelegate,
        string? functionName = null,
        string? description = null)
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

        return functionName;
    }

    private bool TryGetPreRegistration(string name, out List<Function> functions)
    {
        if (_functionsPreRegistration.Remove(name, out var functionInfo))
        {
            functions = RegisterFunction(functionInfo);
            return true;
        }

        // Execute function actions and try to find it again.
        while (_registerFunctionsLastIndex < _registerFunctions.Count)
        {
            _registerFunctions[_registerFunctionsLastIndex++].Invoke(this);
            if (_functionsPreRegistration.Remove(name, out functionInfo))
            {
                functions = RegisterFunction(functionInfo);
                return true;
            }
        }

        functions = new List<Function>();
        return false;
    }

    /// <inheritdoc />
    public string RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(signature, nameof(signature));
        return PreRegisterFunction(signature, @delegate, description: description);
    }

    private List<Function> RegisterFunction(FunctionPreRegistration preRegistration)
    {
        List<Function>? functionsList = null;
        foreach (var signature in preRegistration.Signatures)
        {
            var signatureAst = _astBuilder.BuildFunctionSignatureFromString(signature);

            var function = new Function(preRegistration.Delegate, signatureAst)
            {
                Description = preRegistration.Description ?? string.Empty,
            };
            if (_functions.TryGetValue(NormalizeName(function.Name), out var sameNameFunctionsList))
            {
                var similarFunction = sameNameFunctionsList.Find(f => f.IsSignatureEquals(function));
                if (similarFunction != null)
                {
                    _logger.LogWarning("Possibly similar signature function: {Function}.", function);
                }
            }
            var memberInfo = preRegistration.Delegate.Method;
            var descriptionAttribute = memberInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                function.Description = descriptionAttribute.Description;
            }
            var safeAttribute = memberInfo.GetCustomAttribute<SafeFunctionAttribute>();
            if (safeAttribute != null)
            {
                function.IsSafe = true;
            }
            functionsList = AddFunctionInternal(function);
        }
        return functionsList ?? new List<Function>();
    }

    private List<Function> AddFunctionInternal(Function function)
    {
        var list = _functions!.AddOrUpdate(
            NormalizeName(function.Name),
            addValueFactory: _ => [function],
            updateValueFactory: (_, value) => value!.Add(function))!;
        LogRegisterFunction(function);

        return list;
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

    /// <inheritdoc />
    public void RegisterAggregate<TAggregate>(Func<TAggregate> factory)
        where TAggregate : IAggregateFunction
    {
        _registerAggregateFunctions.Add(_ => RegisterAggregateInternal(factory));
    }

    private void RegisterAggregateInternal<TAggregate>(
        Func<TAggregate> factory)
        where TAggregate : IAggregateFunction
    {
        var aggregateType = typeof(TAggregate);
        var signatureAttributes = aggregateType.GetCustomAttributes<AggregateFunctionSignatureAttribute>();
        foreach (var signatureAttribute in signatureAttributes)
        {
            var signatureAst = _astBuilder.BuildFunctionSignatureFromString(signatureAttribute.Signature);
            var function = new Function(EmptyFunction, signatureAst, aggregate: true);
            var descriptionAttribute = aggregateType.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                function.Description = descriptionAttribute.Description;
            }
            var safeAttribute = aggregateType.GetCustomAttribute<SafeFunctionAttribute>();
            if (safeAttribute != null)
            {
                function.IsSafe = true;
            }
            var functionName = NormalizeName(function.Name);
            _functions!.AddOrUpdate(
                functionName,
                addValueFactory: _ => [function],
                updateValueFactory: (_, value) => value!.Add(function));

            LogRegisterAggregate(function);
            var aggregateFunctionInstance = factory.Invoke();
            _aggregateFunctions.TryAdd(functionName, aggregateFunctionInstance);
        }
    }

    #endregion

    /// <inheritdoc />
    public bool TryFindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes,
        out IFunction[] functions)
    {
        functions = [];
        name = NormalizeName(name);

        if (!_functions.TryGetValue(name, out var outFunctions) && !TryGetPreRegistration(name, out outFunctions))
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
    public bool TryFindAggregateByName(string name, out IAggregateFunction aggregateFunction)
    {
        name = NormalizeName(name);
        if (_aggregateFunctions.TryGetValue(name, out aggregateFunction!))
        {
            return true;
        }

        while (_registerAggregateFunctionsLastIndex < _registerAggregateFunctions.Count)
        {
            _registerAggregateFunctions[_registerAggregateFunctionsLastIndex++].Invoke(this);
            if (_aggregateFunctions.TryGetValue(name, out aggregateFunction!))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Functions enumerable.</returns>
    public IEnumerable<IFunction> GetFunctions()
    {
        while (_registerFunctionsLastIndex < _registerFunctions.Count)
        {
            _registerFunctions[_registerFunctionsLastIndex++].Invoke(this);
        }

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
    public VariantValue CallFunction(IFunction function, IExecutionThread executionThread, FunctionCallArguments callArguments)
    {
        int positionalIndex = 0;

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

        var result = function.Delegate.Invoke(executionThread);
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
    private partial void LogRegisterFunction(Function function);

    [LoggerMessage(LogLevel.Debug, "Register aggregate: {Function}.")]
    private partial void LogRegisterAggregate(Function function);
}
