using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Parser;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed class FunctionsManager
{
    public delegate VariantValue FunctionDelegate(FunctionCallInfo args);

    private record struct FunctionPreRegistration(FunctionDelegate Delegate, MethodInfo MethodInfo);

    private readonly Dictionary<string, List<Function>> _functions = new(capacity: 42);
    private readonly Dictionary<string, IAggregateFunction> _aggregateFunctions = new(capacity: 42);

    private readonly Dictionary<string, FunctionPreRegistration> _functionsPreRegistration = new(capacity: 42);
    private readonly List<Action<FunctionsManager>> _registerFunctions = new(capacity: 42);
    private int _registerFunctionsLastIndex;

    private readonly List<Action<FunctionsManager>> _registerAggregateFunctions = new(capacity: 42);
    private int _registerAggregateFunctionsLastIndex;

    private static Function EmptyAggregate => new(
        _ =>
        {
            Logger.Instance.Warning("Empty aggregate function is not intended to be called!");
            return VariantValue.Null;
        }, new FunctionSignatureNode("Empty", DataType.Null));

    private static VariantValue EmptyFunction(FunctionCallInfo args)
    {
        return VariantValue.Null;
    }

    /// <summary>
    /// Call application function with the specific arguments.
    /// </summary>
    /// <param name="functionDelegate">Function delegate.</param>
    /// <param name="args">Arguments.</param>
    /// <returns>Result.</returns>
    public static VariantValue Call(FunctionDelegate functionDelegate, params object[] args)
    {
        var callInfo = FunctionCallInfo.CreateWithArguments(ExecutionThread.Empty, args);
        return functionDelegate.Invoke(callInfo);
    }

    #region Registration

    public void RegisterFunctionsFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterFromAssembly(assembly);
        }
    }

    public void RegisterFromAssembly(Assembly assembly)
    {
        // If there is class Registration with RegisterFunctions method - call it instead. Use reflection otherwise.
        var registerType = assembly.GetType(assembly.GetName().Name + ".Registration");
        if (registerType != null)
        {
            var registerMethod = registerType.GetMethod("RegisterFunctions");
            if (registerMethod != null)
            {
                _registerFunctions.Add(fm =>
                {
                    registerMethod.Invoke(null, new object?[] { fm });
                });
            }
        }
        else
        {
            foreach (var type in assembly.GetTypes())
            {
                RegisterFromType(type);
            }
        }
    }

    /// <summary>
    /// Try to find and register functions from types.
    /// </summary>
    /// <param name="types">Types to analyze.</param>
    public void RegisterFromTypes(params Type[] types)
    {
        foreach (var type in types)
        {
            RegisterFromType(type);
        }
    }

    public void RegisterFromType<T>() => RegisterFromType(typeof(T));

    public void RegisterFromType(Type type)
    {
        _registerFunctions.Add(fm =>
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                if (!method.GetCustomAttributes<FunctionSignatureAttribute>().Any())
                {
                    continue;
                }

                var args = Expression.Parameter(typeof(FunctionCallInfo), "input");
                var func = Expression.Lambda<FunctionDelegate>(Expression.Call(method, args), args).Compile();
                fm.RegisterFunctionFast(func, method);
            }

            if (typeof(IAggregateFunction).IsAssignableFrom(type))
            {
                fm.RegisterAggregate(type);
            }
        });
    }

    /// <summary>
    /// Register the delegate that describe more functions.
    /// </summary>
    /// <param name="registerFunction">Register function delegate.</param>
    public void RegisterWithFunction(Action<FunctionsManager> registerFunction) => _registerFunctions.Add(registerFunction);

    private void RegisterFunctionFast(FunctionDelegate functionDelegate, MethodInfo methodInfo)
    {
        var signatureAttributes = methodInfo.GetCustomAttributes<FunctionSignatureAttribute>();
        if (signatureAttributes == null)
        {
            throw new QueryCatException($"Function {methodInfo.Name} must have {nameof(FunctionSignatureAttribute)}.");
        }

        foreach (var signatureAttribute in signatureAttributes)
        {
            var indexOfLeftParen = signatureAttribute.Signature.IndexOf("(", StringComparison.Ordinal);
            if (indexOfLeftParen < 0)
            {
                Logger.Instance.Warning($"Incorrect signature: {signatureAttribute.Signature}.");
                return;
            }
            var functionName = NormalizeName(signatureAttribute.Signature[..indexOfLeftParen]);
            if (functionName.StartsWith('['))
            {
                functionName = functionName.Substring(1, functionName.Length - 2);
            }
            if (!_functionsPreRegistration.ContainsKey(functionName))
            {
                _functionsPreRegistration.Add(functionName, new FunctionPreRegistration(functionDelegate, methodInfo));
            }
        }
    }

    public void RegisterFunction(FunctionDelegate functionDelegate)
        => RegisterFunctionFast(functionDelegate, functionDelegate.GetMethodInfo());

    private bool TryGetPreRegistration(string name, out List<Function> functions)
    {
        if (_functionsPreRegistration.TryGetValue(name, out var functionInfo))
        {
            functions = RegisterFunctionInternal(functionInfo.Delegate, functionInfo.MethodInfo);
            return true;
        }

        while (_registerFunctionsLastIndex < _registerFunctions.Count)
        {
            _registerFunctions[_registerFunctionsLastIndex++].Invoke(this);
            if (_functionsPreRegistration.TryGetValue(name, out functionInfo))
            {
                functions = RegisterFunctionInternal(functionInfo.Delegate, functionInfo.MethodInfo);
                return true;
            }
        }

        functions = new List<Function>();
        return false;
    }

    private List<Function> RegisterFunctionInternal(FunctionDelegate functionDelegate, MethodInfo methodInfo)
    {
        var signatureAttributes = methodInfo.GetCustomAttributes<FunctionSignatureAttribute>().ToList();
        if (!signatureAttributes.Any())
        {
            throw new QueryCatException($"Function {methodInfo.Name} must have {nameof(FunctionSignatureAttribute)}.");
        }

        List<Function>? functionsList = null;
        foreach (var signatureAttribute in signatureAttributes)
        {
            var signatureAst = AstBuilder.BuildFunctionSignatureFromString(signatureAttribute.Signature);

            var function = new Function(functionDelegate, signatureAst);
            if (_functions.TryGetValue(NormalizeName(function.Name), out var sameNameFunctionsList))
            {
                var similarFunction = sameNameFunctionsList.Find(f => f.IsSignatureEquals(function));
                if (similarFunction != null)
                {
                    Logger.Instance.Warning($"Possibly similar signature function: {function}.");
                }
            }
            var descriptionAttribute = methodInfo.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                function.Description = descriptionAttribute.Description;
            }
            functionsList = _functions!.AddOrUpdate(
                NormalizeName(function.Name),
                addValueFactory: _ => new List<Function>
                {
                    function
                },
                updateValueFactory: (_, value) => value!.Add(function));
            if (Logger.Instance.IsEnabled(LogLevel.Trace))
            {
                Logger.Instance.Debug($"Register function: {function}.");
            }
        }
        return functionsList ?? new List<Function>();
    }

    public void RegisterAggregate<T>() where T : IAggregateFunction
        => RegisterAggregate(typeof(T));

    public void RegisterAggregate(Type aggregateType)
    {
        _registerAggregateFunctions.Add(fm => RegisterAggregateInternal(fm, aggregateType));
    }

    private static void RegisterAggregateInternal(FunctionsManager functionsManager, Type aggregateType)
    {
        if (Activator.CreateInstance(aggregateType) is not IAggregateFunction aggregateFunctionInstance)
        {
            throw new InvalidOperationException(
                $"Type '{aggregateType.Name}' is not assignable from '{nameof(IAggregateFunction)}.");
        }

        var signatureAttributes = aggregateType.GetCustomAttributes<AggregateFunctionSignatureAttribute>();
        foreach (var signatureAttribute in signatureAttributes)
        {
            var signatureAst = AstBuilder.BuildFunctionSignatureFromString(signatureAttribute.Signature);
            var function = new Function(EmptyFunction, signatureAst, aggregate: true);
            var descriptionAttribute = aggregateType.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute != null)
            {
                function.Description = descriptionAttribute.Description;
            }
            var functionName = NormalizeName(function.Name);
            functionsManager._functions!.AddOrUpdate(
                functionName,
                addValueFactory: _ => new List<Function>
                {
                    function
                },
                updateValueFactory: (_, value) => value!.Add(function));
            if (Logger.Instance.IsEnabled(LogLevel.Trace))
            {
                Logger.Instance.Debug($"Register aggregate: {function}.");
            }

            if (!functionsManager._aggregateFunctions.ContainsKey(functionName))
            {
                functionsManager._aggregateFunctions.Add(functionName, aggregateFunctionInstance);
            }
        }
    }

    #endregion

    public bool TryFindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes,
        out Function[] functions)
    {
        functions = Array.Empty<Function>();
        name = NormalizeName(name);

        if (!_functions.TryGetValue(name, out var outFunctions))
        {
            if (!TryGetPreRegistration(name, out outFunctions))
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
        }

        if (functionArgumentsTypes == null)
        {
            functions = outFunctions!.ToArray();
            return true;
        }

        foreach (var func in outFunctions!)
        {
            if (func.MatchesToArguments(functionArgumentsTypes))
            {
                functions = new[] { func };
                return true;
            }
        }

        return false;
    }

    public Function FindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes = null)
    {
        if (TryFindByName(name, functionArgumentsTypes, out var functions))
        {
            if (functions.Length > 1 && functionArgumentsTypes != null)
            {
                throw new CannotFindFunctionException($"There are more than one signatures for function '{name}'.");
            }
            return functions.First();
        }
        if (functionArgumentsTypes != null)
        {
            throw new CannotFindFunctionException(name, functionArgumentsTypes);
        }
        throw new CannotFindFunctionException(name);
    }

    private bool TryFindAggregateByName(string name, out IAggregateFunction aggregateFunction)
    {
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

    public IAggregateFunction FindAggregateByName(string name)
    {
        name = NormalizeName(name);
        if (TryFindAggregateByName(name, out var aggregateFunction))
        {
            return aggregateFunction;
        }

        throw new CannotFindFunctionException(name);
    }

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Functions enumerable.</returns>
    public IEnumerable<Function> GetFunctions()
    {
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

    private static string NormalizeName(string target) => target.ToUpper();
}
