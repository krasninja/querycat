using QueryCat.Backend.Core.Execution;
using QueryCat.Plugins.Client;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ThriftRemoteAnswerAgent : IAnswerAgent
{
    private readonly ThriftPluginContext _context;
    private readonly int _objectHandle;

    public ThriftRemoteAnswerAgent(ThriftPluginContext context, int objectHandle)
    {
        _context = context;
        _objectHandle = objectHandle;
    }

    /// <inheritdoc />
    public async Task<QuestionResponse> AskAsync(QuestionRequest request, CancellationToken cancellationToken = default)
    {
        using var session = await _context.GetSessionAsync(cancellationToken);
        var response = await session.ClientProxy.AnswerAgent_AskAsync(_objectHandle, SdkConvert.Convert(request), cancellationToken);
        return SdkConvert.Convert(response);
    }
}
