using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Functions;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// Manages functions search and registration.
/// </summary>
public sealed partial class DefaultFunctionsManager : IFunctionsManager
{
    private readonly List<IUriResolver> _uriResolvers = new(capacity: 8);

    private readonly Dictionary<string, List<IFunction>> _functions = new(capacity: 128);
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(DefaultFunctionsManager));

    /// <inheritdoc />
    public FunctionsFactory Factory { get; }

    internal DefaultFunctionsManager(IAstBuilder astBuilder, IEnumerable<IUriResolver>? uriResolvers = null)
    {
        Factory = new DefaultFunctionsFactory(astBuilder);
        if (uriResolvers != null)
        {
            _uriResolvers.AddRange(uriResolvers);
        }
    }

    #region Registration

    /// <inheritdoc />
    public void RegisterFunction(IFunction function)
    {
        var name = FunctionFormatter.NormalizeName(function.Name);
        if (_functions.TryGetValue(name, out var functions))
        {
            functions.Add(function);
        }
        else
        {
            _functions.Add(name, [function]);
        }

        LogRegisterFunction(function.Name);
        RegisterFormatters(name, function.Formatters);
    }

    private static void RegisterFormatters(string callFunctionName, string[] formatters)
    {
        var extension = string.Empty;
        var mimeType = string.Empty;
        foreach (var formatterId in formatters)
        {
            Formatters.FormattersInfo.RegisterFormatter(formatterId,
                (fm, et, args) => fm.CallFunctionAsync(callFunctionName, et, args));

            if (formatterId.StartsWith('.') && formatterId.Length < 10)
            {
                extension = formatterId;
            }
            else if (!formatterId.StartsWith('.') && formatterId.Contains('/'))
            {
                mimeType = formatterId;
            }

            if (!string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(mimeType))
            {
                IOFunctions.MimeTypesProvider.SetMimeAndExtension(extension, mimeType);
            }
        }
    }

    private void WarnAboutSimilarFunctions(IFunction function, IEnumerable<IFunction> functions)
    {
        foreach (var sameNameFunction in functions)
        {
            if (sameNameFunction.IsSignatureEqual(function))
            {
                _logger.LogWarning("Possibly similar signature function: {Function}.", function);
            }
        }
    }

    /// <inheritdoc />
    public IFunction? ResolveUri(string uri)
    {
        foreach (var uriResolver in _uriResolvers)
        {
            if (uriResolver.TryResolve(uri, out var functionName)
                && !string.IsNullOrEmpty(functionName))
            {
                var functions = FindByName(functionName);
                return functions.Length > 0 ? functions[0] : null;
            }
        }

        return null;
    }

    /// <summary>
    /// Add new URI resolver.
    /// </summary>
    /// <param name="uriResolver">Instance of <see cref="IUriResolver" />.</param>
    internal void AddUriResolver(IUriResolver uriResolver) => _uriResolvers.Add(uriResolver);

    #endregion

    /// <inheritdoc />
    public IFunction[] FindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null)
    {
        name = FunctionFormatter.NormalizeName(name);

        if (!_functions.TryGetValue(name, out var outFunctions))
        {
            return [];
        }

        if (functionArgumentsTypes == null)
        {
            return outFunctions.ToArray();
        }

        foreach (var func in outFunctions)
        {
            if (func.MatchesToArguments(functionArgumentsTypes))
            {
                return [func];
            }
        }

        return [];
    }

    /// <summary>
    /// Get all registered functions.
    /// </summary>
    /// <returns>Functions enumerable.</returns>
    public IEnumerable<IFunction> GetFunctions()
    {
        foreach (var function in _functions.Values.SelectMany(f => f))
        {
            yield return function;
        }
    }

    /// <inheritdoc />
    public async ValueTask<VariantValue> CallFunctionAsync(
        IFunction function,
        IExecutionThread executionThread,
        FunctionCallArguments callArguments,
        CancellationToken cancellationToken = default)
    {
        var positionalIndex = 0;

        var frame = executionThread.Stack.CreateFrame();
        foreach (var argument in function.Arguments)
        {
            if (callArguments.Positional.Count >= positionalIndex + 1)
            {
                frame.Push(callArguments.Positional[positionalIndex++]);
                continue;
            }

            if (callArguments.Named.TryGetValue(argument.Name, out var value))
            {
                frame.Push(value);
            }
            else
            {
                frame.Push(argument.DefaultValue);
            }
        }

        var result = await FunctionCaller.CallAsync(function.Delegate, executionThread, cancellationToken);
        frame.Dispose();
        return result;
    }

    [LoggerMessage(LogLevel.Trace, "Register function: {FunctionName}.")]
    private partial void LogRegisterFunction(string functionName);
}
