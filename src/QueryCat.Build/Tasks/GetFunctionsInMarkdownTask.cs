using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Functions-Markdown")]
public sealed class GetFunctionsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private sealed class MarkdownFunctionsManager : IFunctionsManager
    {
        public sealed record FunctionInfo(
            string Signature,
            string Description);

        private readonly List<FunctionInfo> _functions = new();

        public IReadOnlyList<FunctionInfo> Functions => _functions;

        /// <inheritdoc />
        public IFunction? ResolveUri(string uri) => null;

        /// <inheritdoc />
        public void RegisterAggregate<TAggregate>(Func<TAggregate> factory) where TAggregate : IAggregateFunction
        {
        }

        /// <inheritdoc />
        public void RegisterFunction(string signature, FunctionDelegate @delegate, string? description = null)
        {
            _functions.Add(new FunctionInfo(signature, description ?? string.Empty));
        }

        /// <inheritdoc />
        public void RegisterFactory(Action<IFunctionsManager> registerFunction, bool postpone = true)
        {
            registerFunction.Invoke(this);
        }

        /// <inheritdoc />
        public bool TryFindByName(string name, FunctionCallArgumentsTypes? functionArgumentsTypes, out IFunction[] functions)
        {
            functions = [];
            return false;
        }

        /// <inheritdoc />
        public bool TryFindAggregateByName(string name, out IAggregateFunction? aggregateFunction)
        {
            aggregateFunction = null;
            return false;
        }

        /// <inheritdoc />
        public IEnumerable<IFunction> GetFunctions()
        {
            yield break;
        }

        /// <inheritdoc />
        public VariantValue CallFunction(IFunction function, IExecutionThread executionThread, FunctionCallArguments callArguments) => default;
    }

    /// <inheritdoc />
    public override async Task RunAsync(BuildContext context)
    {
        var targetFile = context.Arguments.GetArgument("File");
        var functionsManager = new MarkdownFunctionsManager();
        var loader = new DotNetAssemblyPluginsLoader(functionsManager, [targetFile]);
        await loader.LoadAsync();
        if (!functionsManager.Functions.Any())
        {
            context.Log.Error("No functions.");
            return;
        }

        var sb = new StringBuilder()
            .AppendLine("| Name and Description |")
            .AppendLine("| --- |");
        foreach (var function in functionsManager.Functions.OrderBy(m => m.Signature))
        {
            sb.Append($"| `{function.Signature}`");
            if (!string.IsNullOrEmpty(function.Description))
            {
                sb.Append($"<br /><br /> {function.Description}");
            }
            sb.AppendLine(" |");
        }
        var file = Path.Combine(context.OutputDirectory, "functions.md");
        await File.WriteAllTextAsync(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");
        Console.WriteLine(await File.ReadAllTextAsync(file));

        await base.RunAsync(context);
    }
}
