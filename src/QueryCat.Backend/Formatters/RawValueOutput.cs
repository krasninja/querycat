using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Formatters;

/// <summary>
/// Outputs raw value into a stream.
/// </summary>
internal sealed class RawValueOutput : RowsOutput
{
    private readonly IBlobData _blobData;

    public RawValueOutput(IBlobData blobData)
    {
        _blobData = blobData;
    }

    /// <inheritdoc />
    public override Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public override Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    protected override async ValueTask<ErrorCode> OnWriteAsync(VariantValue[] values, CancellationToken cancellationToken = default)
    {
        await using var outputStream = _blobData.GetStream();
        if (!outputStream.CanWrite)
        {
            return ErrorCode.NotSupported;
        }

        foreach (var value in values)
        {
            switch (value.Type)
            {
                case DataType.Blob:
                    {
                        await using var inputStream = value.AsBlobUnsafe.GetStream();
                        await inputStream.CopyToAsync(outputStream, cancellationToken);
                        await inputStream.FlushAsync(cancellationToken);
                        inputStream.Close();
                        break;
                    }
                case DataType.Null:
                    break;
                default:
                    {
                        var inputBlob = value.Cast(DataType.Blob).AsBlobUnsafe;
                        await using var inputStream = inputBlob.GetStream();
                        await inputStream.CopyToAsync(outputStream, cancellationToken);
                        await inputStream.FlushAsync(cancellationToken);
                        inputStream.Close();
                        break;
                    }
            }
        }

        return ErrorCode.OK;
    }
}
