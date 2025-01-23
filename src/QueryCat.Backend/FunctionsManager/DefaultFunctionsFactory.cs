using System.ComponentModel;
using System.Reflection;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// The class helps to create functions from different sources (delegates, types).
/// </summary>
public sealed class DefaultFunctionsFactory : FunctionsFactory
{
    private readonly IAstBuilder _astBuilder;

    #region Functions

    private sealed class LazyAttributesFunction : IFunction
    {
        private string _signature;
        private readonly IAstBuilder _astBuilder;
        private FunctionSignatureNode? _functionSignature;

        /// <inheritdoc />
        public Delegate Delegate { get; }

        /// <inheritdoc />
        public string Name
        {
            get
            {
                if (_functionSignature != null)
                {
                    return _functionSignature.Name;
                }
                return GetFunctionName(_signature);
            }
        }

        /// <inheritdoc />
        public string Description
        {
            get
            {
                var descriptionAttribute = Delegate.Method.GetCustomAttribute<DescriptionAttribute>();
                return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
            }
        }

        /// <inheritdoc />
        public DataType ReturnType => GetSignature().ReturnType;

        /// <inheritdoc />
        public string ReturnObjectName => GetSignature().ReturnTypeNode.TypeName;

        /// <inheritdoc />
        public bool IsAggregate => false;

        private FunctionSignatureArgument[]? _arguments;

        /// <inheritdoc />
        public FunctionSignatureArgument[] Arguments => GetArguments();

        /// <inheritdoc />
        public bool IsSafe => Delegate.Method.GetCustomAttribute<SafeFunctionAttribute>() != null;

        /// <inheritdoc />
        public string[] Formatters
        {
            get
            {
                var formatterAttribute = Delegate.Method.GetCustomAttribute<FunctionFormattersAttribute>();
                return formatterAttribute != null ? formatterAttribute.FormatterIds : [];
            }
        }

        public LazyAttributesFunction(string signature, Delegate @delegate, IAstBuilder astBuilder)
        {
            _signature = signature;
            _astBuilder = astBuilder;
            Delegate = @delegate;
        }

        private FunctionSignatureNode GetSignature()
        {
            if (_functionSignature != null)
            {
                return _functionSignature;
            }
            _functionSignature = _astBuilder.BuildFunctionSignatureFromString(_signature);
            _signature = string.Empty;
            return _functionSignature;
        }

        private FunctionSignatureArgument[] GetArguments()
        {
            if (_arguments != null)
            {
                return _arguments;
            }
            _arguments = GetSignatureArguments(GetSignature().ArgumentNodes);
            return _arguments;
        }
    }

    private sealed class LazySignatureFunction : IFunction
    {
        private string _signature;
        private readonly IAstBuilder _astBuilder;
        private FunctionSignatureNode? _functionSignature;

        /// <inheritdoc />
        public Delegate Delegate { get; }

        /// <inheritdoc />
        public string Name
        {
            get
            {
                if (_functionSignature != null)
                {
                    return _functionSignature.Name;
                }
                return GetFunctionName(_signature);
            }
        }

        /// <inheritdoc />
        public string Description { get; set; } = string.Empty;

        /// <inheritdoc />
        public DataType ReturnType => GetSignature().ReturnType;

        /// <inheritdoc />
        public string ReturnObjectName => GetSignature().ReturnTypeNode.TypeName;

        /// <inheritdoc />
        public bool IsAggregate => false;

        private FunctionSignatureArgument[]? _arguments;

        /// <inheritdoc />
        public FunctionSignatureArgument[] Arguments => GetArguments();

        /// <inheritdoc />
        public bool IsSafe { get; set; }

        /// <inheritdoc />
        public string[] Formatters { get; set; } = [];

        public LazySignatureFunction(string signature, Delegate @delegate, IAstBuilder astBuilder)
        {
            _signature = signature;
            _astBuilder = astBuilder;
            Delegate = @delegate;
        }

        private FunctionSignatureNode GetSignature()
        {
            if (_functionSignature != null)
            {
                return _functionSignature;
            }
            _functionSignature = _astBuilder.BuildFunctionSignatureFromString(_signature);
            _signature = string.Empty;
            return _functionSignature;
        }

        private FunctionSignatureArgument[] GetArguments()
        {
            if (_arguments != null)
            {
                return _arguments;
            }
            _arguments = GetSignatureArguments(GetSignature().ArgumentNodes);
            return _arguments;
        }
    }

