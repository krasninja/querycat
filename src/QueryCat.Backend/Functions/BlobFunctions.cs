using System.ComponentModel;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// BLOB object functions.
/// </summary>
internal static class BlobFunctions
{
    [SafeFunction]
    [Description("Number of bytes in BLOB object.")]
    [FunctionSignature("length(target: blob): integer")]
    public static VariantValue Length(IExecutionThread thread)
    {
        var blobData = thread.Stack[0].AsBlob;
        return blobData != null ? new VariantValue(blobData.Length) : VariantValue.Null;
    }

    [Description("Get BLOB object from a local file.")]
    [FunctionSignature("blob_from_file(path: string): blob")]
    public static VariantValue BlobFromFile(IExecutionThread thread)
    {
        var file = thread.Stack.Pop().AsString;
        var extension = Path.GetExtension(file);
        var blob = new StreamBlobData(
            () => File.OpenRead(file),
            IOFunctions.MimeTypesProvider.GetContentTypeByExtension(extension));
        return VariantValue.CreateFromObject(blob);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Length);
        functionsManager.RegisterFunction(BlobFromFile);
    }
}
