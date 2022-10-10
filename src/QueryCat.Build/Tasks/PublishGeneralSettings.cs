using Cake.Common.Tools.DotNet.Publish;
using Cake.Core.IO.Arguments;

namespace QueryCat.Build.Tasks;

public class PublishGeneralSettings : DotNetPublishSettings
{
    public PublishGeneralSettings(BuildContext context)
    {
        Configuration = DotNetConstants.ConfigurationRelease;
        PublishTrimmed = true;
        PublishSingleFile = true;
        NoLogo = true;
        PublishReadyToRun = false;
        IncludeAllContentForSelfExtract = true;
        EnableCompressionInSingleFile = false;
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
            pag.Append(new TextArgument($"-p:Version={context.Version}"));
            return pag;
        };
    }
}
