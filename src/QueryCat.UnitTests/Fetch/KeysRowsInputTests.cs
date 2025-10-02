using Xunit;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.UnitTests.Fetch;

/// <summary>
/// Tests for <see cref="KeysRowsInput" />.
/// </summary>
public sealed class KeysRowsInputTests : IDisposable
{
    private sealed class SampleKeysRowsInput : KeysRowsInput
    {
        /// <inheritdoc />
        public override Column[] Columns { get; protected set; } =
        [
            new("col0", DataType.Integer),
            new("col1", DataType.Integer),
            new("col2", DataType.Integer),
            new("col3", DataType.Integer),
            new("col4", DataType.String),
            new("col5", DataType.Integer),
        ];

        public SampleKeysRowsInput()
        {
            AddKeyColumns(
            [
                new KeyColumn(0),
                new KeyColumn(2),
                new KeyColumn(3, false, [VariantValue.Operation.Less, VariantValue.Operation.Greater]),
                new KeyColumn(4, false, [VariantValue.Operation.Equals]),
                new KeyColumn(4, false, [VariantValue.Operation.Like]),
                new KeyColumn(5, true, [VariantValue.Operation.Equals]),
            ]);
        }

        /// <inheritdoc />
        public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
        {
            value = default;
            return ErrorCode.OK;
        }
    }

    private readonly SampleKeysRowsInput _input = new();

    [Fact]
    public void GetKeyColumnValue_NotSetColumn_ShouldReturnNull()
    {
        // Assert.
        var value = _input.GetKeyColumnValue("col0", VariantValue.Operation.Equals);
        Assert.True(value.IsNull);
    }

    [Fact]
    public void SetKeyColumnValue_SetTwice_ShouldChangeValue()
    {
        // Act.
        _input.SetKeyColumnValue(2, new VariantValue(10), VariantValue.Operation.Equals);
        _input.SetKeyColumnValue(2, new VariantValue(12), VariantValue.Operation.Equals);

        // Assert.
        var value = _input.GetKeyColumnValue("col2", VariantValue.Operation.Equals);
        Assert.Equal(12, value.AsInteger);
    }

    [Fact]
    public void SetKeyColumnValue_SetTwoOperations_ShouldSaveTwo()
    {
        // Act.
        _input.SetKeyColumnValue(3, new VariantValue(2), VariantValue.Operation.Greater);
        _input.SetKeyColumnValue(3, new VariantValue(22), VariantValue.Operation.Less);

        // Assert.
        var value1 = _input.GetKeyColumnValue("col3", VariantValue.Operation.Greater);
        var value2 = _input.GetKeyColumnValue("col3", VariantValue.Operation.Less);
        Assert.Equal(2, value1.AsInteger);
        Assert.Equal(22, value2.AsInteger);
        Assert.False(_input.TryGetKeyColumnValue("col3", VariantValue.Operation.Equals, out _));
        Assert.True(_input.TryGetKeyColumnValue("col3", VariantValue.Operation.Less, out _));
    }

    [Fact]
    public void UnsetKeyColumnValue_Call_ShouldReturnNull()
    {
        // Act.
        _input.SetKeyColumnValue(2, new VariantValue(12), VariantValue.Operation.Equals);
        _input.SetKeyColumnValue(3, new VariantValue(12), VariantValue.Operation.Greater);
        _input.UnsetKeyColumnValue(2, VariantValue.Operation.Equals);

        // Assert.
        var value = _input.GetKeyColumnValue("col2", VariantValue.Operation.Equals);
        Assert.True(value.IsNull);
        Assert.False(_input.TryGetKeyColumnValue("col2", VariantValue.Operation.Equals, out _));
    }

    [Fact]
    public void SetKeyColumnValue_EqualsAndLike_ShouldReturnCorrect()
    {
        // Act.
        _input.SetKeyColumnValue(4, new VariantValue("text"), VariantValue.Operation.Equals);
        _input.SetKeyColumnValue(4, new VariantValue("%text%"), VariantValue.Operation.Like);

        // Assert.
        Assert.Equal("text", _input.GetKeyColumnValue("col4", VariantValue.Operation.Equals));
        Assert.Equal("%text%", _input.GetKeyColumnValue("col4", VariantValue.Operation.Like));
    }

    [Fact]
    public void GetKeyColumnValue_RequiredColumn_ShouldThrowException()
    {
        // Assert.
        Assert.Throws<QueryMissedCondition>(() => _input.GetKeyColumnValue("col5", VariantValue.Operation.Equals));
    }

    [Fact]
    public void SetKeyColumnValue_NoKeyColumn_ShouldThrowException()
    {
        // Assert.
        Assert.Throws<InvalidOperationException>(() =>
        {
            _input.SetKeyColumnValue(1, new VariantValue("text"), VariantValue.Operation.Equals);
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _input.Dispose();
    }
}
