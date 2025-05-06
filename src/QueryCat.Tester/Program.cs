using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Tester;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    private static readonly Lazy<ILogger> _logger = new(() => Application.LoggerFactory.CreateLogger(nameof(Program)));

    private static readonly Option<string[]> _pluginFilesOption = new("--plugin-files",
        description: "Plugin files.")
        {
            AllowMultipleArgumentsPerToken = true,
        };

    private static readonly Argument<string> _queryArgument = new("query",
        description: "SQL-like query or command argument.");

    private static readonly Option<string[]> _filesOption = new(["-f", "--files"],
        description: "SQL files to execute.")
        {
            AllowMultipleArgumentsPerToken = true,
        };

    private static readonly Option<string[]> _variablesOption = new("--var",
        description: "Pass variables.");

    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            args = ["-h"];
        }

        var rootCommand = new RootCommand("QueryCat Tester");

        rootCommand.AddOption(_pluginFilesOption);
        rootCommand.AddOption(_filesOption);
        rootCommand.AddArgument(_queryArgument);
        rootCommand.AddOption(_variablesOption);

        rootCommand.SetHandler(
            Run,
            _queryArgument,
            _pluginFilesOption,
            _filesOption,
            _variablesOption);

        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption("-v", "--version")
            .UseDefaults()
            .Build();
        var returnCode = await parser.Parse(args).InvokeAsync();
        return returnCode;
    }

    private static async Task Run(string query, string[] pluginDirectories, string[] files, string[] variables)
    {
        InitializeLogger();

        var workingDirectoryPlugins = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");
        var outputStringBuilder = new StringBuilder();
        var options = new ExecutionOptions
        {
            DefaultRowsOutput = new TextTableOutput(outputStringBuilder),
            UseConfig = true,
            RunBootstrapScript = true,
        };

        var executionThread = new ExecutionThreadBootstrapper(options)
            .WithStandardFunctions()
            .WithStandardUriResolvers()
            .WithConfigStorage(new MemoryInputConfigStorage())
            .WithPluginsLoader(thread => new SimplePluginsAssemblyLoader(
                workingDirectoryPlugins.Union(pluginDirectories),
                thread.FunctionsManager))
            .Create();
        await executionThread.PluginsManager.PluginsLoader.LoadAsync(new PluginsLoadingOptions(), CancellationToken.None);

        AddVariables(executionThread, variables);

        if (files.Any())
        {
            foreach (var file in files)
            {
                var fileContent = await File.ReadAllTextAsync(file);
                await executionThread.RunAsync(fileContent);
            }
        }
        else
        {
            await executionThread.RunAsync(query);
        }
        await Console.Out.WriteLineAsync(outputStringBuilder);
    }

    public static void AddVariables(IExecutionThread executionThread, string[]? variables = null)
    {
        if (variables == null || !variables.Any())
        {
            return;
        }

        foreach (var variable in variables)
        {
            var arr = StringUtils.GetFieldsFromLine(variable, '=');
            if (arr.Length != 2)
            {
                continue;
            }
            var name = arr[0];
            var stringValue = arr[1];
            var targetType = DataTypeUtils.DetermineTypeByValue(stringValue);
            if (!VariantValue.TryCreateFromString(stringValue, targetType, out var value))
            {
                continue;
            }
            executionThread.TopScope.Variables[name] = value.Cast(targetType);
        }
    }

    /// <summary>
    /// Pre initialization step for logger.
    /// </summary>
    private static void InitializeLogger()
    {
        Application.LoggerFactory = new LoggerFactory(
            providers: [new SimpleConsoleLoggerProvider()],
            new LoggerFilterOptions
            {
                MinLevel = LogLevel.Trace,
            });
    }
}
