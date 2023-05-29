using System.ComponentModel;
using System.Reflection;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Functions;

/// <summary>
/// The proxy between native .NET method (<see cref="MethodInfo" />) and QueryCat function.
/// </summary>
internal class MethodFunctionProxy
{
    private readonly MethodBase _method;
    private readonly string _name;

    public MethodBase Method => _method;

    public string Name => _name;

    public MethodFunctionProxy(MethodBase method, string? name = null)
    {
        _method = method;
        _name = name ?? method.Name;
    }

    public FunctionSignatureNode GetSignature(string? nameOverride = null)
    {
        var @params = _method.GetParameters();
        var paramsList = new List<FunctionSignatureArgumentNode>(@params.Length);
        foreach (var parameterInfo in @params)
        {
            var argNode = new FunctionSignatureArgumentNode(
                parameterInfo.Name ?? "p",
                new FunctionTypeNode(Converter.ConvertFromSystem(parameterInfo.ParameterType)),
                parameterInfo.HasDefaultValue ? VariantValue.CreateFromObject(parameterInfo.DefaultValue!) : null);
            paramsList.Add(argNode);
        }
        var returnType = DataType.Void;
        var returnTypeName = string.Empty;
        if (_method is MethodInfo methodInfo)
        {
            returnType = Converter.ConvertFromSystem(methodInfo.ReturnType);
            if (returnType == DataType.Object)
            {
                returnTypeName = GetTypeFromName(methodInfo.ReturnType);
            }
        }
        if (_method is ConstructorInfo constructorInfo)
        {
            returnType = DataType.Object;
            if (constructorInfo.DeclaringType != null)
            {
                returnTypeName = GetTypeFromName(constructorInfo.DeclaringType);
            }
        }

        var node = new FunctionSignatureNode(nameOverride ?? _name, new FunctionTypeNode(returnType, returnTypeName), paramsList);
        return node;
    }

    private static string GetTypeFromName(Type type)
    {
        if (type.IsAssignableTo(typeof(IRowsInput)))
        {
            return nameof(IRowsInput);
        }
        if (type.IsAssignableTo(typeof(IRowsOutput)))
        {
            return nameof(IRowsOutput);
        }
        if (type.IsAssignableTo(typeof(IRowsIterator)))
        {
            return nameof(IRowsIterator);
        }
        if (type.IsAssignableTo(typeof(IRowsFormatter)))
        {
            return nameof(IRowsFormatter);
        }
        return string.Empty;
    }

    public Function GetFunction()
    {
        var descriptionAttribute = _method.GetCustomAttribute<DescriptionAttribute>();
        var description = descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
        var signatureNode = GetSignature();
        return new Function(FunctionDelegate, signatureNode)
        {
            Description = description,
        };
    }

    public VariantValue FunctionDelegate(FunctionCallInfo args)
    {
        var parameters = _method.GetParameters();
        var arr = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            if (parameter.ParameterType == typeof(FunctionCallInfo))
            {
                arr[i] = args;
            }
            else if (parameter.ParameterType == typeof(ExecutionContext))
            {
                arr[i] = args.ExecutionThread;
            }
            else if (parameter.ParameterType == typeof(CancellationToken))
            {
                arr[i] = CancellationToken.None;
            }
            else if (args.Arguments.Values.Length > i)
            {
                arr[i] = Converter.ConvertValue(args.GetAt(i), parameter.ParameterType);
            }
            else if (parameter.HasDefaultValue)
            {
                arr[i] = parameter.DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Cannot set parameter index {i} for method '{_method}'.");
            }
        }
        var result = _method is ConstructorInfo constructorInfo
            ? constructorInfo.Invoke(arr)
            : _method.Invoke(null, arr);

        // If result is awaitable - try to wait.
        if (result is Task task)
        {
            AsyncUtils.RunSync(async () => await task);
            if (_method is MethodInfo methodInfo
                && methodInfo.ReturnType.IsGenericType)
            {
                result = ((dynamic)task).Result;
            }
            else
            {
                result = VariantValue.Null;
            }
        }
        return VariantValue.CreateFromObject(result);
    }
}
