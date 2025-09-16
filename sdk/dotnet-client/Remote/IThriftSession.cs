using System;
using QueryCat.Plugins.Sdk;

namespace QueryCat.Plugins.Client.Remote;

/// <summary>
/// Thrift API session.
/// </summary>
public interface IThriftSession : IDisposable
{
    /// <summary>
    /// API client.
    /// </summary>
    QueryCatIO.IAsync Client { get; }
}
