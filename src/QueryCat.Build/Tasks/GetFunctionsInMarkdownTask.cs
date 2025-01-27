using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.FunctionsManager;
using QueryCat.Backend.Parser;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Functions-Markdown")]
public sealed class GetFunctionsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private sealed class LowercaseFunctionWrapper : IFunction
    {
        private readonly IFunction _function;

        /// <inheritdoc />
        public Delegate Delegate => _function.Delegate;

        /// <inheritdoc />
        public string Name => _function.Name.ToLowerInvariant();

        /// <inheritdoc />
        public string Description => _function.Description;

        /// <inheritdoc />
        public DataType ReturnType => _function.ReturnType;

        /// <inheritdoc />
        public string ReturnObjectName => _function.ReturnObjectName;

        /// <inheritdoc />
        public bool IsAggregate => _function.IsAggregate;

        /// <inheritdoc />
        public FunctionSignatureArgument[] Arguments => _function.Arguments
            .Select(a => new FunctionSignatureArgument(
                a.Name.ToLowerInvariant(),
                a.Type,
                a.DefaultValue,
                a.IsOptional,
                a.IsArray,
                a.IsVariadic))
            .ToArray();

        /// <inheritdoc />
        public bool IsSafe => _function.IsSafe;

        /// <inheritdoc />
        public string[] Formatters => _function.Formatters;

        public LowercaseFunctionWrapper(IFunction function)
        {
            _function = function;
        }
    }

    private sealed class MarkdownFunctionsManager : IFunctionsManager
    {
        private readonly List<IFunction> _functions = new();

        /// <inheritdoc />
        public FunctionsFactory Factory { get; } = new DefaultFunctionsFactory(new AstBuilder());

        /// <inheritdoc />
        public IFunction? ResolveUri(string uri) => null;

        /// <inheritdoc />
        public void RegisterFunction(IFunction function)
        {
            _functions.Add(new LowercaseFunctionWrapper(function));
        }

        /// <inheritdoc />
        public IFunction[] FindByName(
            string name,
            FunctionCallArgumentsTypes? functionArgumentsTypes = null)
        {
            return [];
        }

        /// <inheritdoc />
        public IEnumerable<IFunction> GetFunctions() => _functions;

        /// <inheritdoc />
        public ValueTask<VariantValue> CallFunctionAsync(
            IFunction function,
            IExecutionThread executionThread,
            FunctionCallArguments callArguments,
            CancellationToken cancellationToken = default) => ValueTask.FromResult(VariantValue.Null);
    }

    /// <inheritdoc />
    public override async Task RunAsync(BuildContext context)
    {
        var targetFile = context.Arguments.GetArgument("File");
        var outDirectory = context.Arguments.HasArgument("Out")
            ? context.Arguments.GetArgument("Out")
            : context.OutputDirectory;

        var functionsManager = new MarkdownFunctionsManager();
        var loader = new DotNetAssemblyPluginsLoader(functionsManager, [targetFile]);
        await loader.LoadAsync();

        var sb = new StringBuilder()
            .AppendLine("# Functions")
            .AppendLine()
            .AppendLine("| Name and Description |")
            .AppendLine("| --- |");
        foreach (var function in functionsManager.GetFunctions().OrderBy(m => m.Name))
        {
            var signature = FunctionFormatter.GetSignature(function, forceLowerCase: true);
            sb.Append($"| `{signature}`");
            if (!string.IsNullOrEmpty(function.Description))
            {
                sb.Append($"<br /><br /> {function.Description}");
            }
            sb.AppendLine(" |");
        }
        var file = Path.Combine(outDirectory, "Functions.md");
        await File.WriteAllTextAsync(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");
        Console.WriteLine(await File.ReadAllTextAsync(file));

        await base.RunAsync(context);
    }
}
