using System.ComponentModel;
using QueryCat.Backend.Abstractions.Functions;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// The provider implements simple read and write functions. The actual function will
/// be resolved by URI schema.
/// </summary>
internal static class GenericInputOutput
{
    [Description("Read data from a URI.")]
    [FunctionSignature("read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Read(FunctionCallInfo args)
    {
        var uri = args.GetAt(0).AsString;
        if (uri.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith(@"https://", StringComparison.OrdinalIgnoreCase))
        {
            return CurlInput.WGet(args);
        }

        return FileInputOutput.ReadFile(args);
    }

    [Description("Write data to a URI.")]
    [FunctionSignature("write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue Write(FunctionCallInfo args)
    {
        return FileInputOutput.WriteFile(args);
    }
}
