using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Plugins.Client.Remote;

public sealed class ThriftRemoteAnswerAgent : IAnswerAgent
{
    private readonly IThriftSessionProvider _sessionProvider;
    private readonly int _objectHandle;
    private readonly int _token;

    public ThriftRemoteAnswerAgent(IThriftSessionProvider sessionProvider, int objectHandle, int token = 0)
    {
        _sessionProvider = sessionProvider;
        _objectHandle = objectHandle;
        _token = token;
    }

    /// <inheritdoc />
    public async Task<QuestionResponse> AskAsync(QuestionRequest request, CancellationToken cancellationToken = default)
    {
        using var session = await _sessionProvider.GetAsync(cancellationToken);
        var response = await session.Client.AnswerAgent_AskAsync(_token, _objectHandle,
            SdkConvert.Convert(request), cancellationToken);
        return SdkConvert.Convert(response);
    }
}
