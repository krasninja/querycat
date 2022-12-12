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
public class ExecutionThreadBootstrapper
{
    public void Bootstrap(ExecutionThread executionThread)
    {
#if DEBUG
        var timer = new Stopwatch();
        timer.Start();
#endif
        executionThread.FunctionsManager.RegisterFactory(DsvFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(NullFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(TextLineFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(DateTimeFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(InfoFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MathFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MiscFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(StringFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(AggregatesRegistration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(Providers.Registration.RegisterFunctions);
#if ENABLE_PLUGINS
        executionThread.LoadPlugins();
#endif
#if DEBUG
        timer.Stop();
        Logger.Instance.Trace($"Bootstrap time: {timer.Elapsed}.");
#endif
    }
}
