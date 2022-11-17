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
        ExecutionThread.FunctionsManager.RegisterWithFunction(DsvFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(JsonFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(NullFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(TextLineFormatter.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(DateTimeFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(InfoFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(MathFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(MiscFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(StringFunctions.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(AggregatesRegistration.RegisterFunctions);
        ExecutionThread.FunctionsManager.RegisterWithFunction(Providers.Registration.RegisterFunctions);
        LoadPlugins();
#if DEBUG
        timer.Stop();
        Logger.Instance.Trace($"Bootstrap time: {timer.Elapsed}.");
#endif
    }

    private void LoadPlugins()
    {
        // Additional directories to find plugins.
        var exeDirectory = AppContext.BaseDirectory;
        ExecutionThread.Options.PluginDirectories.Add(
            Path.Combine(ExecutionThread.GetApplicationDirectory(), ExecutionThread.ApplicationPluginsDirectory));
        ExecutionThread.Options.PluginDirectories.Add(exeDirectory);
        ExecutionThread.Options.PluginDirectories.Add(
            Path.Combine(exeDirectory, ExecutionThread.ApplicationPluginsDirectory));

        var pluginLoader = new PluginsLoader(ExecutionThread.Options.PluginDirectories);
        ExecutionThread.Options.PluginAssemblies.AddRange(pluginLoader.LoadPlugins());

        foreach (var pluginAssembly in ExecutionThread.Options.PluginAssemblies)
        {
            ExecutionThread.FunctionsManager.RegisterFromAssembly(pluginAssembly);
        }
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
        var programNode = AstBuilder.BuildProgramFromString(query);

        // Set first executing statement and run.
        ExecutionThread.ExecutingStatement = programNode.Statements.FirstOrDefault();
        ExecutionThread.Run();
        return ExecutionThread.LastResult;
    }
}
