using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Ast.Nodes.Function;
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

    [DebuggerDisplay("Name = {Name}, ReturnType = {ReturnType}")]
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

    [DebuggerDisplay("Name = {Name}, ReturnType = {ReturnType}")]
    private sealed class LazySignatureFunction : IFunction
    {
        private string _signature;
        private FunctionSignatureNode? _functionSignature;
        private readonly IAstBuilder _astBuilder;

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

    [DebuggerDisplay("Name = {Name}, ReturnType = {ReturnType}")]
    private sealed class LazyAggregateFunction<TAggregate> : IFunction where TAggregate : IAggregateFunction
    {
        private string _signature;
        private FunctionSignatureNode? _functionSignature;
        private readonly IAstBuilder _astBuilder;

        /// <inheritdoc />
        public Delegate Delegate => DelegateMethod;

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
                var descriptionAttribute = typeof(TAggregate).GetCustomAttribute<DescriptionAttribute>();
                return descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
            }
        }

        /// <inheritdoc />
        public DataType ReturnType => GetSignature().ReturnType;

        /// <inheritdoc />
        public string ReturnObjectName => GetSignature().ReturnTypeNode.TypeName;

        /// <inheritdoc />
        public bool IsAggregate => true;

        private FunctionSignatureArgument[]? _arguments;

        /// <inheritdoc />
        public FunctionSignatureArgument[] Arguments => GetArguments();

        /// <inheritdoc />
        public bool IsSafe => true;

        /// <inheritdoc />
        public string[] Formatters => [];

        private VariantValue DelegateMethod(IExecutionThread executionThread)
        {
            return VariantValue.CreateFromObject(TAggregate.CreateInstance());
        }

        public LazyAggregateFunction(string signature, IAstBuilder astBuilder)
        {
            _signature = signature;
            _astBuilder = astBuilder;
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

    private static string GetFunctionName(string signature)
    {
        // Fast path to get function name.
        var indexOfLeftParen = signature.IndexOf('(', StringComparison.InvariantCulture);
        if (indexOfLeftParen < 0)
        {
            return FunctionFormatter.NormalizeName(signature);
        }
        return FunctionFormatter.NormalizeName(signature[..indexOfLeftParen]);
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
    public override IEnumerable<IFunction> CreateAggregateFromType<TAggregate>()
    {
        var signatureAttributes = typeof(TAggregate).GetCustomAttributes<AggregateFunctionSignatureAttribute>();
        foreach (var signatureAttribute in signatureAttributes)
        {
            var function = new LazyAggregateFunction<TAggregate>(signatureAttribute.Signature, _astBuilder);
            yield return function;
        }
    }

    /// <inheritdoc />
    public override IEnumerable<IFunction> CreateFromDelegate(Delegate functionDelegate)
    {
        if (FunctionCaller.IsValidFunctionDelegate(functionDelegate))
        {
            var methodAttributes = functionDelegate.Method.GetCustomAttributes<FunctionSignatureAttribute>();

            foreach (var methodAttribute in methodAttributes)
            {
                var function = new LazyAttributesFunction(methodAttribute.Signature, functionDelegate, _astBuilder);
                yield return function;
            }
        }
        else
        {
            var function = CreateFunctionFromMethodInfo(functionDelegate.Method);
            if (function != null)
            {
                yield return function;
            }
        }
    }

    /// <inheritdoc />
    public override IFunction CreateFromSignature(
        string signature,
        Delegate functionDelegate,
        FunctionMetadata? functionMetadata = null)
    {
        var function = new LazySignatureFunction(signature, functionDelegate, _astBuilder);
        if (functionMetadata != null)
        {
            function.Description = functionMetadata.Description;
            function.IsSafe = functionMetadata.IsSafe;
            function.Formatters = functionMetadata.Formatters;
        }
        return function;
    }
}
