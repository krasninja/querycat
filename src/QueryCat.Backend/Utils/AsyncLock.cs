using System.Diagnostics;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Utils;

/// <summary>
/// A mutual exclusion lock that is compatible with async. This lock is supports recursive calls.
/// </summary>
/// <remarks>
/// For reference: https://github.com/dotnet/wcf/blob/main/src/System.ServiceModel.Primitives/src/Internals/System/Runtime/AsyncLock.cs.
/// </remarks>
[DebuggerDisplay("Taken = {IsTaken}")]
internal sealed class AsyncLock : IAsyncDisposable, IDisposable
{
    private static readonly DisposableObjectPool<SemaphoreSlim> _semaphorePool
        = new(() => new SemaphoreSlim(1), maximumRetained: 40);

    private readonly AsyncLocal<SemaphoreSlim?> _currentSemaphore = new();
#pragma warning disable CA2213
    private readonly SemaphoreSlim _topLevelSemaphore;
#pragma warning restore CA2213
    private bool _isDisposed;

    /// <summary>
    /// Is lock currently taken.
    /// </summary>
    public bool IsTaken => _topLevelSemaphore.CurrentCount == 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AsyncLock()
    {
        _topLevelSemaphore = _semaphorePool.Get();
    }

    /// <summary>
    /// Take the lock.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(AsyncLock));
        }

        _currentSemaphore.Value ??= _topLevelSemaphore;
        var localCurrentSemaphore = _currentSemaphore.Value;
        var nextSemaphore = _semaphorePool.Get();
        _currentSemaphore.Value = nextSemaphore;
        var safeRelease = new SafeSemaphoreRelease(localCurrentSemaphore, nextSemaphore, this);
        return TakeLockCoreAsync(localCurrentSemaphore, safeRelease, cancellationToken);
    }

    private async Task<IAsyncDisposable> TakeLockCoreAsync(
        SemaphoreSlim localCurrentSemaphore,
        SafeSemaphoreRelease safeSemaphoreRelease,
        CancellationToken cancellationToken)
    {
        await localCurrentSemaphore.WaitAsync(cancellationToken);
        return safeSemaphoreRelease;
    }

    /// <summary>
    /// Take the lock. Sync version.
    /// </summary>
    /// <returns>Disposable.</returns>
    public IDisposable Lock()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(AsyncLock));
        }

        _currentSemaphore.Value ??= _topLevelSemaphore;
        SemaphoreSlim localCurrentSemaphore = _currentSemaphore.Value;
        localCurrentSemaphore.Wait();
        var nextSemaphore = _semaphorePool.Get();
        _currentSemaphore.Value = nextSemaphore;
        return new SafeSemaphoreRelease(localCurrentSemaphore, nextSemaphore, this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        // Ensure the lock isn't held. If it is, wait for it to be released
        // before completing to dispose.
        await _topLevelSemaphore.WaitAsync();
        _topLevelSemaphore.Release();
        _semaphorePool.Return(_topLevelSemaphore);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        // Ensure the lock isn't held. If it is, wait for it to be released
        // before completing to dispose.
        _topLevelSemaphore.Wait();
        _semaphorePool.Return(_topLevelSemaphore);
    }

    [DebuggerDisplay("Taken = {IsTaken}")]
    private readonly struct SafeSemaphoreRelease : IAsyncDisposable, IDisposable
    {
        private readonly SemaphoreSlim _currentSemaphore;
        private readonly SemaphoreSlim _nextSemaphore;
        private readonly AsyncLock _asyncLock;

        public bool IsTaken => _asyncLock.IsTaken;

        public SafeSemaphoreRelease(SemaphoreSlim currentSemaphore, SemaphoreSlim nextSemaphore, AsyncLock asyncLock)
        {
            _currentSemaphore = currentSemaphore;
            _nextSemaphore = nextSemaphore;
            _asyncLock = asyncLock;
        }

        public ValueTask DisposeAsync()
        {
            // Update _asyncLock._currentSemaphore in the calling ExecutionContext
            // and defer any awaits to DisposeCoreAsync(). If this isn't done, the
            // update will happen in a copy of the ExecutionContext and the caller
            // won't see the changes.
            if (_currentSemaphore == _asyncLock._topLevelSemaphore)
            {
                _asyncLock._currentSemaphore.Value = null;
            }
            else
            {
                _asyncLock._currentSemaphore.Value = _currentSemaphore;
            }

            return DisposeCoreAsync();
        }

        private async ValueTask DisposeCoreAsync()
        {
            await _nextSemaphore.WaitAsync();
            _currentSemaphore.Release();
            _nextSemaphore.Release();
            _semaphorePool.Return(_nextSemaphore);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_currentSemaphore == _asyncLock._topLevelSemaphore)
            {
                _asyncLock._currentSemaphore.Value = null;
            }
            else
            {
                _asyncLock._currentSemaphore.Value = _currentSemaphore;
            }

            _nextSemaphore.Wait();
            _currentSemaphore.Release();
            _nextSemaphore.Release();
            _semaphorePool.Return(_nextSemaphore);
        }
    }
}
