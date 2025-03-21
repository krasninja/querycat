using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Hashing and encryption functions.
/// </summary>
internal static class CryptoFunctions
{
    [SafeFunction]
    [Description("Computes the MD5 hash of the given data.")]
    [FunctionSignature("md5(\"text\": string): string")]
    public static VariantValue Md5(IExecutionThread thread)
    {
        var text = thread.Stack.Pop().AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(MD5.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA1 hash of the given data.")]
    [FunctionSignature("sha1(\"text\": string): string")]
    public static VariantValue Sha1(IExecutionThread thread)
    {
        var text = thread.Stack.Pop().AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA1.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA256 hash of the given data.")]
    [FunctionSignature("sha256(\"text\": string): string")]
    public static VariantValue Sha256(IExecutionThread thread)
    {
        var text = thread.Stack.Pop().AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA256.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA384 hash of the given data.")]
    [FunctionSignature("sha384(\"text\": string): string")]
    public static VariantValue Sha384(IExecutionThread thread)
    {
        var text = thread.Stack.Pop().AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA384.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA512 hash of the given data.")]
    [FunctionSignature("sha512(\"text\": string): string")]
    public static VariantValue Sha512(IExecutionThread thread)
    {
        var text = thread.Stack.Pop().AsString;
        var textData = Encoding.UTF8.GetBytes(text);
        return new VariantValue(Convert.ToHexString(SHA512.HashData(textData)));
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Md5);
        functionsManager.RegisterFunction(Sha1);
        functionsManager.RegisterFunction(Sha256);
        functionsManager.RegisterFunction(Sha384);
        functionsManager.RegisterFunction(Sha512);
    }
}
