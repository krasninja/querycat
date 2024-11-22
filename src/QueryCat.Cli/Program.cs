using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Parser;
using QueryCat.Cli.Commands;

namespace QueryCat.Cli;

/// <summary>
/// Program entry point.
/// </summary>
internal class Program
{
    private static readonly Lazy<ILogger> _logger = new(() => Application.LoggerFactory.CreateLogger(nameof(Program)));

    private static readonly object _objLock = new();

    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">Application execution arguments.</param>
    /// <returns>Error code.</returns>
    public static int Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        if (args.Length < 1)
        {
            args = new[] { "-h" };
        }

        // Root.
        var rootCommand = new ApplicationRootCommand
        {
            new QueryCommand(),
            new ExplainCommand(),
            new AstCommand(),
            new SchemaCommand(),
            new ServeCommand(),
#if ENABLE_PLUGINS
            new Command("plugin", "Plugins management.")
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
            IsHidden = true,
        };
        rootCommand.AddArgument(queryArgument);
        rootCommand.SetHandler(_ =>
        {
            // Allow to use with shebang.
            if (args.Length == 1
                && args[0].Length < 140
                && !args[0].Contains(Environment.NewLine)
                && File.Exists(args[0]))
            {
                new QueryCommand().Parse("-f", args[0]).Invoke();
            }
            else
            {
                new QueryCommand().Parse(args).Invoke();
            }
        }, queryArgument);

        var parser = new CommandLineBuilder(rootCommand)
            .UseVersionOption("-v", "--version")
            .UseDefaults()
            .UseHelp(context =>
            {
                if (context.Command is RootCommand)
                {
                    var layouts = HelpBuilder.Default.GetLayout().ToList();
                    layouts.Insert(1, _ =>
                    {
                        context.Output.Write(Resources.Messages.HelpText);
                    });
                    context.HelpBuilder.CustomizeLayout(_ => layouts);
                }
            })
            .UseExceptionHandler((exception, ic) =>
            {
                ic.ExitCode = ProcessException(exception);
            }, errorExitCode: 1)
            .Build();

        var returnCode = parser.Parse(args).Invoke();
        Application.LoggerFactory.Dispose();
        return returnCode;
    }

    private static void CurrentDomainOnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            ProcessException(exception);
        }
        Environment.Exit(1);
    }

    private static int ProcessException(Exception exception)
    {
        var logger = _logger.Value;
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
            else
            {
                logger.LogCritical(exception, exception.Message);
                return 1;
            }
        }

        return 1;
    }
}
