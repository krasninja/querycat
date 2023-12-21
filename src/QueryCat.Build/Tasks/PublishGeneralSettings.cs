using Cake.Common.Tools.DotNet.Publish;
using Cake.Core.IO.Arguments;

namespace QueryCat.Build.Tasks;

public class PublishGeneralSettings : DotNetPublishSettings
{
    public PublishGeneralSettings(BuildContext context, bool publishAot = false)
    {
        OutputDirectory = context.OutputDirectory;
        Configuration = DotNetConstants.ConfigurationRelease;
        PublishTrimmed = publishAot;
        PublishSingleFile = publishAot ? null : true;
        NoLogo = true;
        IncludeAllContentForSelfExtract = publishAot ? null : true;
        EnableCompressionInSingleFile = publishAot ? null : true;
        SelfContained = publishAot ? null : true;
        ArgumentCustomization = pag =>
        {
            if (publishAot)
            {
                pag.Append(new TextArgument("-p:PublishAot=true"));
                pag.Append(new TextArgument("-p:OptimizationPreference=Size"));
                pag.Append(new TextArgument("-p:StripSymbols=true"));
            }
            // https://docs.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options.
            pag.Append(new TextArgument("-p:AutoreleasePoolSupport=false"));
            pag.Append(new TextArgument("-p:DebuggerSupport=false"));
            pag.Append(new TextArgument("-p:EnableUnsafeBinaryFormatterSerialization=false"));
            pag.Append(new TextArgument("-p:EnableUnsafeUTF7Encoding=true"));
            pag.Append(new TextArgument("-p:EventSourceSupport=false"));
            pag.Append(new TextArgument("-p:HttpActivityPropagationSupport=false"));
            pag.Append(new TextArgument("-p:InvariantGlobalization=true"));
            pag.Append(new TextArgument("-p:MetadataUpdaterSupport=false"));
            pag.Append(new TextArgument("-p:UseNativeHttpHandler=true"));
            pag.Append(new TextArgument("-p:UseSystemResourceKeys=true"));
            // For reference: https://andrewlock.net/version-vs-versionsuffix-vs-packageversion-what-do-they-all-mean/.
            pag.Append(new TextArgument($"-p:InformationalVersion={context.Version}"));
            return pag;
        };
    }
}
