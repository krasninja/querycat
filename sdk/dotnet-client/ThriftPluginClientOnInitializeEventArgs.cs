using System;
using QueryCat.Backend.Core.Execution;

namespace QueryCat.Plugins.Client;

public sealed class ThriftPluginClientOnInitializeEventArgs : EventArgs
{
    public IExecutionThread ExecutionThread { get; }

    /// <inheritdoc />
    public ThriftPluginClientOnInitializeEventArgs(IExecutionThread executionThread)
    {
        ExecutionThread = executionThread;
    }
}
