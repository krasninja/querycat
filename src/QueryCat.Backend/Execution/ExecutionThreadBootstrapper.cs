using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public class ExecutionThreadBootstrapper
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<ExecutionThreadBootstrapper>();

    public void Bootstrap(ExecutionThread executionThread, PluginsLoader pluginsLoader)
    {
#if DEBUG
        var timer = new Stopwatch();
        timer.Start();
#endif
        executionThread.FunctionsManager.RegisterFactory(DsvFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(JsonFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(StringFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(NullFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(TextLineFormatter.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(CryptoFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(DateTimeFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(InfoFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MathFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MiscFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(JsonFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(ObjectFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(AggregatesRegistration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(Providers.Registration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(XmlFormatter.RegisterFunctions);
        AsyncUtils.RunSync(pluginsLoader.LoadAsync);
#if DEBUG
        timer.Stop();
        _logger.LogTrace("Bootstrap time: {Time}.", timer.Elapsed);
#endif
    }
}
