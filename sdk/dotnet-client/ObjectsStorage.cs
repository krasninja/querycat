using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The class is a simple objects storage.
/// </summary>
public sealed class ObjectsStorage
{
#if NET9_0_OR_GREATER
    private readonly Lock _objLock = new();
#else
    private readonly object _objLock = new();
#endif
    private readonly List<object?> _objects = new();

    public int Count
    {
        get
        {
            lock (_objLock)
            {
                return _objects.Count;
            }
        }
    }

    public int Add(object obj)
    {
        lock (_objLock)
        {
            _objects.Add(obj);
            return _objects.Count - 1;
        }
    }

    public T Get<T>(int index) where T : class
    {
        lock (_objLock)
        {
            if (index < 0 || index >= _objects.Count)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_InvalidHandle);
            }
            var plainObject = _objects[index];
            if (plainObject == null)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_Released);
            }
            var obj = plainObject as T;
            if (obj == null)
            {
                throw new QueryCatPluginException(
                    ErrorType.INVALID_OBJECT,
                    string.Format(Resources.Errors.Object_InvalidTypeSource, plainObject.GetType().Name, typeof(T).Name));
            }
            return obj;
        }
    }

    public bool TryGet<T>(int index, [NotNullWhen(true)] out T? obj) where T : class
    {
        lock (_objLock)
        {
            if (index < 0 || index >= _objects.Count)
            {
                obj = null;
                return false;
            }
            var rawObject = _objects[index];
            if (rawObject == null)
            {
                obj = null;
                return false;
            }
            obj = rawObject as T;
            if (obj == null)
            {
                return false;
            }
            return true;
        }
    }

    public int GetOrAdd(object obj)
    {
        lock (_objLock)
        {
            var index = Find(obj);
            if (index < 0)
            {
                index = Add(obj);
            }
            return index;
        }
    }

    public int Find(object obj)
    {
        lock (_objLock)
        {
            for (var i = 0; i < _objects.Count; i++)
            {
                var targetObj = _objects[i];
                if (targetObj != null
                    && object.ReferenceEquals(obj, targetObj))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public void Remove(int index)
    {
        object? obj;
        lock (_objLock)
        {
            if (index < 0 || index >= _objects.Count)
            {
                return;
            }
            obj = _objects[index];
            _objects[index] = null;
        }

        if (obj is IAsyncDisposable asyncDisposable)
        {
            AsyncUtils.RunSync(async () => await asyncDisposable.DisposeAsync());
        }
        else if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public void Clean()
    {
        int totalObjects;
        lock (_objLock)
        {
            totalObjects = _objects.Count;
        }
        for (var i = 0; i < totalObjects; i++)
        {
            Remove(i);
        }
    }
}
