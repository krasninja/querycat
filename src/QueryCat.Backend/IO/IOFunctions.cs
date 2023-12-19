using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.IO;

internal static partial class IOFunctions
{
    private const string ContentTypeHeader = "Content-Type";

    [Description("Read data from a URI.")]
    [FunctionSignature("read(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue Read(FunctionCallInfo args)
    {
        var uri = args.GetAt(0).AsString;
        if (uri.StartsWith(@"http://", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith(@"https://", StringComparison.OrdinalIgnoreCase))
        {
            return Curl_Curl(args);
        }

        return File_ReadFile(args);
    }

    [Description("Write data to a URI.")]
    [FunctionSignature("write(uri: string, fmt?: object<IRowsFormatter>): object<IRowsOutput>")]
    public static VariantValue Write(FunctionCallInfo args)
    {
        return File_WriteFile(args);
    }

    [Description("Reads data from a string.")]
    [FunctionSignature("read_text([text]: string, fmt: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue ReadString(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var formatter = (IRowsFormatter)args.GetAt(1).AsObject!;

        var stringStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
        return VariantValue.CreateFromObject(formatter.OpenInput(stringStream));
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Stdio_Stdout);
        functionsManager.RegisterFunction(Stdio_Stdin);

        functionsManager.RegisterFunction(Read);
        functionsManager.RegisterFunction(Write);

        functionsManager.RegisterFunction(File_ReadFile);
        functionsManager.RegisterFunction(File_WriteFile);

        functionsManager.RegisterFunction(ReadString);

        functionsManager.RegisterFunction(Curl_Curl);
    }
}
