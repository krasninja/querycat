using System.Threading;
using System.Threading.Tasks;

namespace QueryCat.Plugins.Client.Remote;

/// <summary>
/// Provides session to work with Thrift API.
/// </summary>
public interface IThriftSessionProvider
{
    /// <summary>
    /// Returns the instance of <see cref="IThriftSession" />.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IThriftSession" />.</returns>
    ValueTask<IThriftSession> GetAsync(CancellationToken cancellationToken = default);
}
