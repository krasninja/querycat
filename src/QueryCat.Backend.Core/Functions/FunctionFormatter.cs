using System.Reflection;
using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Functions;

internal static class FunctionFormatter
{
    public static string FormatSignatureFromParameters(string name, ParameterInfo[] parameterInfos, Type outputType)
    {
        var sb = new StringBuilder();
        sb.Append(name);
        sb.Append('(');
        for (var i = 0; i < parameterInfos.Length; i++)
        {
            sb.Append(ToSnakeCase(parameterInfos[i].Name ?? string.Empty))
                .Append(": ")
                .Append(GetTypeName(parameterInfos[i].ParameterType));
            if (i < parameterInfos.Length - 1)
            {
                sb.Append(", ");
            }
        }
        sb.Append("): ");
        sb.Append(GetTypeName(outputType));

        return sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        string GetObjectTypeFromName(Type objType)
        {
            if (objType.IsAssignableTo(typeof(IRowsInput)))
            {
                return nameof(IRowsInput);
            }
            if (objType.IsAssignableTo(typeof(IRowsOutput)))
            {
                return nameof(IRowsOutput);
            }
            if (objType.IsAssignableTo(typeof(IRowsIterator)))
            {
                return nameof(IRowsIterator);
            }
            if (objType.IsAssignableTo(typeof(IRowsFormatter)))
            {
                return nameof(IRowsFormatter);
            }
            return string.Empty;
        }

        // Unwrap generic types.
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type)!;
            }
            else if (type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                type = type.GetGenericArguments()[0];
            }
        }
        else if (type == typeof(Task))
        {
            type = typeof(void);
        }

        var dataType = Converter.ConvertFromSystem(type);
        if (dataType == DataType.Object)
        {
            var objectTypeName = GetObjectTypeFromName(type);
            return !string.IsNullOrEmpty(objectTypeName) ? $"{dataType}<{objectTypeName}>" : dataType.ToString();
        }
        return dataType.ToString();
    }

    public static string ToSnakeCase(string target)
    {
        // Based on https://stackoverflow.com/questions/63055621/how-to-convert-camel-case-to-snake-case-with-two-capitals-next-to-each-other.
        var sb = new StringBuilder(capacity: target.Length)
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

    internal static Delegate CreateDelegateFromMethod(MethodBase method)
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
                    arr[i] = CancellationToken.None;
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
}
