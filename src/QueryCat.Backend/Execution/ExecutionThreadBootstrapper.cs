using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Abstractions.Plugins;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Functions.StandardFunctions;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public sealed class ExecutionThreadBootstrapper
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger<ExecutionThreadBootstrapper>();

    public void Bootstrap(ExecutionThread executionThread, PluginsLoader pluginsLoader, params Action<FunctionsManager>[] registrations)
    {
#if DEBUG
        var timer = new Stopwatch();
        timer.Start();
#endif

        executionThread.FunctionsManager.RegisterFactory(StringFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(CryptoFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(DateTimeFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(InfoFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MathFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(MiscFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(JsonFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(ObjectFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(AggregatesRegistration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(Providers.Registration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(Formatters.Registration.Register);
        foreach (var registration in registrations)
        {
            registration.Invoke(executionThread.FunctionsManager);
        }
        AsyncUtils.RunSync(pluginsLoader.LoadAsync);
#if DEBUG
        timer.Stop();
        _logger.LogTrace("Bootstrap time: {Time}.", timer.Elapsed);
#endif
    }
}
