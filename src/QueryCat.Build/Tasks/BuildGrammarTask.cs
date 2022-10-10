using System.Threading.Tasks;
using Cake.Common;
using Cake.Frosting;

namespace QueryCat.Build.Tasks;

[TaskName("Build-Grammar")]
[TaskDescription("Generate C# files for ANTLR4 grammar")]
public class BuildGrammarTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var args = new[]
        {
            "-Dlanguage=CSharp",
            "-no-listener",
            "-visitor",
            "-package QueryCat.Backend.Parser",
            "../QueryCat.Backend/Parser/QueryCatLexer.g4",
            "../QueryCat.Backend/Parser/QueryCatParser.g4",
        };
        context.StartProcess("antlr4", string.Join(' ', args));
        return Task.CompletedTask;
    }
}
