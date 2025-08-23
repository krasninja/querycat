using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Parser;
using QueryCat.Cli.Commands;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
internal sealed class Program
{
    private static readonly Lock _objLock = new();

    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">Application execution arguments.</param>
    /// <returns>Error code.</returns>
    public static async Task<int> Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        // Root.
        var rootCommand = new ApplicationRootCommand
        {
            new QueryCommand(),
            new ExplainCommand(),
            new AstCommand(),
            new SchemaCommand(),
            new CallFunctionCommand(),
            new ServeCommand(),
#if ENABLE_PLUGINS
            new Command("plugin", Resources.Messages.PluginCommand_Description)
            {
                new PluginInstallCommand(),
                new PluginListCommand(),
                new PluginRemoveCommand(),
                new PluginUpdateCommand(),
                new PluginDebugCommand(),
#if PLUGIN_THRIFT
                new PluginProxyCommand(),
#endif
            },
#endif
        };
        rootCommand.TreatUnmatchedTokensAsErrors = false;

        // Allow to query without "query" command. Fast way.
        var queryArgument = new Argument<string>("query")
        {
            Hidden = true,
        };
        rootCommand.Add(queryArgument);
        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            // Allow to use with shebang.
            if (args.Length == 1
                && args[0].Length < 140
                && !args[0].Contains(Environment.NewLine)
                && File.Exists(args[0]))
            {
                await new QueryCommand().Parse(["-f", args[0]])
                    .InvokeAsync(parseResult.InvocationConfiguration, cancellationToken);
            }
            else
            {
                await new QueryCommand().Parse(args)
                    .InvokeAsync(parseResult.InvocationConfiguration, cancellationToken);
            }
        });

        SetCustomHelpMessage(rootCommand);

        int returnCode;
        try
        {
            returnCode = await rootCommand.Parse(args).InvokeAsync();
        }
        catch (Exception e)
        {
            returnCode = ProcessException(e);
        }
        Application.LoggerFactory.Dispose();
        return returnCode;
    }

    private static void SetCustomHelpMessage(RootCommand rootCommand)
    {
        foreach (var rootCommandOption in rootCommand.Options)
        {
            // RootCommand has a default HelpOption, we need to update its Action.
            if (rootCommandOption is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new UsageHelpSection((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }
    }

    private static void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        var returnCode = 1;
        if (e.ExceptionObject is Exception exception)
        {
            returnCode = ProcessException(exception);
        }
        Environment.Exit(returnCode);
    }

    private sealed class UsageHelpSection(HelpAction action) : SynchronousCommandLineAction
    {
        /// <inheritdoc />
        public override int Invoke(ParseResult parseResult)
        {
            var result = action.Invoke(parseResult);
            if (parseResult.CommandResult.Command is ApplicationRootCommand)
            {
                var output = parseResult.InvocationConfiguration.Output;
                output.WriteLine(Resources.Messages.HelpText);
            }
            return result;
        }
    }

    private static int ProcessException(Exception exception)
    {
        var logger = Application.LoggerFactory.CreateLogger(nameof(Program));
        lock (_objLock)
        {
            if (exception is AggregateException aggregateException)
            {
                exception = aggregateException.InnerExceptions[0];
            }

            if (exception is SyntaxException syntaxException)
            {
                logger.LogError(syntaxException.GetErrorLine());
                logger.LogError(new string(' ', syntaxException.Position) + '^');
                logger.LogError("{Line}:{Position}: {Message}", syntaxException.Line, syntaxException.Position,
                    syntaxException.Message);
                return 4;
            }
            else if (exception is QueryCatException domainException)
            {
                logger.LogError(domainException.Message);
                return 2;
            }
            else if (exception is FormatException formatException)
            {
                logger.LogError(formatException.Message);
                return 3;
            }
            else if (exception is OperationCanceledException)
            {
                return 0;
            }
            else
            {
                logger.LogCritical(logger.IsEnabled(LogLevel.Debug) ? exception : null, exception.Message);
                return 1;
            }
        }
    }
}
