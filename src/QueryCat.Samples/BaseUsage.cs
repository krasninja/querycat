using System.Globalization;
using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Samples;

/// <summary>
/// Just the base class to make all examples common.
/// </summary>
internal abstract class BaseUsage
{
    /// <summary>
    /// Run the example.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    public abstract Task RunAsync();

    protected async Task<string> SerializeValueToStringAsync(VariantValue value, CancellationToken cancellationToken = default)
    {
        if (value.Type == DataType.Object)
        {
            var sb = new StringBuilder();

            var obj = value.AsObject;
            if (obj is IRowsInput rowsInput)
            {
                await new TextTableOutput(sb).WriteAsync(
                    rowsInput,
                    adjustColumnsLengths: true,
                    cancellationToken: cancellationToken);
                return sb.ToString();
            }
            else if (obj is IRowsIterator iterator)
            {
                await new TextTableOutput(sb).WriteAsync(iterator, adjustColumnsLengths: true, cancellationToken: cancellationToken);
                return sb.ToString();
            }
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }
}
