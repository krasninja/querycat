using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client.Testing;

/// <summary>
/// In memory config storage to be used for testing.
/// </summary>
internal sealed class TestInputConfigStorage : IInputConfigStorage
{
    private readonly Dictionary<string, VariantValue> _map = new();

    /// <inheritdoc />
    public void Set(string key, VariantValue value)
    {
        if (value.IsNull)
        {
            _map.Remove(key);
        }
        else
        {
            _map[key] = value;
        }
    }

    /// <inheritdoc />
    public bool Has(string key) => _map.ContainsKey(key);

    /// <inheritdoc />
    public VariantValue Get(string key) => _map.GetValueOrDefault(key);

    /// <inheritdoc />
    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
