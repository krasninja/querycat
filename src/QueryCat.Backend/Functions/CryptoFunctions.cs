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
        var value = thread.Stack.Pop();
        if (value.IsNull)
        {
            return VariantValue.Null;
        }
        var textData = Encoding.UTF8.GetBytes(value.AsString);
        return new VariantValue(ToHexLower(MD5.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA1 hash of the given data.")]
    [FunctionSignature("sha1(\"text\": string): string")]
    public static VariantValue Sha1(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        if (value.IsNull)
        {
            return VariantValue.Null;
        }
        var textData = Encoding.UTF8.GetBytes(value.AsString);
        return new VariantValue(ToHexLower(SHA1.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA256 hash of the given data.")]
    [FunctionSignature("sha256(\"text\": string): string")]
    public static VariantValue Sha256(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        if (value.IsNull)
        {
            return VariantValue.Null;
        }
        var textData = Encoding.UTF8.GetBytes(value.AsString);
        return new VariantValue(ToHexLower(SHA256.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA384 hash of the given data.")]
    [FunctionSignature("sha384(\"text\": string): string")]
    public static VariantValue Sha384(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        if (value.IsNull)
        {
            return VariantValue.Null;
        }
        var textData = Encoding.UTF8.GetBytes(value.AsString);
        return new VariantValue(ToHexLower(SHA384.HashData(textData)));
    }

    [SafeFunction]
    [Description("Computes the SHA512 hash of the given data.")]
    [FunctionSignature("sha512(\"text\": string): string")]
    public static VariantValue Sha512(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        if (value.IsNull)
        {
            return VariantValue.Null;
        }
        var textData = Encoding.UTF8.GetBytes(value.AsString);
        return new VariantValue(ToHexLower(SHA512.HashData(textData)));
    }

    private static string ToHexLower(ReadOnlySpan<byte> bytes)
    {
#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#else
        return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
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
