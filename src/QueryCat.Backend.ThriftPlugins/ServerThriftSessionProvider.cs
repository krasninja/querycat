using QueryCat.Plugins.Client.Remote;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Backend.ThriftPlugins;

internal sealed class ServerThriftSessionProvider : IThriftSessionProvider
{
    private readonly ThriftPluginContext _context;

    private sealed class ServerThriftSession : IThriftSession
    {
        private readonly ThriftPluginContext.ClientWrapper _clientWrapper;

        /// <inheritdoc />
        public QueryCatIO.IAsync Client => _clientWrapper.ClientProxy;

        public ServerThriftSession(ThriftPluginContext.ClientWrapper clientWrapper)
        {
            _clientWrapper = clientWrapper;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _clientWrapper.Dispose();
        }
    }

    public ServerThriftSessionProvider(ThriftPluginContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async ValueTask<IThriftSession> GetAsync(CancellationToken cancellationToken = default)
    {
        var session = await _context.GetSessionAsync(cancellationToken);
        return new ServerThriftSession(session);
    }
}
