using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The class helps to create functions from different sources (delegates, types).
/// </summary>
public abstract class FunctionsFactory
{
    /// <summary>
    /// Create function from delegate.
    /// </summary>
    /// <param name="functionDelegate">Delegate.</param>
    /// <returns>Created functions.</returns>
    public abstract IFunction[] CreateFromDelegate(Delegate functionDelegate);

    /// <summary>
    /// Create function from signature and delegate.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    /// <param name="functionDelegate">Delegate to call.</param>
    /// <param name="description">Function description.</param>
    /// <param name="isSafe">Is it safe function.</param>
    /// <param name="formatters">Formatters.</param>
    /// <returns>Instance of <see cref="IFunction" />.</returns>
    public abstract IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        string? description = null,
        bool isSafe = false,
        string[]? formatters = null);

    /// <summary>
    /// Create aggregate function from type.
    /// </summary>
    /// <param name="aggregateType">Aggregate type.</param>
    /// <returns>Functions.</returns>
    public abstract IFunction[] CreateAggregateFromType(Type aggregateType);

    /// <summary>
    /// Create aggregate function from type.
    /// </summary>
    /// <typeparam name="TAggregate">Aggregate type.</typeparam>
    /// <returns>Functions.</returns>
    public IFunction[] CreateAggregateFromType<TAggregate>() where TAggregate : IAggregateFunction
        => CreateAggregateFromType(typeof(TAggregate));

    /// <summary>
    /// Register type methods as functions.
    /// </summary>
    /// <param name="type">Target type.</param>
    /// <returns>Functions.</returns>
    public IFunction[] CreateFromType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        var list = new List<IFunction>();

        // Try to register class as function.
        var classAttributes = Attribute.GetCustomAttributes(type, typeof(FunctionSignatureAttribute));
        if (classAttributes.Any())
        {
            foreach (var classAttribute in classAttributes)
            {
                var firstConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (firstConstructor != null)
                {
                    var functionName = GetFunctionName(((FunctionSignatureAttribute)classAttribute).Signature, type);
                    if (string.IsNullOrEmpty(functionName))
                    {
                        continue;
                    }
                    var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, firstConstructor.GetParameters(), type);
                    var @delegate = FunctionFormatter.CreateDelegateFromMethod(firstConstructor);
                    var description = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
                    list.Add(CreateFromSignature(signature, @delegate, description));
                }
            }
            return list.ToArray();
        }

        // Try to register aggregates from type.
        if (typeof(IAggregateFunction).IsAssignableFrom(type))
        {
            list.AddRange(CreateAggregateFromType(type));
            return list.ToArray();
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
            // The standard case: VariantValue FunctionName(IExecutionThread thread).
            if (methodParameters.Length == 1
                && methodParameters[0].ParameterType == typeof(IExecutionThread)
                && method.ReturnType == typeof(VariantValue))
            {
                var args = Expression.Parameter(typeof(IExecutionThread), "input");
                var func = Expression.Lambda<Func<IExecutionThread, VariantValue>>(Expression.Call(method, args), args)
                    .Compile();
                var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return [CreateFromSignature(methodSignature.Signature, func, description)];
            }
            // The async standard case: ValueTask<VariantValue> FunctionName(IExecutionThread thread, CancellationToken token).
            else if (methodParameters.Length == 2
                && methodParameters[0].ParameterType == typeof(IExecutionThread)
                && methodParameters[1].ParameterType == typeof(CancellationToken)
                && method.ReturnType == typeof(ValueTask<VariantValue>))
            {
                var args = Expression.Parameter(typeof(IExecutionThread), "input");
                var func = Expression.Lambda<Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>>>(
                        Expression.Call(method, args), args)
                    .Compile();
                var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description;
                return [CreateFromSignature(methodSignature.Signature, func, description)];
            }
            // Non-standard case. Construct signature from function definition.
            else
            {
                var function = CreateFunctionFromMethodInfo(method);
                if (function != null)
                {
                    list.Add(function);
                }
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Create function from <see cref="MethodInfo" />.
    /// </summary>
    /// <param name="method">Method.</param>
    /// <returns>Instance of <see cref="IFunction" />.</returns>
    protected IFunction? CreateFunctionFromMethodInfo(MethodInfo method)
    {
        var methodSignature = method.GetCustomAttributes<FunctionSignatureAttribute>().FirstOrDefault();
        if (methodSignature == null)
        {
            return null;
        }

        var description = method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        var functionName = GetFunctionName(methodSignature.Signature, method);
        var signature = FunctionFormatter.FormatSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
        var @delegate = FunctionFormatter.CreateDelegateFromMethod(method);
        return CreateFromSignature(signature, @delegate, description);
    }

    private static string GetFunctionName(string signature, MemberInfo memberInfo)
    {
        var functionName = GetFunctionName(signature);
        if (functionName.Length < 1)
        {
            functionName = FunctionFormatter.ToSnakeCase(memberInfo.Name);
        }
        return functionName.ToString();
    }

    private static ReadOnlySpan<char> GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.Ordinal);
        if (indexOfLeftParen < 0)
        {
            return signature;
        }
        return signature.AsSpan()[..indexOfLeftParen];
    }
}
