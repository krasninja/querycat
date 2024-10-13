using Xunit;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Commands.Select.Inputs;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.IntegrationTests.Storage;

/// <summary>
/// Tests for <see cref="CacheRowsInput" />.
/// </summary>
public class CacheRowsInputTests
{
    [Fact]
    public void RepeatRead_Data_ShouldReuseCache()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame.AddRow("Sergey");
        rowsFrame.AddRow("Anna");
        var cacheRowsInput = new CacheRowsInput(NullExecutionThread.Instance, new RowsIteratorInput(rowsFrame.GetIterator()));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        iterator.MoveNext(); // 2 total and 1 cache read.
        iterator.MoveNext(); // 2 total and 1 cache read.
        iterator.Reset();
        iterator.MoveNext(); // 1 total and 1 cache read.
        iterator.MoveNext(); // 1 total and 1 cache read.

        // Assert.
        Assert.Equal(2, cacheRowsInput.InputReads);
        Assert.Equal(4, cacheRowsInput.CacheReads);
    }

    [Fact]
    public void RepeatRead_Data_ShouldContinueUseCache()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame.AddRow("Ivan");
        rowsFrame.AddRow("Marina");
        rowsFrame.AddRow("Pasha");
        var cacheRowsInput = new CacheRowsInput(NullExecutionThread.Instance,
            new RowsIteratorInput(rowsFrame.GetIterator()));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        iterator.MoveNext(); // 2 total and 1 cache read.
        iterator.MoveNext(); // 2 total and 1 cache read.
        iterator.Reset();
        iterator.MoveNext(); // 2 total and 1 cache read.
        iterator.MoveNext(); // 1 total and 1 cache read.
        iterator.MoveNext(); // 1 total and 1 cache read.

        // Assert.
        Assert.Equal(3, cacheRowsInput.InputReads);
        Assert.Equal(5, cacheRowsInput.CacheReads);
    }

    [Fact]
    public void RepeatRead_TwoDataSet_ShouldUseSeparateCache()
    {
        // Arrange.
        var rowsFrame1 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame1.AddRow("Ivan");
        var rowsFrame2 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame2.AddRow("Marina");
        var input1 = new RowsIteratorInput(rowsFrame1.GetIterator(), "input1");
        var input2 = new RowsIteratorInput(rowsFrame2.GetIterator(), "input2");
        var proxyInput = new ProxyRowsInput(input1);
        var cacheRowsInput = new CacheRowsInput(NullExecutionThread.Instance, proxyInput);
        cacheRowsInput.QueryContext = new SelectInputQueryContext(cacheRowsInput);
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        proxyInput.SetInput(input1);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input2);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input1);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read cache.

        // Assert.
        Assert.Equal(2, cacheRowsInput.InputReads);
        Assert.Equal(2, cacheRowsInput.TotalCacheEntries);
        Assert.Equal(3, cacheRowsInput.CacheReads);
    }

    [Fact]
    public void RepeatRead_IncompleteReadCache_ShouldDiscardIncompleteCache()
    {
        // Arrange.
        var rowsFrame1 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame1.AddRow("Ivan");
        rowsFrame1.AddRow("Vladimir");
        var rowsFrame2 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame2.AddRow("Marina");
        var input1 = new RowsIteratorInput(rowsFrame1.GetIterator(), "input1");
        var input2 = new RowsIteratorInput(rowsFrame2.GetIterator(), "input2");
        var proxyInput = new ProxyRowsInput(input1);
        var cacheRowsInput = new CacheRowsInput(NullExecutionThread.Instance, proxyInput);
        cacheRowsInput.QueryContext = new SelectInputQueryContext(cacheRowsInput);
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        proxyInput.SetInput(input1);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.

        proxyInput.SetInput(input2);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input1);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.

        proxyInput.SetInput(input2);
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read cache.

        // Assert.
        Assert.Equal(3, cacheRowsInput.InputReads);
        Assert.Equal(1, cacheRowsInput.TotalCacheEntries);
        Assert.Equal(4, cacheRowsInput.CacheReads);
    }
}
