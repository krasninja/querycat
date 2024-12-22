using System;
using System.Collections;
using System.Collections.Generic;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class ThriftPluginExecutionScope : IExecutionScope
{
    private sealed class RemoteDictionary : IDictionary<string, VariantValue>
    {
        private readonly ThriftPluginExecutionScope _thriftPluginExecutionScope;

        /// <inheritdoc />
        public ICollection<string> Keys => [];

        /// <inheritdoc />
        public ICollection<VariantValue> Values => [];

        /// <inheritdoc />
        public int Count => throw new NotImplementedException();

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public VariantValue this[string key]
        {
            get
            {
                var result = AsyncUtils.RunSync(ct => _thriftPluginExecutionScope._client.GetVariableAsync(key, ct));
                return SdkConvert.Convert(result);
            }
            set
            {
                var convertedValue = SdkConvert.Convert(value);
                AsyncUtils.RunSync(ct => _thriftPluginExecutionScope._client.SetVariableAsync(key, convertedValue, ct));
            }
        }

        public RemoteDictionary(ThriftPluginExecutionScope thriftPluginExecutionScope)
        {
            _thriftPluginExecutionScope = thriftPluginExecutionScope;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, VariantValue>> GetEnumerator()
        {
            yield break;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, VariantValue> item)
        {
            this[item.Key] = item.Value;
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, VariantValue> item)
        {
            return false;
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, VariantValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, VariantValue> item)
        {
            return false;
        }

        /// <inheritdoc />
        public void Add(string key, VariantValue value) => this[key] = value;

        /// <inheritdoc />
        public bool ContainsKey(string key) => !this[key].IsNull;

        /// <inheritdoc />
        public bool Remove(string key)
        {
            this[key] = VariantValue.Null;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out VariantValue value)
        {
            value = this[key];
            return !value.IsNull;
        }
    }

    private readonly PluginsManager.Client _client;

    /// <inheritdoc />
    public IDictionary<string, VariantValue> Variables { get; }

    /// <inheritdoc />
    public IExecutionScope? Parent => null;

    public ThriftPluginExecutionScope(PluginsManager.Client client)
    {
        _client = client;
        Variables = new RemoteDictionary(this);
    }
}