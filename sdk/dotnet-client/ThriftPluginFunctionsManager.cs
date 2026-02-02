using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;
using FunctionCallArguments = QueryCat.Backend.Core.Functions.FunctionCallArguments;
using FunctionCallArgumentsTypes = QueryCat.Backend.Core.Functions.FunctionCallArgumentsTypes;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Plugin functions manager. This is the simplified and limited version of functions manager.
/// It does not support aggregates and does not parse functions signature. No overloading.
/// </summary>
public sealed class ThriftPluginFunctionsManager : IFunctionsManager
{
    private readonly ThriftPluginClient _client;
    private readonly Dictionary<string, IFunction> _functions = new();

    /// <inheritdoc />
    public FunctionsFactory Factory { get; } = new PluginFunctionsFactory();

    public ThriftPluginFunctionsManager(ThriftPluginClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public IFunction ResolveUri(string uri)
    {
        return AsyncUtils.RunSync(async () =>
        {
            var function = await _client.ThriftClient.ResolveUriAsync(
                _client.Token,
                uri);
            return CreateFunction(function);
        })!;
    }

    private PluginFunction CreateFunction(Sdk.Function function)
    {
        async Task<VariantValue> ExecuteDelegate(IExecutionThread t, CancellationToken ct)
        {
            var args = new FunctionCallArguments();
            while (t.Stack.FrameLength > 0)
            {
                var arg = t.Stack.Pop();
                args.Add(SdkConvert.Convert(arg));
            }

            return await this.CallFunctionAsync(
                PluginFunction.GetFunctionName(function.Signature),
                _client.ExecutionThread,
                args,
                ct
            );
        }

        return new PluginFunction(
            @delegate: ExecuteDelegate,
            function.Signature,
            new FunctionMetadata
            {
                Description = function.Description,
                IsAggregate = function.IsAggregate,
                IsSafe = function.IsSafe,
            }
        );
    }

    /// <inheritdoc />
    public void RegisterFunction(IFunction function)
    {
        _functions[function.Name] = function;
        if (_client.IsActive)
        {
            AsyncUtils.RunSync(async ct =>
            {
                await _client.ThriftClient.RegisterFunctionAsync(
                    _client.Token,
                    [new Function(
                        FunctionFormatter.GetSignature(function),
                        function.Description,
                        function.IsAggregate)],
                    ct);
            });
        }
    }

    /// <inheritdoc />
    public IFunction[] FindByName(
        string name,
        FunctionCallArgumentsTypes? functionArgumentsTypes = null)
    {
        name = FunctionFormatter.NormalizeName(name);
        if (_functions.TryGetValue(name, out var functionInfo))
        {
            return [functionInfo];
        }

        var result = AsyncUtils.RunSync(async ct =>
        {
            return await _client.ThriftClient.FindFunctionByNameAsync(
                _client.Token,
                name,
                functionArgumentsTypes != null
                    ? SdkConvert.Convert(functionArgumentsTypes)
                    : null,
                ct);
        })!;

        return result.Select(f => (IFunction)CreateFunction(f)).ToArray();
    }

    /// <inheritdoc />
    public IEnumerable<IFunction> GetFunctions()
    {
        var localFunctions = _functions.Values;
        if (!_client.IsActive)
        {
            return localFunctions;
        }
        var remoteFunctions = AsyncUtils.RunSync(async ct =>
        {
            return await _client.ThriftClient.GetFunctionsAsync(
                _client.Token,
                ct);
        })!;
        var localFunctionsSignatures = localFunctions.Select(FunctionFormatter.GetSignature);
        remoteFunctions = remoteFunctions
            .Where(f => localFunctionsSignatures.All(lf => lf != f.Signature))
            .ToList();
        return localFunctions.Union(remoteFunctions.Select(CreateFunction));
    }

    /// <summary>
    /// Get all functions signatures.
    /// </summary>
    /// <returns>Signatures strings.</returns>
    public IEnumerable<IFunction> GetPluginFunctions() => _functions.Values;

    /// <inheritdoc />
    public async ValueTask<VariantValue> CallFunctionAsync(
        IFunction function,
        IExecutionThread executionThread,
        FunctionCallArguments callArguments,
        CancellationToken cancellationToken = default)
    {
        var result = await _client.ThriftClient.CallFunctionAsync(
            _client.Token,
            function.Name,
            SdkConvert.Convert(callArguments),
            -1,
            cancellationToken);
        return SdkConvert.Convert(result);
    }
}
