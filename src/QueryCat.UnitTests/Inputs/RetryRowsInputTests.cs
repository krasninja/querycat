using Xunit;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Inputs;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.UnitTests.Inputs;

/// <summary>
/// Tests for <see cref="RetryRowsInput" />.
/// </summary>
public class RetryRowsInputTests
{
    private sealed class InputWithException : IRowsInput
    {
        private int _seed = 20250216;
        private int _calls = 0;

        public int ExceptionsCount { get; private set; }

        /// <inheritdoc />
        public Column[] Columns =>
        [
            new("id", DataType.Integer),
        ];

        /// <inheritdoc />
        public string[] UniqueKey => [];

        /// <inheritdoc />
        public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

        /// <inheritdoc />
        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public Task ResetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        public ErrorCode ReadValue(int columnIndex, out VariantValue value)
        {
            value = new VariantValue(_seed);
            return ErrorCode.OK;
        }

        /// <inheritdoc />
        public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
        {
            _calls++;
            if (_calls < 3)
            {
                ExceptionsCount++;
                throw new Exception($"Attempt {_calls}.");
            }
            _seed++;
            return ValueTask.FromResult(_calls < 10);
        }

        /// <inheritdoc />
        public void Explain(IndentedStringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("InputWithException");
        }

        /// <inheritdoc />
        public IReadOnlyList<KeyColumn> GetKeyColumns() => [];

        /// <inheritdoc />
        public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
        {
        }

        /// <inheritdoc />
        public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
        {
        }
    }

    [Fact]
    public async Task ReadNext_InputWithException_ShouldProceed()
    {
        // Arrange.
        var inputWithException = new InputWithException();
        var inputWithRetry = new RetryRowsInput(inputWithException, 3, TimeSpan.FromMilliseconds(20));
        await inputWithRetry.OpenAsync();
        var rowsIterator = new RowsInputIterator(inputWithRetry);

        // Act.
        var frame = await rowsIterator.ToFrameAsync();

        // Assert.
        Assert.Equal(2, inputWithException.ExceptionsCount);
        Assert.Equal(7, frame.TotalRows);
    }
}
