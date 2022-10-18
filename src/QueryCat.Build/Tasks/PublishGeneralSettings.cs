using Cake.Common.Tools.DotNet.Publish;
using Cake.Core.IO.Arguments;

namespace QueryCat.Build.Tasks;

public class PublishGeneralSettings : DotNetPublishSettings
{
    public PublishGeneralSettings(BuildContext context)
    {
        Configuration = DotNetConstants.ConfigurationRelease;
        PublishTrimmed = false;
        PublishSingleFile = true;
        NoLogo = true;
        PublishReadyToRun = true;
        IncludeAllContentForSelfExtract = false;
        EnableCompressionInSingleFile = true;
        SelfContained = true;
        ArgumentCustomization = pag =>
        {
            // https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options.
            pag.Append(new TextArgument("-p:AutoreleasePoolSupport=false"));
            pag.Append(new TextArgument("-p:DebuggerSupport=false"));
            pag.Append(new TextArgument("-p:EnableUnsafeBinaryFormatterSerialization=false"));
            pag.Append(new TextArgument("-p:EventSourceSupport=false"));
            pag.Append(new TextArgument("-p:HttpActivityPropagationSupport=false"));
            pag.Append(new TextArgument("-p:InvariantGlobalization=true"));
            pag.Append(new TextArgument("-p:MetadataUpdaterSupport=false"));
            // For reference: https://andrewlock.net/version-vs-versionsuffix-vs-packageversion-what-do-they-all-mean/.
            pag.Append(new TextArgument($"-p:InformationalVersion={context.Version}"));
            return pag;
        };
    }
}