    private sealed class AggregateFunction : IFunction
    {
        /// <inheritdoc />
        public Delegate Delegate { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public string Description { get; set; } = string.Empty;

        /// <inheritdoc />
        public DataType ReturnType { get; }

        /// <inheritdoc />
        public string ReturnObjectName { get; }

        /// <inheritdoc />
        public bool IsAggregate => true;

        /// <inheritdoc />
        public FunctionSignatureArgument[] Arguments { get; }

        /// <inheritdoc />
        public bool IsSafe { get; init; }

        /// <inheritdoc />
        public string[] Formatters => [];

        public AggregateFunction(FunctionSignatureNode signatureNode, Type aggregateType)
        {
            Name = signatureNode.Name;
            Delegate = (IExecutionThread thread)
                => VariantValue.CreateFromObject((IAggregateFunction)Activator.CreateInstance(aggregateType)!);
            ReturnType = signatureNode.ReturnType;
            ReturnObjectName = signatureNode.ReturnTypeNode.TypeName;
            Arguments = GetSignatureArguments(signatureNode.ArgumentNodes);
        }
    }

    private static string NormalizeName(string target) => target.ToUpperInvariant();

    private static string GetFunctionName(string signature)
    {
        // Fast path to get function name.
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.InvariantCulture);
        if (indexOfLeftParen < 0)
        {
            return NormalizeName(signature);
        }
        return NormalizeName(signature[..indexOfLeftParen]);
    }

    private static FunctionSignatureArgument[] GetSignatureArguments(FunctionSignatureArgumentNode[] argNodes)
    {
        var arguments = new FunctionSignatureArgument[argNodes.Length];
        for (var i = 0; i < argNodes.Length; i++)
        {
            arguments[i] = argNodes[i].SignatureArgument;
        }
        return arguments;
    }

    #endregion

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="astBuilder">AST builder.</param>
    internal DefaultFunctionsFactory(IAstBuilder astBuilder)
    {
        _astBuilder = astBuilder;
    }

    /// <inheritdoc />
    public override IFunction[] CreateAggregateFromType(Type aggregateType)
    {
        var signatureAttributes = aggregateType.GetCustomAttributes<AggregateFunctionSignatureAttribute>();
        var functions = new List<IFunction>();
        var descriptionAttribute = aggregateType.GetCustomAttribute<DescriptionAttribute>();
        var safeAttribute = aggregateType.GetCustomAttribute<SafeFunctionAttribute>();
        foreach (var signatureAttribute in signatureAttributes)
        {
            var signatureAst = _astBuilder.BuildFunctionSignatureFromString(signatureAttribute.Signature);
            var function = new AggregateFunction(signatureAst, aggregateType)
            {
                Description = descriptionAttribute != null ? descriptionAttribute.Description : string.Empty,
                IsSafe = safeAttribute != null,
            };
            functions.Add(function);
        }
        return functions.ToArray();
    }

    /// <inheritdoc />
    public override IFunction[] CreateFromDelegate(Delegate functionDelegate)
    {
        if (FunctionCaller.IsValidFunctionDelegate(functionDelegate))
        {
            var methodAttributes = Attribute.GetCustomAttributes(functionDelegate.Method, typeof(FunctionSignatureAttribute));
            if (methodAttributes.Length < 1)
            {
                throw new QueryCatException($"Delegate must have '{nameof(FunctionSignatureAttribute)}'.");
            }

            var functions = new IFunction[methodAttributes.Length];
            for (var i = 0; i < functions.Length; i++)
            {
                var methodAttribute = (FunctionSignatureAttribute)methodAttributes[i];
                var function = new LazyAttributesFunction(methodAttribute.Signature, functionDelegate, _astBuilder);
                functions[i] = function;
            }
            return functions;
        }
        else
        {
            var function = CreateFunctionFromMethodInfo(functionDelegate.Method);
            if (function != null)
            {
                return [function];
            }
        }

        throw new InvalidOperationException("Cannot create function by delegate.");
    }

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        string? description = null,
        bool isSafe = false,
        string[]? formatters = null)
    {
        var function = new LazySignatureFunction(signature, functionDelegate, _astBuilder);
        function.Description = description ?? string.Empty;
        function.IsSafe = isSafe;
        if (formatters != null)
        {
            function.Formatters = formatters;
        }
        return function;
    }
}
