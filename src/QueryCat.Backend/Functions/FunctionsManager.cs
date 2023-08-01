using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Parser;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed class FunctionsManager
{
    private const int DefaultCapacity = 42;

    private record FunctionPreRegistration(FunctionDelegate Delegate, MemberInfo? MemberInfo, List<string> Signatures);

    private readonly Dictionary<string, List<Function>> _functions = new(capacity: DefaultCapacity);
    private readonly Dictionary<string, IAggregateFunction> _aggregateFunctions = new(capacity: DefaultCapacity);

    private readonly Dictionary<string, FunctionPreRegistration> _functionsPreRegistration = new(capacity: DefaultCapacity);
    private readonly List<Action<FunctionsManager>> _registerFunctions = new(capacity: DefaultCapacity);
    private int _registerFunctionsLastIndex;

    private readonly List<Action<FunctionsManager>> _registerAggregateFunctions = new(capacity: DefaultCapacity);
    private int _registerAggregateFunctionsLastIndex;

    private readonly ExecutionThread _thread;
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<FunctionsManager>();

    private static readonly ILogger Logger = Application.LoggerFactory.CreateLogger<FunctionsManager>();

    private static VariantValue EmptyFunction(FunctionCallInfo args)
    {
        return VariantValue.Null;
    }

    public FunctionsManager(ExecutionThread thread)
    {
        _thread = thread;
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

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    public void RegisterFromType<T>() => RegisterFromType(typeof(T));

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    /// <param name="type">Target type.</param>
    public void RegisterFromType(Type type)
    {
        string GetFunctionNameWithAlternate(string signature, MemberInfo memberInfo)
        {
            var functionName = GetFunctionName(signature);
            if (string.IsNullOrEmpty(functionName))
            {
                functionName = ToSnakeCase(memberInfo.Name);
            }
            return functionName;
        }

        _registerFunctions.Add(fm =>
        {
            // Try to register class as function.
            var classAttribute = type.GetCustomAttributes<FunctionSignatureAttribute>().FirstOrDefault();
            if (classAttribute != null)
            {
                var firstConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (firstConstructor != null)
                {
                    var functionName = GetFunctionNameWithAlternate(classAttribute.Signature, type);
                    var proxy = new MethodFunctionProxy(firstConstructor, functionName);
                    fm.RegisterFunctionFast(proxy.FunctionDelegate, type, proxy);
                    return;
                }
            }

            // Try to register aggregates from type.
            if (typeof(IAggregateFunction).IsAssignableFrom(type))
            {
                fm.RegisterAggregate(type);
                return;
            }

            // Try to register methods from type.
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                var methodSignature = method.GetCustomAttributes<FunctionSignatureAttribute>().FirstOrDefault();
                if (methodSignature == null)
                {
                    continue;
                }

                var methodParameters = method.GetParameters();
                if (methodParameters.Length == 1 && methodParameters[0].ParameterType == typeof(FunctionCallInfo)
                    && method.ReturnType == typeof(VariantValue))
                {
                    var args = Expression.Parameter(typeof(FunctionCallInfo), "input");
                    var func = Expression.Lambda<FunctionDelegate>(Expression.Call(method, args), args).Compile();
                    fm.RegisterFunctionFast(func, method);
                }
                else
                {
                    var functionName = GetFunctionNameWithAlternate(methodSignature.Signature, method);
                    var proxy = new MethodFunctionProxy(method, functionName);
                    fm.RegisterFunctionFast(proxy.FunctionDelegate, proxy.Method, proxy);
                }
            }
        });
    }

    /// <summary>
    /// Register the delegate that describe more functions.
    /// </summary>
    /// <param name="registerFunction">Register function delegate.</param>
    /// <param name="postpone">Postpone actual registration and add to pending list instead.</param>
    public void RegisterFactory(Action<FunctionsManager> registerFunction, bool postpone = true)
    {
        if (postpone)
        {
            _registerFunctions.Add(registerFunction);
        }
        else
        {
            registerFunction.Invoke(_thread.FunctionsManager);
        }
    }

    private void RegisterFunctionFast(FunctionDelegate functionDelegate, MemberInfo memberInfo, MethodFunctionProxy? proxy = null)
    {
        var signatureAttributes = memberInfo.GetCustomAttributes<FunctionSignatureAttribute>();
        if (signatureAttributes == null)
        {
            throw new QueryCatException($"Function {memberInfo.Name} must have {nameof(FunctionSignatureAttribute)}.");
        }

        foreach (var signatureAttribute in signatureAttributes)
        {
            var signature = signatureAttribute.Signature;
            // Convert "" -> "func" (method/class name as snake case).
            if (proxy != null)
            {
                if (string.IsNullOrEmpty(signature))
                {
                    signature = proxy.GetSignature().ToString();
                }
                // Convert "func" -> "func(a: integer): void (add signature from class/method definition).
                if (IsShortSignature(signature))
                {
                    signature = proxy.GetSignature(signature).ToString();
                }
            }
            if (string.IsNullOrEmpty(signature))
            {
                throw new InvalidOperationException("Empty function signature.");
            }

            var functionName = GetFunctionName(signature);
            if (string.IsNullOrEmpty(functionName) && proxy != null)
            {
                functionName = proxy.Name;
            }

            if (_functionsPreRegistration.TryGetValue(functionName, out var preRegistration))
            {
                preRegistration.Signatures.Add(signature);
            }
            else
            {
                var signatures = new List<string> { signature };
                _functionsPreRegistration.Add(functionName,
                    new FunctionPreRegistration(functionDelegate, memberInfo, signatures));
            }
        }
    }

    public void RegisterFunction(FunctionDelegate functionDelegate)
        => RegisterFunctionFast(functionDelegate, functionDelegate.GetMethodInfo());

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

    public Function RegisterFunction(string signature, FunctionDelegate @delegate,
        string? description = null)
    {
        var function = RegisterFunction(new FunctionPreRegistration(
            @delegate,
            null,
            new List<string>
            {
                signature
            })).First();
        if (!string.IsNullOrEmpty(description))
        {
            function.Description = description;
        }
        return function;
    }

    private List<Function> RegisterFunction(FunctionPreRegistration preRegistration)
    {
        List<Function>? functionsList = null;
        foreach (var signature in preRegistration.Signatures)
        {
            var signatureAst = AstBuilder.BuildFunctionSignatureFromString(signature);

            var function = new Function(preRegistration.Delegate, signatureAst);
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

            Logger.LogDebug("Register aggregate: {Function}.", function);
            functionsManager._aggregateFunctions.TryAdd(functionName, aggregateFunctionInstance);
        }
    }

    #endregion

    /// <summary>
    /// Tries to find the function by name and it arguments types.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="functionArgumentsTypes">Function arguments types.</param>
    /// <param name="functions">Found functions.</param>
    /// <returns>Returns <c>true</c> if functions were found, <c>false</c> otherwise.</returns>
    public bool TryFindByName(
        string name,
        FunctionArgumentsTypes? functionArgumentsTypes,
        out Function[] functions)
    {
        functions = Array.Empty<Function>();
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
        name = NormalizeName(name);
        if (TryFindByName(name, functionArgumentsTypes, out var functions))
        {
            if (functions.Length > 1 && functionArgumentsTypes != null)
            {
                throw new CannotFindFunctionException($"There is more than one signature for function '{name}'.");
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

    /// <summary>
    /// Call function with arguments.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="arguments">Call arguments.</param>
    /// <returns>Return result.</returns>
    public VariantValue CallFunction(string functionName, FunctionArguments? arguments = null)
    {
        arguments ??= new FunctionArguments();

        var function = FindByName(functionName, arguments.GetTypes());
        var info = new FunctionCallInfo(_thread);
        info.FunctionName = function.Name;
        int positionalIndex = 0;

        for (var i = 0; i < function.Arguments.Length; i++)
        {
            var argument = function.Arguments[i];

            if (arguments.Positional.Count >= positionalIndex + 1)
            {
                info.Push(arguments.Positional[positionalIndex++]);
                continue;
            }

            if (arguments.Named.TryGetValue(argument.Name, out var value))
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

    /// <summary>
    /// Call function with arguments.
    /// </summary>
    /// <param name="functionName">Function name.</param>
    /// <param name="arguments">Call arguments.</param>
    /// <typeparam name="T">Return type.</typeparam>
    /// <returns>Return result.</returns>
    public T CallFunction<T>(string functionName, FunctionArguments? arguments = null)
        => CallFunction(functionName, arguments).As<T>();

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

    private static string ToSnakeCase(string target)
    {
        // Based on https://stackoverflow.com/questions/63055621/how-to-convert-camel-case-to-snake-case-with-two-capitals-next-to-each-other.
        var sb = new StringBuilder()
            .Append(char.ToLower(target[0]));
        for (var i = 1; i < target.Length; ++i)
        {
            var ch = target[i];
            if (char.IsUpper(ch))
            {
                sb.Append('_');
                sb.Append(char.ToLower(ch));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    private static bool IsShortSignature(string signature)
        => signature.IndexOf("(", StringComparison.Ordinal) < 0;
}
