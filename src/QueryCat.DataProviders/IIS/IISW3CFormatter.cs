using System.ComponentModel;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.DataProviders.IIS;

// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public sealed class IISW3CFormatter : IRowsFormatter
{
    [Description("IIS W3C log files formatter.")]
    [FunctionSignature("iisw3c(): object<IRowsFormatter>")]
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    public static VariantValue IISW3C(FunctionCallInfo args) => VariantValue.CreateFromObject(new IISW3CFormatter());

    /// <inheritdoc />
    public IRowsInput OpenInput(Stream input) => new IISW3CInput(input);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(Stream output)
    {
        throw new NotImplementedException();
    }
}
