using System;
using System.Collections.Generic;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The class is a simple objects storage.
/// </summary>
public sealed class ObjectsStorage
{
    private readonly object _objLock = new();
    private readonly List<object?> _objects = new();

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
            if (index > _objects.Count - 1)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_InvalidHandle);
            }
            var rawObject = _objects[index];
            if (rawObject == null)
            {
                throw new QueryCatPluginException(ErrorType.INVALID_OBJECT, Resources.Errors.Object_Released);
            }
            var obj = rawObject as T;
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

    public void Remove(int index)
    {
        lock (_objLock)
        {
            if (_objects[index] is IRowsSource rowsSource)
            {
                AsyncUtils.RunSync(() => rowsSource.CloseAsync());
            }
            if (_objects[index] is IDisposable disposable)
            {
                disposable.Dispose();
            }
            _objects[index] = null;
        }
    }

    public void Clean()
    {
        var totalObjects = 0;
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
