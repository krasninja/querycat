﻿using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Text;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Tester;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    private static readonly Lazy<ILogger> _logger = new(() => Application.LoggerFactory.CreateLogger(nameof(Program)));

    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            args = ["-h"];
        }

        var rootCommand = new RootCommand("QueryCat Tester");
        var pluginFilesOption = new Option<string[]>("--plugin-files",
            description: "Plugin files.")
        {
            AllowMultipleArgumentsPerToken = true,
        };
        var queryArgument = new Argument<string>("query",
            description: "SQL-like query or command argument.",
            getDefaultValue: () => string.Empty);

        rootCommand.AddOption(pluginFilesOption);
        rootCommand.AddArgument(queryArgument);

        rootCommand.SetHandler(
            Run,
            queryArgument,
            pluginFilesOption);

        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption("-v", "--version")
            .UseDefaults()
            .Build();
        var returnCode = parser.Parse(args).Invoke();
        return returnCode;
    }

    public static void Run(string query, string[] pluginDirectories)
    {
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
                pluginDirectories.Union(workingDirectoryPlugins),
                thread.FunctionsManager))
            .Create();

        executionThread.Run(query);
        Console.Out.WriteLine(outputStringBuilder);
    }
}