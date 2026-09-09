using System.Diagnostics;
using QueryCat.Backend.Core.Utils;
#if !DEBUG
using SemaphoreSlimImpl = System.Threading.SemaphoreSlim;
#endif

namespace QueryCat.Backend.Utils;

#if DEBUG
[DebuggerDisplay("Id = {Id}, Current Count = {CurrentCount}")]
internal sealed class SemaphoreSlimImpl : SemaphoreSlim
{
    private static int _id;

    /// <summary>
    /// Identifier for debug.
    /// </summary>
    public int Id { get; } = Interlocked.Increment(ref _id);

    /// <inheritdoc />
    public SemaphoreSlimImpl(int initialCount) : base(initialCount)
    {
    }

    /// <inheritdoc />
    public SemaphoreSlimImpl(int initialCount, int maxCount) : base(initialCount, maxCount)
    {
    }

    /// <inheritdoc />
    public override string ToString() => $"{Id} - {base.ToString()}";
}
#endif

/// <summary>
/// A mutual exclusion lock that is compatible with async. This lock supports recursive calls.
/// </summary>
/// <remarks>
/// For reference: https://github.com/dotnet/wcf/blob/main/src/System.ServiceModel.Primitives/src/Internals/System/Runtime/AsyncLock.cs.
/// </remarks>
[DebuggerDisplay("Taken = {IsTaken}")]
public sealed class AsyncLock : IAsyncDisposable, IDisposable
{
    private static readonly DisposableObjectPool<SemaphoreSlimImpl> _semaphorePool = new(
        createFunc: () => new SemaphoreSlimImpl(1, 1),
        maximumRetained: 40
    );

    private readonly AsyncLocal<LockScope?> _currentScope = new();
#pragma warning disable CA2213
    private readonly SemaphoreSlimImpl _topLevelSemaphore;
#pragma warning restore CA2213
    private volatile bool _isDisposed;

    /// <summary>
    /// Is lock currently taken.
    /// </summary>
    public bool IsTaken => !_isDisposed && _topLevelSemaphore.CurrentCount == 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    public AsyncLock()
    {
        _topLevelSemaphore = _semaphorePool.Get();
    }

    private static void ReturnSemaphore(SemaphoreSlimImpl semaphore)
    {
        Debug.Assert(semaphore.CurrentCount == 1, "Semaphore must be fully released before pooling.");
        if (semaphore.CurrentCount == 1)
        {
            _semaphorePool.Return(semaphore);
        }
    }

    /// <summary>
    /// Take the lock.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var localCurrentSemaphore = ResolveTargetSemaphore(out var parent);
        var nextSemaphore = _semaphorePool.Get();
        var scope = new LockScope(parent, localCurrentSemaphore, nextSemaphore, this);

        var waitTask = scope.Current.WaitAsync(cancellationToken);
        _currentScope.Value = scope;
        if (waitTask.IsCompletedSuccessfully)
        {
            return ValueTask.FromResult<IAsyncDisposable>(scope);
        }

        return TakeLockCoreAsync(scope, waitTask);
    }

    private static async ValueTask<IAsyncDisposable> TakeLockCoreAsync(
        LockScope lockScope,
        Task waitTask)
    {
        try
        {
            await waitTask.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The acquisition failed (canceled). Nothing to release.
            lockScope.Abandon();
            throw;
        }
        return lockScope;
    }

    /// <summary>
    /// Take the lock. Sync version.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Disposable.</returns>
    public IDisposable Lock(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        var localCurrentSemaphore = ResolveTargetSemaphore(out var parent);
        localCurrentSemaphore.Wait(cancellationToken);
        var scope = new LockScope(parent, localCurrentSemaphore, _semaphorePool.Get(), this);
        _currentScope.Value = scope;
        return scope;
    }

    private SemaphoreSlimImpl ResolveTargetSemaphore(out LockScope? parent)
    {
        var scope = _currentScope.Value;
        while (scope != null)
        {
            var next = scope.Next;
            if (next != null)
            {
                parent = scope;
                return next;
            }
            scope = scope.Parent;
        }

        parent = null;
        return _topLevelSemaphore;
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
        await _topLevelSemaphore.WaitAsync().ConfigureAwait(false);
        _topLevelSemaphore.Release();
        ReturnSemaphore(_topLevelSemaphore);
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
        _topLevelSemaphore.Release();
        ReturnSemaphore(_topLevelSemaphore);
    }

    [DebuggerDisplay("Taken = {IsTaken}")]
    private sealed class LockScope : IAsyncDisposable, IDisposable
    {
        private readonly LockScope? _parent;
        private readonly SemaphoreSlimImpl _currentSemaphore;
        private SemaphoreSlimImpl? _nextSemaphore;
        private readonly AsyncLock _asyncLock;

        public bool IsTaken => _asyncLock.IsTaken;

        public LockScope? Parent => _parent;

        public SemaphoreSlimImpl Current => _currentSemaphore;

        public SemaphoreSlimImpl? Next => Volatile.Read(ref _nextSemaphore);

        public LockScope(LockScope? parent, SemaphoreSlimImpl currentSemaphore, SemaphoreSlimImpl nextSemaphore, AsyncLock asyncLock)
        {
            _parent = parent;
            _currentSemaphore = currentSemaphore;
            _nextSemaphore = nextSemaphore;
            _asyncLock = asyncLock;
        }

        /// <summary>
        /// The acquisition failed (canceled). Nothing was taken, so nothing is released.
        /// </summary>
        internal void Abandon()
        {
            var nextSemaphore = Interlocked.Exchange(ref _nextSemaphore, null);
            if (nextSemaphore != null)
            {
                ReturnSemaphore(nextSemaphore);
            }
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            var nextSemaphore = Interlocked.Exchange(ref _nextSemaphore, null);
            if (nextSemaphore == null)
            {
                return ValueTask.CompletedTask;
            }
            _asyncLock._currentScope.Value = Parent;

            return DisposeCoreAsync(nextSemaphore);
        }

        private async ValueTask DisposeCoreAsync(SemaphoreSlimImpl next)
        {
            await next.WaitAsync().ConfigureAwait(false);
            _currentSemaphore.Release();
            next.Release();
            ReturnSemaphore(next);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            var nextSemaphore = Interlocked.Exchange(ref _nextSemaphore, null);
            if (nextSemaphore == null)
            {
                return;
            }

            _asyncLock._currentScope.Value = Parent;

            nextSemaphore.Wait();
            _currentSemaphore.Release();
            nextSemaphore.Release();
            ReturnSemaphore(nextSemaphore);
        }
    }
}
