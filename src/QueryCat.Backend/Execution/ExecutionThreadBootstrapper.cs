using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Functions.AggregateFunctions;
using QueryCat.Backend.Functions.StandardFunctions;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The facade class that contains workflow to run query from string.
/// </summary>
public sealed class ExecutionThreadBootstrapper
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ExecutionThreadBootstrapper));

    public void Bootstrap(
        ExecutionThread executionThread,
        PluginsLoader? pluginsLoader = null,
        params Action<IFunctionsManager>[] registrations)
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
        executionThread.FunctionsManager.RegisterFactory(Inputs.Registration.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(IO.IOFunctions.RegisterFunctions);
        executionThread.FunctionsManager.RegisterFactory(Formatters.Registration.Register, postpone: false);
        foreach (var registration in registrations)
        {
            executionThread.FunctionsManager.RegisterFactory(registration, postpone: false);
        }
        if (pluginsLoader != null)
        {
            AsyncUtils.RunSync(pluginsLoader.LoadAsync);
        }
#if DEBUG
        timer.Stop();
        _logger.LogTrace("Bootstrap time: {Time}.", timer.Elapsed);
#endif
    }
}
