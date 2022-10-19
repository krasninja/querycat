using System.ComponentModel;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// String functions.
/// </summary>
public static class StringFunctions
{
    [Description("Convert a string to lower case.")]
    [FunctionSignature("lower(target: string): string")]
    public static VariantValue Lower(FunctionCallInfo args)
    {
        var value = args.GetAt(0);
        return new VariantValue(value.AsString.ToLower());
    }

    [Description("Convert a string to upper case.")]
    [FunctionSignature("upper(target: string): string")]
    public static VariantValue Upper(FunctionCallInfo args)
    {
        var value = args.GetAt(0);
        return new VariantValue(value.AsString.ToUpper());
    }

    [Description("Removes the longest string containing only characters in characters from the start of string.")]
    [FunctionSignature("ltrim(target: string, characters: string = ' '): string")]
    public static VariantValue LTrim(FunctionCallInfo args)
    {
        var value = args.GetAt(0).AsString;
        var trimCharacters = args.GetAt(1).AsString;
        return new VariantValue(value.TrimStart(trimCharacters.ToArray()));
    }

    [Description("Removes the longest string containing only characters in characters from the end of string.")]
    [FunctionSignature("rtrim(target: string, characters: string = ' '): string")]
    public static VariantValue RTrim(FunctionCallInfo args)
    {
        var value = args.GetAt(0).AsString;
        var trimCharacters = args.GetAt(1).AsString;
        return new VariantValue(value.TrimEnd(trimCharacters.ToArray()));
    }

    [Description("Remove the longest string consisting only of characters in characters from the start and end of string.")]
    [FunctionSignature("btrim(target: string, characters: string = ' '): string")]
    public static VariantValue BTrim(FunctionCallInfo args)
    {
        var value = args.GetAt(0).AsString;
        var trimCharacters = args.GetAt(1).AsString;
        return new VariantValue(value.Trim(trimCharacters.ToArray()));
    }

    [Description("Extracts the substring of string starting at the start'th character, and extending for count characters if that is specified.")]
    [FunctionSignature("substr(target: string, start: integer, count?: integer): string")]
    public static VariantValue SubString(FunctionCallInfo args)
    {
        var value = args.GetAt(0).AsString;
        var start = (int)args.GetAt(1).AsInteger;
        var count = !args.GetAt(2).IsNull ? (int)args.GetAt(2).AsInteger : value.Length - start;
        return new VariantValue(value.Substring(start, count));
    }

    [Description("Number of characters in string.")]
    [FunctionSignature("length(target: string): integer")]
    [FunctionSignature("char_length(target: string): integer")]
    [FunctionSignature("character_length(target: string): integer")]
    public static VariantValue Length(FunctionCallInfo args)
    {
        var value = args.GetAt(0).AsString;
        return new VariantValue(value.Length);
    }

    [Description("Convert value to string according to the given format.")]
    [FunctionSignature("to_char(args: any, fmt?: string): string")]
    public static VariantValue ToChar(FunctionCallInfo args)
    {
        var arg = args.GetAt(0);
        var format = args.GetAt(1);
        return !string.IsNullOrEmpty(format)
            ? new VariantValue(arg.ToString(format))
            : new VariantValue(arg.ToString());
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Lower);
        functionsManager.RegisterFunction(Upper);
        functionsManager.RegisterFunction(LTrim);
        functionsManager.RegisterFunction(RTrim);
        functionsManager.RegisterFunction(BTrim);
        functionsManager.RegisterFunction(SubString);
        functionsManager.RegisterFunction(Length);
        functionsManager.RegisterFunction(ToChar);
    }
}
