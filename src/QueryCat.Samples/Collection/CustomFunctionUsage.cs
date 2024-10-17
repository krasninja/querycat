using System.ComponentModel;
using QueryCat.Backend;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Samples.Collection;

internal class CustomFunctionUsage : BaseUsage
{
    [Description("Energy calculator.")]
    [FunctionSignature("e(m: numeric): numeric")]
    public static VariantValue EnergyFunction(IExecutionThread thread)
    {
        var lightSpeedMetersPerSecond = 299_792_458;
        var mass = thread.Stack[0].AsNumeric;
        return new VariantValue(mass * lightSpeedMetersPerSecond * lightSpeedMetersPerSecond);
    }

    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThreadBootstrapper().Create();
        executionThread.FunctionsManager.RegisterFunction(EnergyFunction);
        Console.WriteLine(executionThread.Run("e(100::numeric)").AsString); // 8987551787368176400
    }
}
