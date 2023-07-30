using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", "Show query AST for debugging.")
    {
        this.SetHandler((applicationOptions, query, variables, files) =>
        {
            applicationOptions.InitializeLogger();
            var root = applicationOptions.CreateApplicationRoot();
            root.Thread.BeforeStatementExecute += (_, threadArgs) =>
            {
                Console.WriteLine(root.Thread.DumpAst());
                threadArgs.ContinueExecution = false;
            };
            AddVariables(root.Thread, variables);
            RunQuery(root.Thread, query, files);
        },
            new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption),
            QueryArgument,
            VariablesOption,
            FilesOption);
    }
}
