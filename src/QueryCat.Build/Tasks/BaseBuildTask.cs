using Cake.Core;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

/// <summary>
/// Base task for platform builds.
/// </summary>
public class BaseBuildTask : AsyncFrostingTask<BuildContext>
{
    protected bool GetPublishAot(BuildContext context)
    {
        if (bool.TryParse(context.Arguments.GetArgument(DotNetConstants.PublishAotArgument), out var value))
        {
            return value;
        }
        return true;
    }

    protected string GetProperties(BuildContext context) => context.Arguments.GetArgument(DotNetConstants.PropertiesArgument);

    protected string GetPlatform(BuildContext context, string @default)
    {
        var platform = context.Arguments.GetArgument(DotNetConstants.PlatformArgument);
        return !string.IsNullOrEmpty(platform) ? platform : @default;
    }
}
