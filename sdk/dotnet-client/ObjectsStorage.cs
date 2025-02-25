using System;
using System.Collections.Generic;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The class is a simple objects storage.
/// </summary>
public sealed class ObjectsStorage
{
    private readonly object _objLock = new();
    private readonly List<WeakReference?> _objects = new();

    public int Add(object obj)
    {
        lock (_objLock)
        {
            _objects.Add(new WeakReference(obj));
            return _objects.Count - 1;
        }
    }

    public T Get<T>(int index) where T : class
    {
        lock (_objLock)
        {
            if (index > _objects.Count - 1)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_InvalidHandle);
            }
            var rawObject = _objects[index];
            if (rawObject == null || !rawObject.IsAlive)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_Released);
            }
            var obj = rawObject.Target as T;
            if (obj == null)
            {
                throw new QueryCatPluginException(
                    ErrorType.INVALID_OBJECT,
                    string.Format(Resources.Errors.Object_InvalidTypeSource, rawObject.GetType().Name, typeof(T).Name));
            }
            return obj;
        }
    }

    public bool TryGet<T>(int index, out T? obj) where T : class
    {
        lock (_objLock)
        {
            if (index > _objects.Count - 1)
            {
                obj = null;
                return false;
            }
            var rawObject = _objects[index];
            if (rawObject == null || !rawObject.IsAlive)
            {
                obj = null;
                return false;
            }
            obj = rawObject.Target as T;
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
                var weakRef = _objects[i];
                if (weakRef != null
                    && weakRef.IsAlive
                    && weakRef.Target != null
                    && object.ReferenceEquals(obj, weakRef.Target))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public void Remove(int index)
    {
        lock (_objLock)
        {
            var weakRef = _objects[index];
            if (weakRef != null && weakRef.IsAlive && weakRef.Target is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _objects[index] = null;
        }
    }

    public void Clean()
    {
        lock (_objLock)
        {
            var totalObjects = _objects.Count;
            for (var i = 0; i < totalObjects; i++)
            {
                Remove(i);
            }
        }
    }
}
