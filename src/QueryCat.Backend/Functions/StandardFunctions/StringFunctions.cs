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
        var start = (int)args.GetAt(1).AsInteger - 1;
        var count = !args.GetAt(2).IsNull ? (int)args.GetAt(2).AsInteger : value.Length - start;

        if (start < 0)
        {
            start = 0;
        }
        else if (start > value.Length)
        {
            return new VariantValue(string.Empty);
        }
        if (count == 0)
        {
            count = value.Length;
        }
        else if (start + count > value.Length)
        {
            count = value.Length - start;
        }

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

    [Description("Returns first starting index of the specified substring within string, or zero if it's not present.")]
    [FunctionSignature("[position](substring: string, target: string): integer")]
    public static VariantValue Position(FunctionCallInfo args)
    {
        var substring = args.GetAt(0).AsString;
        var target = args.GetAt(1).AsString;
        return new VariantValue(target.IndexOf(substring, StringComparison.InvariantCulture) + 1);
    }

    [Description("Replaces all occurrences in string of substring from with substring to.")]
    [FunctionSignature("replace(target: string, old: string, new: string): string")]
    public static VariantValue Replace(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var from = args.GetAt(1).AsString;
        var to = args.GetAt(2).AsString;
        return new VariantValue(target.Replace(from, to));
    }

    [Description("Reverses the order of the characters in the string.")]
    [FunctionSignature("reverse(target: string): string")]
    public static VariantValue Reverse(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var charArray = target.ToCharArray();
        Array.Reverse(charArray);
        return new VariantValue(new string(charArray));
    }

    [Description("Returns the character with the given code.")]
    [FunctionSignature("chr(code: integer): string")]
    public static VariantValue Chr(FunctionCallInfo args)
    {
        var code = args.GetAt(0).AsInteger;
        return new VariantValue(Convert.ToChar(code).ToString());
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
        functionsManager.RegisterFunction(Position);
        functionsManager.RegisterFunction(Replace);
        functionsManager.RegisterFunction(Reverse);
        functionsManager.RegisterFunction(Chr);
    }
}
