using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public sealed class IISW3CFormatter : IRowsFormatter
{
    [Description("IIS W3C log files formatter.")]
    [FunctionSignature("iisw3c(): object<IRowsFormatter>")]
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    public static VariantValue IISW3C(FunctionCallInfo args)
    {
        return VariantValue.CreateFromObject(new IISW3CFormatter());
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input, string? key = null) => new IISW3CInput(input, key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new QueryCatException($"{nameof(IISW3CFormatter)} does not support output.");
    }
}
