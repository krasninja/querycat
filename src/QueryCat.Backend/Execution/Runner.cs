using System.Diagnostics;
using System.Text;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Logging;
using QueryCat.Backend.Parser;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public sealed class Runner
{
    private readonly AstBuilder _astBuilder = new();

    public ExecutionThread ExecutionThread { get; }

    public Runner(ExecutionOptions? executionOptions = null)
    {
        ExecutionThread = new ExecutionThread(executionOptions);
    }

    public void Bootstrap()
    {
#if DEBUG
        var timer = new Stopwatch();
        timer.Start();
#endif
        ExecutionThread.FunctionsManager.RegisterWithFunction(DsvFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(JsonFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(TextLineFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(DateTimeFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(InfoFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(MathFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(MiscFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(StringFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(AggregatesRegistration.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(Providers.Registration.RegisterFunctions);
        foreach (var pluginAssembly in ExecutionThread.Options.PluginAssemblies)
        {
            ExecutionThread.FunctionsManager.RegisterFromAssembly(pluginAssembly);
        }
#if DEBUG
        timer.Stop();
        Logger.Instance.Trace($"Bootstrap time: {timer.Elapsed}.");
#endif
    }

    /// <summary>
    /// Dumps current executing AST statement.
    /// </summary>
    /// <returns>AST string.</returns>
    public string DumpAst()
    {
        if (ExecutionThread.ExecutingStatement == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        var visitor = new StringDumpAstVisitor(sb);
        visitor.Run(ExecutionThread.ExecutingStatement);
        return sb.ToString();
    }

    /// <summary>
    /// Run text query.
    /// </summary>
    /// <param name="query">Query.</param>
    public VariantValue Run(string query)
    {
        var programNode = _astBuilder.BuildProgramFromString(query);

        // Set first executing statement and run.
        ExecutionThread.ExecutingStatement = programNode.Statements.FirstOrDefault();
        ExecutionThread.Run();
        return ExecutionThread.LastResult;
    }
}
