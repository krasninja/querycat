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
    public abstract IEnumerable<IFunction> CreateFromDelegate(Delegate functionDelegate);

    /// <summary>
    /// Create function from signature and delegate.
    /// </summary>
    /// <param name="signature">Function signature.</param>
    /// <param name="functionDelegate">Delegate to call.</param>
    /// <param name="functionMetadata">Additional function information.</param>
    /// <returns>Instance of <see cref="IFunction" />.</returns>
    public abstract IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        FunctionMetadata? functionMetadata = null);

    /// <summary>
    /// Create aggregate function from type.
    /// </summary>
    /// <typeparam name="TAggregate">Aggregate type.</typeparam>
    /// <returns>Functions.</returns>
    public abstract IEnumerable<IFunction> CreateAggregateFromType<TAggregate>()
        where TAggregate : IAggregateFunction;

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
                    var signature = FunctionFormatter.GetSignatureFromParameters(functionName, firstConstructor.GetParameters(), type);
                    var @delegate = CreateDelegateFromMethod(firstConstructor);
                    var metadata = FunctionMetadata.CreateFromAttributes(type);
                    list.Add(CreateFromSignature(signature, @delegate, metadata));
                }
            }
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
                var metadata = FunctionMetadata.CreateFromAttributes(type);
                return [CreateFromSignature(methodSignature.Signature, func, metadata)];
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
                var metadata = FunctionMetadata.CreateFromAttributes(type);
                return [CreateFromSignature(methodSignature.Signature, func, metadata)];
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

        var functionName = GetFunctionName(methodSignature.Signature, method);
        var signature = FunctionFormatter.GetSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
        var @delegate = CreateDelegateFromMethod(method);
        var metadata = FunctionMetadata.CreateFromAttributes(method);
        return CreateFromSignature(signature, @delegate, metadata);
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

    private static Delegate CreateDelegateFromMethod(MethodBase method)
    {
        async ValueTask<VariantValue> FunctionDelegate(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var parameters = method.GetParameters();
            var arr = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (typeof(IExecutionThread).IsAssignableFrom(parameter.ParameterType))
                {
                    arr[i] = thread;
                }
                else if (parameter.ParameterType == typeof(CancellationToken))
                {
                    arr[i] = cancellationToken;
                }
                else if (thread.Stack.FrameLength > i)
                {
                    arr[i] = Converter.ConvertValue(thread.Stack[i], parameter.ParameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    arr[i] = parameter.DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(Resources.Errors.CannotSetParameterIndexFromMethod, i, method));
                }
            }
            var result = method is ConstructorInfo constructorInfo
                ? constructorInfo.Invoke(arr)
                : method.Invoke(null, arr);

            // If result is awaitable - try to wait.
            if (result is Task task)
            {
                await task;
                result = GetResultFromTask(method, task);
            }
            else if (result is ValueTask valueTask)
            {
                var valueTaskResolved = valueTask.AsTask();
                await valueTaskResolved;
                result = GetResultFromTask(method, valueTaskResolved);
            }
            return VariantValue.CreateFromObject(result);
        }

        return FunctionDelegate;
    }

    private static object? GetResultFromTask(MethodBase method, object task)
    {
        if (method is MethodInfo methodInfo
            && methodInfo.ReturnType.IsGenericType)
        {
            var resultProperty = task.GetType().GetProperty("Result");
            if (resultProperty != null)
            {
                return resultProperty.GetValue(task);
            }
        }
        return VariantValue.Null;
    }
}
