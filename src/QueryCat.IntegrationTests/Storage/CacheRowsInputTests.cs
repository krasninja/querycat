using Xunit;
using QueryCat.Backend.Commands.Select;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

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
        var cacheRowsInput = new CacheRowsInput(new RowsIteratorInput(rowsFrame.GetIterator()));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        iterator.MoveNext();
        iterator.MoveNext();
        iterator.Reset();
        iterator.MoveNext();
        iterator.MoveNext();

        // Assert.
        Assert.Equal(4, cacheRowsInput.TotalReads);
        Assert.Equal(2, cacheRowsInput.CacheReads);
    }

    [Fact]
    public void RepeatRead_Data_ShouldContinueUseCache()
    {
        // Arrange.
        var rowsFrame = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame.AddRow("Ivan");
        rowsFrame.AddRow("Marina");
        rowsFrame.AddRow("Pasha");
        var cacheRowsInput = new CacheRowsInput(new RowsIteratorInput(rowsFrame.GetIterator()));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        iterator.MoveNext();
        iterator.MoveNext();
        iterator.Reset();
        iterator.MoveNext();
        iterator.MoveNext();
        iterator.MoveNext();

        // Assert.
        Assert.Equal(5, cacheRowsInput.TotalReads);
        Assert.Equal(2, cacheRowsInput.CacheReads);
    }

    [Fact]
    public void RepeatRead_TwoDataSet_ShouldUseSeparateCache()
    {
        // Arrange.
        var rowsFrame1 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame1.AddRow("Ivan");
        var rowsFrame2 = new RowsFrame(new Column("Name", DataType.String));
        rowsFrame2.AddRow("Marina");
        var input1 = new RowsIteratorInput(rowsFrame1.GetIterator());
        var input2 = new RowsIteratorInput(rowsFrame2.GetIterator());
        var proxyInput = new ProxyRowsInput(input1);
        var cacheRowsInput = new CacheRowsInput(proxyInput);
        cacheRowsInput.SetContext(new SelectInputQueryContext(cacheRowsInput));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        proxyInput.SetInput(input1, nameof(input1));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input2, nameof(input2));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input1, nameof(input1));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read cache.

        // Assert.
        Assert.Equal(3, cacheRowsInput.TotalReads);
        Assert.Equal(2, cacheRowsInput.TotalCacheEntries);
        Assert.Equal(1, cacheRowsInput.CacheReads);
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
        var input1 = new RowsIteratorInput(rowsFrame1.GetIterator());
        var input2 = new RowsIteratorInput(rowsFrame2.GetIterator());
        var proxyInput = new ProxyRowsInput(input1);
        var cacheRowsInput = new CacheRowsInput(proxyInput);
        cacheRowsInput.SetContext(new SelectInputQueryContext(cacheRowsInput));
        var iterator = cacheRowsInput.AsIterable(autoFetch: true);

        // Act.
        proxyInput.SetInput(input1, nameof(input1));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.

        proxyInput.SetInput(input2, nameof(input2));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.
        iterator.MoveNext(); // Read EOF.

        proxyInput.SetInput(input1, nameof(input1));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read.

        proxyInput.SetInput(input2, nameof(input2));
        cacheRowsInput.Reset();
        iterator.MoveNext(); // Read cache.

        // Assert.
        Assert.Equal(4, cacheRowsInput.TotalReads);
        Assert.Equal(1, cacheRowsInput.TotalCacheEntries);
        Assert.Equal(1, cacheRowsInput.CacheReads);
    }
}