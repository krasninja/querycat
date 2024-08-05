using System.ComponentModel;
using Cake.Common.Tools.DotNet;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Run-Tests")]
[Description("Run integration and unit tests.")]
public sealed class TestTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        context.DotNetTest(context.UnitTestsProjectDirectory);
        context.DotNetTest(context.IntegrationTestsProjectDirectory);
        return base.RunAsync(context);
    }
}
