using System.Diagnostics.CodeAnalysis;
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
    /// Create function from delegate.
    /// </summary>
    /// <param name="functionDelegate">Delegate.</param>
    /// <returns>Created functions.</returns>
    public IEnumerable<IFunction> CreateFromDelegate(
        Func<IExecutionThread, VariantValue> functionDelegate) => CreateFromDelegate((Delegate)functionDelegate);

    /// <summary>
    /// Create function from delegate.
    /// </summary>
    /// <param name="functionDelegate">Delegate.</param>
    /// <returns>Created functions.</returns>
    public IEnumerable<IFunction> CreateFromDelegate(
        Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>> functionDelegate) => CreateFromDelegate((Delegate)functionDelegate);

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
    /// <typeparam name="T">Target type.</typeparam>
    /// <returns>Functions.</returns>
    public IFunction[] CreateFromType<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods)] T
    >() => CreateFromType(typeof(T));

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
        if (classAttributes.Length > 0)
        {
            foreach (var classAttribute in classAttributes)
            {
                var firstConstructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();
                if (firstConstructor != null)
                {
                    var functionName = GetFunctionName(((FunctionSignatureAttribute)classAttribute).Signature, type);
                    if (functionName.Length < 1)
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
            var methodSignatures = method.GetCustomAttributes<FunctionSignatureAttribute>().ToArray();
            if (methodSignatures.Length == 0)
            {
                continue;
            }

            var methodParameters = method.GetParameters();
            Delegate? standardDelegate = null;
            // The standard case: VariantValue FunctionName(IExecutionThread thread).
            if (methodParameters.Length == 1
                && methodParameters[0].ParameterType == typeof(IExecutionThread)
                && method.ReturnType == typeof(VariantValue))
            {
                standardDelegate = method.CreateDelegate<Func<IExecutionThread, VariantValue>>();
            }
            // The async standard case: ValueTask<VariantValue> FunctionName(IExecutionThread thread, CancellationToken token).
            else if (methodParameters.Length == 2
                     && methodParameters[0].ParameterType == typeof(IExecutionThread)
                     && methodParameters[1].ParameterType == typeof(CancellationToken)
                     && method.ReturnType == typeof(ValueTask<VariantValue>))
            {
                standardDelegate = method.CreateDelegate<Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>>>();
            }

            // Non-standard case. Construct signature from function definition.
            if (standardDelegate == null)
            {
                list.AddRange(CreateFunctionsFromMethodInfo(method));
                continue;
            }

            var metadata = FunctionMetadata.CreateFromAttributes(method);
            foreach (var methodSignature in methodSignatures)
            {
                list.Add(CreateFromSignature(methodSignature.Signature, standardDelegate, metadata));
            }
        }

        return list.ToArray();
    }

    /// <summary>
    /// Create functions from <see cref="MethodInfo" />.
    /// </summary>
    /// <param name="method">Method.</param>
    /// <returns>Instances of <see cref="IFunction" />.</returns>
    protected IEnumerable<IFunction> CreateFunctionsFromMethodInfo(MethodInfo method)
    {
        var methodSignatures = method.GetCustomAttributes<FunctionSignatureAttribute>();
        var @delegate = CreateDelegateFromMethod(method);
        var metadata = FunctionMetadata.CreateFromAttributes(method);
        foreach (var methodSignature in methodSignatures)
        {
            var functionName = GetFunctionName(methodSignature.Signature, method);
            var signature = FunctionFormatter.GetSignatureFromParameters(functionName, method.GetParameters(), method.ReturnType);
            yield return CreateFromSignature(signature, @delegate, metadata);
        }
    }

    private static ReadOnlySpan<char> GetFunctionName(string signature, MemberInfo memberInfo)
    {
        var functionName = GetFunctionName(signature);
        if (functionName.Length < 1)
        {
            functionName = FunctionFormatter.ToSnakeCase(memberInfo.Name);
        }
        return functionName.Trim();
    }

    private static ReadOnlySpan<char> GetFunctionName(string signature)
    {
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.Ordinal);
        if (indexOfLeftParen < 0)
        {
            return signature;
        }
        return signature.AsSpan()[..indexOfLeftParen].Trim();
    }

    private static Delegate CreateDelegateFromMethod(MethodBase method)
    {
        var parameters = method.GetParameters();
        var resultUnwrapper = CreateResultUnwrapper(method);

        async ValueTask<VariantValue> FunctionDelegate(IExecutionThread thread, CancellationToken cancellationToken)
        {
            var arr = new object?[parameters.Length];
            var stackIndex = 0;
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
                else if (thread.Stack.FrameLength > stackIndex)
                {
                    arr[i] = Converter.ConvertValue(thread.Stack[stackIndex++], parameter.ParameterType);
                }
                else if (parameter.HasDefaultValue)
                {
                    arr[i] = parameter.DefaultValue;
                    stackIndex++;
                }
                else
                {
                    throw new InvalidOperationException(
                        string.Format(Resources.Errors.CannotSetParameterIndexFromMethod, i, method));
                }
            }
            var result = method is ConstructorInfo constructorInfo
                ? constructorInfo.Invoke(BindingFlags.DoNotWrapExceptions, binder: null, arr, culture: null)
                : method.Invoke(null, BindingFlags.DoNotWrapExceptions, binder: null, arr, culture: null);

            // If result is awaitable - wait and take the actual value.
            if (resultUnwrapper != null)
            {
                result = await resultUnwrapper.Invoke(result);
            }
            return VariantValue.CreateFromObject(result);
        }

        return FunctionDelegate;
    }

    /// <summary>
    /// Create the delegate that awaits the method result if it is <see cref="Task" />, <see cref="Task{TResult}" />,
    /// <see cref="ValueTask" /> or <see cref="ValueTask{TResult}" /> and returns the awaited value.
    /// </summary>
    /// <param name="method">Method.</param>
    /// <returns>Unwrap delegate or <c>null</c> if the method result is not awaitable.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2075",
        Justification = "Task<T>.Result and ValueTask<T>.AsTask are public members of the method return type.")]
    private static Func<object?, ValueTask<object?>>? CreateResultUnwrapper(MethodBase method)
    {
        // Constructors return the created instance.
        if (method is not MethodInfo methodInfo)
        {
            return null;
        }

        var returnType = methodInfo.ReturnType;
        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            return UnwrapVoidAsync;
        }
        // Fast path for the most common case, no reflection needed.
        if (returnType == typeof(ValueTask<VariantValue>))
        {
            return static async result => result is ValueTask<VariantValue> valueTask ? await valueTask : null;
        }
        if (!returnType.IsGenericType)
        {
            return null;
        }

        var genericTypeDefinition = returnType.GetGenericTypeDefinition();
        if (genericTypeDefinition == typeof(Task<>))
        {
            var resultProperty = returnType.GetProperty(nameof(Task<>.Result))!;
            return async result =>
            {
                if (result is not Task task)
                {
                    return null;
                }
                await task;
                return resultProperty.GetValue(task);
            };
        }
        if (genericTypeDefinition == typeof(ValueTask<>))
        {
            var asTaskMethod = returnType.GetMethod(nameof(ValueTask<>.AsTask), Type.EmptyTypes)!;
            var resultProperty = asTaskMethod.ReturnType.GetProperty(nameof(Task<>.Result))!;
            return async result =>
            {
                if (result == null)
                {
                    return null;
                }
                var task = (Task)asTaskMethod.Invoke(result, BindingFlags.DoNotWrapExceptions, binder: null, null, culture: null)!;
                await task;
                return resultProperty.GetValue(task);
            };
        }
        return null;

        static async ValueTask<object?> UnwrapVoidAsync(object? result)
        {
            if (result is Task task)
            {
                await task;
            }
            else if (result is ValueTask valueTask)
            {
                await valueTask;
            }
            return null;
        }
    }
}
