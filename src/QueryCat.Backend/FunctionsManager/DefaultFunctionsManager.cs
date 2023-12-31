using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed class DefaultFunctionsManager : IFunctionsManager
{
    private const int DefaultCapacity = 42;

    private readonly IAstBuilder _astBuilder;

    private record FunctionPreRegistration(
        FunctionDelegate Delegate,
        MemberInfo? MemberInfo,
        List<string> Signatures,
        string? Description = null);

    private readonly Dictionary<string, List<Function>> _functions = new(capacity: DefaultCapacity);
    private readonly Dictionary<string, IAggregateFunction> _aggregateFunctions = new(capacity: DefaultCapacity);

    private readonly Dictionary<string, FunctionPreRegistration> _functionsPreRegistration = new(capacity: DefaultCapacity);
    private readonly List<Action<DefaultFunctionsManager>> _registerFunctions = new(capacity: DefaultCapacity);
    private int _registerFunctionsLastIndex;

    private readonly List<Action<DefaultFunctionsManager>> _registerAggregateFunctions = new(capacity: DefaultCapacity);
    private int _registerAggregateFunctionsLastIndex;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultFunctionsManager));
    private static readonly ILogger Logger = Application.LoggerFactory.CreateLogger(nameof(DefaultFunctionsManager));

    private static VariantValue EmptyFunction(FunctionCallInfo args)
    {
        return VariantValue.Null;
    }

    internal DefaultFunctionsManager(IAstBuilder astBuilder)
    {
        _astBuilder = astBuilder;
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

    private void PreRegisterFunction(
        string signature,
        FunctionDelegate functionDelegate,
        string? functionName = null,
        string? description = null,
        MemberInfo? memberInfo = null)
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
                new FunctionPreRegistration(functionDelegate, memberInfo, signatures, description));
        }
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
    public void RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null)
    {
        if (string.IsNullOrEmpty(signature))
        {
            throw new ArgumentNullException(nameof(signature));
        }

        PreRegisterFunction(signature, @delegate, description: description);
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
            var descriptionAttribute = preRegistration.MemberInfo?.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                function.Description = descriptionAttribute.Description;
            }
            functionsList = AddFunctionInternal(function);
        }
        return functionsList ?? new List<Function>();
    }

    private List<Function> AddFunctionInternal(Function function)
    {
        var list = _functions!.AddOrUpdate(
            NormalizeName(function.Name),
            addValueFactory: _ => new List<Function>
            {
                function
            },
            updateValueFactory: (_, value) => value!.Add(function))!;
        _logger.LogDebug("Register function: {Function}.", function);
        return list;
    }

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
            var functionName = NormalizeName(function.Name);
            _functions!.AddOrUpdate(
                functionName,
                addValueFactory: _ => new List<Function>
                {
                    function
                },
                updateValueFactory: (_, value) => value!.Add(function));

            Logger.LogDebug("Register aggregate: {Function}.", function);
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
        functions = Array.Empty<IFunction>();
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
        var info = new FunctionCallInfo(executionThread, function.Name);
        int positionalIndex = 0;

        for (var i = 0; i < function.Arguments.Length; i++)
        {
            var argument = function.Arguments[i];

            if (callArguments.Positional.Count >= positionalIndex + 1)
            {
                info.Push(callArguments.Positional[positionalIndex++]);
                continue;
            }

            if (callArguments.Named.TryGetValue(argument.Name, out var value))
            {
                info.Push(value);
            }
            else
            {
                info.Push(argument.DefaultValue);
            }
        }

        return function.Delegate.Invoke(info);
    }

    private static string NormalizeName(string target) => target.ToUpper();

    private static string GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf("(", StringComparison.Ordinal);
        if (indexOfLeftParen < 0)
        {
            return NormalizeName(signature);
        }
        var name = NormalizeName(signature[..indexOfLeftParen]);
        if (name.StartsWith('['))
        {
            name = name.Substring(1, name.Length - 2);
        }
        return name;
    }
}
