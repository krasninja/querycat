using System.ComponentModel;
using QueryCat.Backend.Core;
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
        if (blobData == null)
        {
            return VariantValue.Null;
        }
        var length = blobData.Length;
        return length > -1 ? new VariantValue(length) : VariantValue.Null;
    }

    [SafeFunction]
    [Description("Get BLOB object from a local file.")]
    [FunctionSignature("blob_from_file(path: string): blob")]
    public static VariantValue BlobFromFile(IExecutionThread thread)
    {
        var fileVar = thread.Stack.Pop();
        if (fileVar.IsNull)
        {
            return VariantValue.Null;
        }
        var file = IOFunctions.ResolveHomeDirectory(fileVar.AsString);
        if (string.IsNullOrEmpty(file))
        {
            throw new QueryCatException(Resources.Errors.PathNotDefined);
        }
        if (!File.Exists(file))
        {
            throw new QueryCatException(string.Format(Resources.Errors.FileNoExists, file));
        }

        var extension = Path.GetExtension(file);
        var blob = new StreamBlobData(
            () => File.OpenRead(file),
            IOFunctions.MimeTypesProvider.GetContentTypeByExtension(extension),
            Path.GetFileName(file)
        );
        return VariantValue.CreateFromObject(blob);
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Length);
        functionsManager.RegisterFunction(BlobFromFile);
    }
}
