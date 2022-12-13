using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Hashing and encryption functions.
/// </summary>
public class CryptoFunctions
{
    [Description("Computes a hash of the given data. Type is the algorithm to use.")]
    [FunctionSignature("digest(text: string, type: string): string")]
    public static VariantValue Digest(FunctionCallInfo args)
    {
        var text = args.GetAt(0).AsString;
        var type = args.GetAt(1).AsString.ToLower();
        var textData = Encoding.UTF8.GetBytes(text);
        var hash = type switch
        {
            "md5" => MD5.HashData(textData),
            "sha1" => SHA1.HashData(textData),
            "sha256" => SHA256.HashData(textData),
            "sha384" => SHA384.HashData(textData),
            "sha512" => SHA512.HashData(textData),
            _ => throw new QueryCatException("Invalid hash type."),
        };
        return new VariantValue(Convert.ToHexString(hash));
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Digest);
    }
}
