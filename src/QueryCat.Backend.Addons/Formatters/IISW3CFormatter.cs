using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Addons.Formatters;

// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public sealed class IISW3CFormatter : IRowsFormatter
{
    [SafeFunction]
    [Description("IIS W3C log files formatter.")]
    [FunctionSignature("iisw3c(): object<IRowsFormatter>")]
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    public static VariantValue IISW3C(IExecutionThread thread)
    {
        return VariantValue.CreateFromObject(new IISW3CFormatter());
    }

    /// <inheritdoc />
    public IRowsInput OpenInput(IBlobData blob, string? key = null) => new IISW3CInput(blob.GetStream(), key);

    /// <inheritdoc />
    public IRowsOutput OpenOutput(IBlobData blob) => throw new NotImplementedException();
}
