using System.Threading;
using System.Threading.Tasks;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client.Remote;

public sealed class SimpleThriftSessionProvider : IThriftSessionProvider
{
    private readonly IThriftSession _session;

    private sealed class SimpleThriftSession : IThriftSession
    {
        /// <inheritdoc />
        public QueryCatIO.IAsync Client { get; }

        public SimpleThriftSession(QueryCatIO.IAsync client)
        {
            Client = client;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    public SimpleThriftSessionProvider(QueryCatIO.IAsync client)
    {
        _session = new SimpleThriftSession(client);
    }

    /// <inheritdoc />
    public ValueTask<IThriftSession> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_session);
}