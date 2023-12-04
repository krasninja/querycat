using System.ComponentModel;
using System.Text.RegularExpressions;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// String functions.
/// </summary>
internal static class StringFunctions
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
        return new VariantValue(StringUtils.SafeSubstring(value, start, count));
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

    [Description("Returns true if string starts with prefix.")]
    [FunctionSignature("starts_with(target: string, prefix: string): boolean")]
    public static VariantValue StartsWith(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var prefix = args.GetAt(1).AsString;
        return new VariantValue(target.StartsWith(prefix));
    }

    [Description("Splits string at occurrences of delimiter and returns the n'th field (counting from one).")]
    [FunctionSignature("split_part(target: string, delimiter: string, n: integer): string")]
    public static VariantValue SplitPart(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var delimiter = args.GetAt(1).AsString;
        var n = args.GetAt(2).AsInteger;

        var split = target.Split(delimiter);
        if (n < 0)
        {
            n = split.Length + n + 1;
        }
        n--;
        if (n < 0 || n >= split.Length)
        {
            return VariantValue.FalseValue;
        }
        return new VariantValue(split[n]);
    }

    [Description("Splits the string at occurrences of delimiter and returns the resulting fields as a set of text rows.")]
    [FunctionSignature("string_to_table(target: string, delimiter?: string, null_string?: string := null): object<IRowsIterator>")]
    public static VariantValue StringToTable(FunctionCallInfo args)
    {
        IEnumerable<VariantValue> GetSplitItems(string target, string? delimiter, string? nullString)
        {
            // If delimiter is null - return every character.
            if (delimiter == null)
            {
                foreach (var chr in target)
                {
                    yield return new VariantValue(chr.ToString());
                }
                yield break;
            }

            // If delimiter is empty string - return the whole line.
            if (delimiter.Length == 0)
            {
                yield return new VariantValue(target);
                yield break;
            }

            int currentIndex, prevIndex = 0;
            string part;
            while ((currentIndex = target.IndexOf(delimiter, prevIndex, StringComparison.Ordinal)) > -1)
            {
                part = target.Substring(prevIndex, currentIndex - prevIndex);
                yield return part != nullString ? new VariantValue(part) : VariantValue.Null;
                prevIndex = currentIndex + delimiter.Length;
            }

            part = target.Substring(prevIndex);
            yield return part != nullString ? new VariantValue(target.Substring(prevIndex)) : VariantValue.Null;
        }

        var target = args.GetAt(0).AsString;
        string? delimiter = !args.GetAt(1).IsNull ? args.GetAt(1) : null;
        var nullString = args.GetAt(2).AsString;

        var result = GetSplitItems(target, delimiter, nullString).ToList();
        var iterator = new ClassRowsIterator<VariantValue>(
            new[]
            {
                new Column("value", DataType.String, "String part."),
            },
            new Func<VariantValue, VariantValue>[]
            {
                part => part,
            },
            result);
        return VariantValue.CreateFromObject(iterator);
    }

    [Description("Returns the substring within string that matches the N'th occurrence of the regular expression pattern, or NULL.")]
    [FunctionSignature("regexp_substr(target: string, pattern: string, start?: integer = 1, n?: integer = 1, subexpr?: integer = 1): string")]
    public static VariantValue RegexpSubstring(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var pattern = args.GetAt(1).AsString;
        var start = (int)args.GetAt(2).AsInteger - 1;
        var n = (int)args.GetAt(3).AsInteger - 1;
        var subexpr = (int)args.GetAt(4).AsInteger - 1;

        target = StringUtils.SafeSubstring(target, start);
        var matches = Regex.Matches(target, pattern, RegexOptions.Compiled);
        if (n < 0 || n > matches.Count - 1)
        {
            return VariantValue.Null;
        }
        var match = matches[n];
        if (subexpr < 0 || subexpr > match.Groups.Count - 1)
        {
            return VariantValue.Null;
        }
        return new VariantValue(matches[n].Groups[subexpr].Value);
    }

    [Description("Returns the number of times the regular expression pattern matches in the string.")]
    [FunctionSignature("regexp_count(target: string, pattern: string, start?: integer = 1): string")]
    public static VariantValue RegexpCount(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var pattern = args.GetAt(1).AsString;
        var start = (int)args.GetAt(2).AsInteger - 1;

        target = StringUtils.SafeSubstring(target, start);
        var matches = Regex.Matches(target, pattern, RegexOptions.Compiled);
        return new VariantValue(matches.Count);
    }

    [Description("Provides substitution of new text for substrings that match regular expression patterns.")]
    [FunctionSignature("regexp_replace(target: string, pattern: string, replacement: string, start?: integer = 1): string")]
    public static VariantValue RegexpReplace(FunctionCallInfo args)
    {
        var target = args.GetAt(0).AsString;
        var pattern = args.GetAt(1).AsString;
        var replacement = args.GetAt(2).AsString;
        var start = (int)args.GetAt(3).AsInteger - 1;

        target = StringUtils.SafeSubstring(target, start);
        var result = Regex.Replace(target, pattern, replacement);
        return new VariantValue(result);
    }

    internal static RegexOptions FlagsToRegexOptions(string? flags)
    {
        var options = RegexOptions.None;
        foreach (var flag in (flags ?? string.Empty).ToLowerInvariant())
        {
            switch (flag)
            {
                case 'i': options |= RegexOptions.IgnoreCase; break;
            }
        }
        return options;
    }

    public static void RegisterFunctions(IFunctionsManager functionsManager)
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
        functionsManager.RegisterFunction(StartsWith);
        functionsManager.RegisterFunction(SplitPart);
        functionsManager.RegisterFunction(StringToTable);
        functionsManager.RegisterFunction(RegexpSubstring);
        functionsManager.RegisterFunction(RegexpCount);
        functionsManager.RegisterFunction(RegexpReplace);
    }
}
