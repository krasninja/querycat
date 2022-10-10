using System.Threading.Tasks;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Clean")]
public class CleanTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        // TODO:
        return Task.CompletedTask;
    }
}
