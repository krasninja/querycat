using Xunit;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Types;

namespace QueryCat.UnitTests.Fetch;

/// <summary>
/// Tests for <see cref="KeysRowsInput" />.
/// </summary>
public class KeysRowsInputTests
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
        ];

        public SampleKeysRowsInput()
        {
            AddKeyColumns(
            [
                new KeyColumn(0),
                new KeyColumn(2),
                new KeyColumn(3, false, [VariantValue.Operation.Less, VariantValue.Operation.Greater]),
            ]);
        }

        /// <inheritdoc />
        public override ErrorCode ReadValue(int columnIndex, out VariantValue value)
        {
            value = default;
            return ErrorCode.OK;
        }
    }

    [Fact]
    public void SetKeyColumnValue_NoSetColumn_ShouldReturnNull()
    {
        // Arrange.
        var input = new SampleKeysRowsInput();

        // Assert.
        var value = input.GetKeyColumnValue("col0", VariantValue.Operation.Equals);
        Assert.True(value.IsNull);
    }

    [Fact]
    public void SetKeyColumnValue_SetTwice_ShouldChangeValue()
    {
        // Arrange.
        var input = new SampleKeysRowsInput();

        // Act.
        input.SetKeyColumnValue(2, new VariantValue(10), VariantValue.Operation.Equals);
        input.SetKeyColumnValue(2, new VariantValue(12), VariantValue.Operation.Equals);

        // Assert.
        var value = input.GetKeyColumnValue("col2", VariantValue.Operation.Equals);
        Assert.Equal(12, value.AsInteger);
    }

    [Fact]
    public void SetKeyColumnValue_SetTwoOperations_ShouldSaveTwo()
    {
        // Arrange.
        var input = new SampleKeysRowsInput();

        // Act.
        input.SetKeyColumnValue(3, new VariantValue(2), VariantValue.Operation.Greater);
        input.SetKeyColumnValue(3, new VariantValue(22), VariantValue.Operation.Less);

        // Assert.
        var value1 = input.GetKeyColumnValue("col3", VariantValue.Operation.Greater);
        var value2 = input.GetKeyColumnValue("col3", VariantValue.Operation.Less);
        Assert.Equal(2, value1.AsInteger);
        Assert.Equal(22, value2.AsInteger);
        Assert.False(input.TryGetKeyColumnValue("col3", VariantValue.Operation.Equals, out _));
        Assert.True(input.TryGetKeyColumnValue("col3", VariantValue.Operation.Less, out _));
    }

    [Fact]
    public void UnsetKeyColumnValue_Call_ShouldReturnNull()
    {
        // Arrange.
        var input = new SampleKeysRowsInput();

        // Act.
        input.SetKeyColumnValue(2, new VariantValue(12), VariantValue.Operation.Equals);
        input.SetKeyColumnValue(3, new VariantValue(12), VariantValue.Operation.Greater);
        input.UnsetKeyColumnValue(2, VariantValue.Operation.Equals);

        // Assert.
        var value = input.GetKeyColumnValue("col2", VariantValue.Operation.Equals);
        Assert.True(value.IsNull);
        Assert.False(input.TryGetKeyColumnValue("col2", VariantValue.Operation.Equals, out _));
    }
}
