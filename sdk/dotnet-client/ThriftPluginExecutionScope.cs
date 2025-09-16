using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Utils;
using QueryCat.Plugins.Sdk;
using VariantValue = QueryCat.Backend.Core.Types.VariantValue;

namespace QueryCat.Plugins.Client;

public sealed class ThriftPluginExecutionScope : IExecutionScope
{
    public const int NoScopeId = -1;

    private sealed class RemoteDictionary : IDictionary<string, VariantValue>
    {
        private readonly ThriftPluginExecutionScope _thriftPluginExecutionScope;

        /// <inheritdoc />
        public ICollection<string> Keys => GetAllVariables().Select(v => v.Name).ToArray();

        /// <inheritdoc />
        public ICollection<VariantValue> Values => GetAllVariables().Select(v => SdkConvert.Convert(v.Value)).ToArray();

        /// <inheritdoc />
        public int Count => GetAllVariables().Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public VariantValue this[string key]
        {
            get
            {
                var result = AsyncUtils.RunSync(ct =>
                    _thriftPluginExecutionScope._client.ThriftClient.GetVariableAsync(
                        _thriftPluginExecutionScope._client.Token, key, ct));
                return SdkConvert.Convert(result);
            }
            set
            {
                var convertedValue = SdkConvert.Convert(value);
                AsyncUtils.RunSync(ct
                    => _thriftPluginExecutionScope._client.ThriftClient.SetVariableAsync(
                        _thriftPluginExecutionScope._client.Token, key, convertedValue, ct));
            }
        }

        public RemoteDictionary(ThriftPluginExecutionScope thriftPluginExecutionScope)
        {
            _thriftPluginExecutionScope = thriftPluginExecutionScope;
        }

        private IReadOnlyList<ScopeVariable> GetAllVariables()
        {
            var values = AsyncUtils.RunSync(ct
                => _thriftPluginExecutionScope._client.ThriftClient.GetVariablesAsync(
                    _thriftPluginExecutionScope._client.Token,
                    scope_id: _thriftPluginExecutionScope._id,
                    ct));
            return values ?? [];
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, VariantValue>> GetEnumerator()
        {
            var values = GetAllVariables();
            foreach (var value in values)
            {
                yield return new KeyValuePair<string, VariantValue>(value.Name, SdkConvert.Convert(value.Value));
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
            this.ToArray().CopyTo(array, arrayIndex);
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

    private readonly ThriftPluginClient _client;
    private readonly int _id;
    private readonly int _parentId;

    /// <inheritdoc />
    public IDictionary<string, VariantValue> Variables { get; }

    /// <inheritdoc />
    public IExecutionScope? Parent => null;

    public ThriftPluginExecutionScope(ThriftPluginClient client, int id, int parentId)
    {
        _client = client;
        _id = id;
        _parentId = parentId;
        Variables = new RemoteDictionary(this);
    }

    /// <inheritdoc />
    public bool TryGetVariable(string name, out VariantValue value)
    {
        var currentScope = (IExecutionScope)this;
        while (currentScope != null)
        {
            if (currentScope.Variables.TryGetValue(name, out value))
            {
                return true;
            }
            currentScope = currentScope.Parent;
        }

        value = VariantValue.Null;
        return false;
    }

    /// <inheritdoc />
    public bool TrySetVariable(string name, VariantValue value)
    {
        var currentScope = (IExecutionScope)this;
        while (currentScope != null)
        {
            if (currentScope.Variables.ContainsKey(name))
            {
                currentScope.Variables[name] = value;
                return true;
            }
            currentScope = currentScope.Parent;
        }

        Variables[name] = value;
        return true;
    }
}