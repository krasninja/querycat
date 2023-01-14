using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Hashing and encryption functions.
/// </summary>
public static class CryptoFunctions
{
    [Description("Computes the MD5 hash of the given data.")]
    [FunctionSignature("md5(text: string): string")]
    public static VariantValue Md5(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(MD5.HashData(textData)));
    }

    [Description("Computes the SHA1 hash of the given data.")]
    [FunctionSignature("sha1(text: string): string")]
    public static VariantValue Sha1(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA1.HashData(textData)));
    }

    [Description("Computes the SHA256 hash of the given data.")]
    [FunctionSignature("sha256(text: string): string")]
    public static VariantValue Sha256(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA256.HashData(textData)));
    }

    [Description("Computes the SHA384 hash of the given data.")]
    [FunctionSignature("sha384(text: string): string")]
    public static VariantValue Sha384(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA384.HashData(textData)));
    }

    [Description("Computes the SHA512 hash of the given data.")]
    [FunctionSignature("sha512(text: string): string")]
    public static VariantValue Sha512(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA512.HashData(textData)));
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Md5);
        functionsManager.RegisterFunction(Sha1);
        functionsManager.RegisterFunction(Sha256);
        functionsManager.RegisterFunction(Sha384);
        functionsManager.RegisterFunction(Sha512);
    }
}
