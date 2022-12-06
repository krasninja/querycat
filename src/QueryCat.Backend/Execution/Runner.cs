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
public class Runner
{
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
        ExecutionThread.FunctionsManager.RegisterFactory(DsvFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(NullFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(TextLineFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(DateTimeFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(InfoFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(MathFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(MiscFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(StringFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(AggregatesRegistration.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterFactory(Providers.Registration.RegisterFunctions);
#if ENABLE_PLUGINS
        ExecutionThread.LoadPlugins();
#endif
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
        if (string.IsNullOrEmpty(query))
        {
            return VariantValue.Null;
        }

        var programNode = AstBuilder.BuildProgramFromString(query);

        // Set first executing statement and run.
        ExecutionThread.ExecutingStatement = programNode.Statements.FirstOrDefault();
        ExecutionThread.Run();
        return ExecutionThread.LastResult;
    }
}
