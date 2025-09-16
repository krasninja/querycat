using System.CommandLine;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
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
    private static readonly Option<string[]> _pluginFilesOption = new("--plugin-files")
    {
        Description = "Plugin files.",
        AllowMultipleArgumentsPerToken = true,
    };

    private static readonly Argument<string> _queryArgument = new("query")
    {
        Description = "SQL-like query or command argument.",
    };

    private static readonly Option<string[]> _filesOption = new("-f", "--files")
    {
        Description = "SQL files to execute.",
        AllowMultipleArgumentsPerToken = true,
    };

    private static readonly Option<string[]> _variablesOption = new("--var")
    {
        Description = "Variables for query.",
    };

    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("QueryCat Tester");

        rootCommand.Add(_pluginFilesOption);
        rootCommand.Add(_filesOption);
        rootCommand.Add(_queryArgument);
        rootCommand.Add(_variablesOption);

        rootCommand.SetAction((parseResult, cancellationToken) =>
        {
            return Run(
                parseResult.GetValue(_queryArgument) ?? string.Empty,
                parseResult.GetValue(_pluginFilesOption) ?? [],
                parseResult.GetValue(_filesOption) ?? [],
                parseResult.GetValue(_variablesOption) ?? [],
                cancellationToken
            );
        });

        return await rootCommand.Parse(args).InvokeAsync();
    }

    private static async Task Run(
        string query,
        string[] pluginDirectories,
        string[] files,
        string[] variables,
        CancellationToken cancellationToken = default)
    {
        InitializeLogger();

        var workingDirectoryPlugins = Directory.GetFiles(Environment.CurrentDirectory, "*.dll");
        var options = new ExecutionOptions
        {
            UseConfig = true,
            RunBootstrapScript = true,
        };

        var executionThread = new ExecutionThreadBootstrapper(options)
            .WithStandardFunctions()
            .WithStandardUriResolvers()
            .WithConfigStorage(new MemoryConfigStorage())
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
                var fileContent = await File.ReadAllTextAsync(file, cancellationToken);
                var result = await executionThread.RunAsync(fileContent, cancellationToken: cancellationToken);
                await PrintResultAsync(result, cancellationToken);
            }
        }
        else
        {
            var result = await executionThread.RunAsync(query, cancellationToken: cancellationToken);
            await PrintResultAsync(result, cancellationToken);
        }
    }

    private static async Task PrintResultAsync(
        VariantValue result,
        CancellationToken cancellationToken = default)
    {
        var outputStringBuilder = new StringBuilder();
        await using var output = new TextTableOutput(outputStringBuilder);
        var iterator = RowsIteratorConverter.Convert(result);
        output.QueryContext = new SimpleQueryContext(new QueryContextQueryInfo(iterator.Columns));
        await output.OpenAsync(cancellationToken);
        while (await iterator.MoveNextAsync(cancellationToken))
        {
            await output.WriteValuesAsync(iterator.Current.AsArray(), cancellationToken);
        }
        await output.CloseAsync(cancellationToken);
        await Console.Out.WriteLineAsync(outputStringBuilder, cancellationToken);
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
