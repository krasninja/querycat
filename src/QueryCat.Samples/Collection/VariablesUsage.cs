using QueryCat.Backend.Execution;

namespace QueryCat.Samples.Collection;

internal class VariablesUsage : BaseUsage
{
    /// <inheritdoc />
    public override void Run()
    {
        var executionThread = new ExecutionThread();
        // Define variable in script.
        executionThread.Run("declare x int := 10;");
        // Define variable in code.
        executionThread.TopScope.DefineVariable("y", 5);
        Console.WriteLine(executionThread.Run("x+y").ToString());
    }
}