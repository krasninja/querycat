using System.ComponentModel;
using System.Reflection;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Types;

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
                new FunctionTypeNode(DataTypeUtils.ConvertFromSystem(parameterInfo.ParameterType)),
                parameterInfo.HasDefaultValue ? VariantValue.CreateFromObject(parameterInfo.DefaultValue!) : null);
            paramsList.Add(argNode);
        }
        var returnType = DataType.Void;
        if (_method is MethodInfo methodInfo)
        {
            returnType = DataTypeUtils.ConvertFromSystem(methodInfo.ReturnType);
        }
        if (_method is ConstructorInfo)
        {
            returnType = DataType.Object;
        }

        var node = new FunctionSignatureNode(nameOverride ?? _name, returnType, paramsList);
        return node;
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
                arr[i] = DataTypeUtils.ConvertValue(args.GetAt(i), parameter.ParameterType);
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
            task.ConfigureAwait(false).GetAwaiter().GetResult();
            if (_method is MethodInfo methodInfo &&
                methodInfo.ReturnType.IsGenericType)
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
