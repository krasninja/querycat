using System.ComponentModel;
using System.Text.RegularExpressions;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Functions;

/// <summary>
/// String functions.
/// </summary>
internal static class StringFunctions
{
    [SafeFunction]
    [Description("Convert a string to lower case.")]
    [FunctionSignature("lower(target: string): string")]
    public static VariantValue Lower(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        return new VariantValue(value.AsString.ToLower(Application.Culture));
    }

    [SafeFunction]
    [Description("Convert a string to upper case.")]
    [FunctionSignature("upper(target: string): string")]
    public static VariantValue Upper(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        return new VariantValue(value.AsString.ToUpper(Application.Culture));
    }

    [SafeFunction]
    [Description("Removes the longest string containing only characters in characters from the start of string.")]
    [FunctionSignature("ltrim(target: string, characters: string = ' '): string")]
    public static VariantValue LTrim(IExecutionThread thread)
    {
        var value = thread.Stack[0].AsString;
        var trimCharacters = thread.Stack[1].AsString;
        return new VariantValue(value.TrimStart(trimCharacters.ToArray()));
    }

    [SafeFunction]
    [Description("Removes the longest string containing only characters in characters from the end of string.")]
    [FunctionSignature("rtrim(target: string, characters: string = ' '): string")]
    public static VariantValue RTrim(IExecutionThread thread)
    {
        var value = thread.Stack[0].AsString;
        var trimCharacters = thread.Stack[1].AsString;
        return new VariantValue(value.TrimEnd(trimCharacters.ToArray()));
    }

    [SafeFunction]
    [Description("Remove the longest string consisting only of characters in characters from the start and end of string.")]
    [FunctionSignature("btrim(target: string, characters: string = ' '): string")]
    public static VariantValue BTrim(IExecutionThread thread)
    {
        var value = thread.Stack[0].AsString;
        var trimCharacters = thread.Stack[1].AsString;
        return new VariantValue(value.Trim(trimCharacters.ToArray()));
    }

    [SafeFunction]
    [Description("Extracts the substring of string starting at the start'th character, and extending for count characters if that is specified.")]
    [FunctionSignature("substr(target: string, start: integer, count?: integer): string")]
    public static VariantValue SubString(IExecutionThread thread)
    {
        var startValue = thread.Stack[1].AsInteger;
        var countValue = thread.Stack[2];
        if (!startValue.HasValue)
        {
            return VariantValue.Null;
        }

        var value = thread.Stack[0].AsString;
        var start = (int)startValue.Value - 1;
        var count = !countValue.IsNull && countValue.AsInteger.HasValue ? (int)countValue.AsInteger.Value : value.Length - start;
        return new VariantValue(StringUtils.SafeSubstring(value, start, count));
    }

    [SafeFunction]
    [Description("Number of characters in string.")]
    [FunctionSignature("length(target: string): integer")]
    [FunctionSignature("char_length(target: string): integer")]
    [FunctionSignature("character_length(target: string): integer")]
    public static VariantValue Length(IExecutionThread thread)
    {
        var value = thread.Stack.Pop().AsString;
        return new VariantValue(value.Length);
    }

    [SafeFunction]
    [Description("Convert value to string according to the given format.")]
    [FunctionSignature("to_char(args: any, fmt?: string): string")]
    public static VariantValue ToChar(IExecutionThread thread)
    {
        var arg = thread.Stack[0];
        var format = thread.Stack[1];
        return !string.IsNullOrEmpty(format)
            ? new VariantValue(arg.ToString(format))
            : new VariantValue(arg.ToString(Application.Culture));
    }

    [SafeFunction]
    [Description("Returns first starting index of the specified substring within string, or zero if it's not present.")]
    [FunctionSignature("\"position\"(substring: string, target: string): integer")]
    public static VariantValue Position(IExecutionThread thread)
    {
        var substring = thread.Stack[0].AsString;
        var target = thread.Stack[1].AsString;
        return new VariantValue(target.IndexOf(substring, StringComparison.Ordinal) + 1);
    }

    [SafeFunction]
    [Description("Replaces all occurrences in string of substring from with substring to.")]
    [FunctionSignature("replace(target: string, old: string, new: string): string")]
    public static VariantValue Replace(IExecutionThread thread)
    {
        var target = thread.Stack[0].AsString;
        var from = thread.Stack[1].AsString;
        var to = thread.Stack[2].AsString;
        return new VariantValue(target.Replace(from, to));
    }

    [SafeFunction]
    [Description("Reverses the order of the characters in the string.")]
    [FunctionSignature("reverse(target: string): string")]
    public static VariantValue Reverse(IExecutionThread thread)
    {
        var target = thread.Stack.Pop().AsString;
        var charArray = target.ToCharArray();
        Array.Reverse(charArray);
        return new VariantValue(new string(charArray));
    }

    [SafeFunction]
    [Description("Returns the character with the given code.")]
    [FunctionSignature("chr(code: integer): string")]
    public static VariantValue Chr(IExecutionThread thread)
    {
        var code = thread.Stack.Pop().AsInteger;
        return new VariantValue(Convert.ToChar(code).ToString(Application.Culture));
    }

    [SafeFunction]
    [Description("Returns true if string starts with prefix.")]
    [FunctionSignature("starts_with(target: string, prefix: string): boolean")]
    public static VariantValue StartsWith(IExecutionThread thread)
    {
        var target = thread.Stack[0].AsString;
        var prefix = thread.Stack[1].AsString;
        return new VariantValue(target.StartsWith(prefix));
    }

    [SafeFunction]
    [Description("Splits string at occurrences of delimiter and returns the n'th field (counting from one).")]
    [FunctionSignature("split_part(target: string, delimiter: string, n: integer): string")]
    public static VariantValue SplitPart(IExecutionThread thread)
    {
        var target = thread.Stack[0].AsString;
        var delimiter = thread.Stack[1].AsString;
        var n = thread.Stack[2].AsInteger;

        if (!n.HasValue)
        {
            return VariantValue.Null;
        }

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
        return new VariantValue(split[n.Value]);
    }

    [SafeFunction]
    [Description("Splits the string at occurrences of delimiter and returns the resulting fields as a set of text rows.")]
    [FunctionSignature("string_to_table(target: string, delimiter?: string, null_string?: string := null): object<IRowsIterator>")]
    public static VariantValue StringToTable(IExecutionThread thread)
    {
        IEnumerable<string> GetSplitItems(string target, string? delimiter, string? nullString)
        {
            // If delimiter is null - return every character.
            if (delimiter == null)
            {
                foreach (var chr in target)
                {
                    yield return chr.ToString();
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

        var target = thread.Stack[0].AsString;
        string? delimiter = !thread.Stack[1].IsNull ? thread.Stack[1] : null;
        var nullString = thread.Stack[2].AsString;

        var result = GetSplitItems(target, delimiter, nullString).ToList();
        var input = EnumerableRowsInput<string>.FromSource(result,
            builder => builder.AddProperty(Column.ValueColumnTitle, p => p, "String part."));
        return VariantValue.CreateFromObject(input);
    }

    [SafeFunction]
    [Description("Returns the substring within string that matches the N'th occurrence of the regular expression pattern, or NULL.")]
    [FunctionSignature("regexp_substr(target: string, pattern: string, start?: integer = 1, n?: integer = 1, subexpr?: integer = 1): string")]
    public static VariantValue RegexpSubstring(IExecutionThread thread)
    {
        var startValue = thread.Stack[2].AsInteger;
        var nValue = thread.Stack[3].AsInteger;
        var subexprValue = thread.Stack[4].AsInteger;
        if (!startValue.HasValue || !nValue.HasValue || !subexprValue.HasValue)
        {
            return VariantValue.Null;
        }

        var target = thread.Stack[0].AsString;
        var pattern = thread.Stack[1].AsString;
        var start = (int)startValue.Value - 1;
        var n = (int)nValue.Value - 1;
        var subexpr = (int)subexprValue.Value - 1;

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

    [SafeFunction]
    [Description("Returns the number of times the regular expression pattern matches in the string.")]
    [FunctionSignature("regexp_count(target: string, pattern: string, start?: integer = 1): string")]
    public static VariantValue RegexpCount(IExecutionThread thread)
    {
        var startValue = thread.Stack[2].AsInteger;
        if (!startValue.HasValue)
        {
            return VariantValue.Null;
        }

        var target = thread.Stack[0].AsString;
        var pattern = thread.Stack[1].AsString;
        var start = (int)startValue.Value - 1;

        target = StringUtils.SafeSubstring(target, start);
        var matches = Regex.Matches(target, pattern, RegexOptions.Compiled);
        return new VariantValue(matches.Count);
    }

    [SafeFunction]
    [Description("Provides substitution of new text for substrings that match regular expression patterns.")]
    [FunctionSignature("regexp_replace(target: string, pattern: string, replacement: string, start?: integer = 1): string")]
    public static VariantValue RegexpReplace(IExecutionThread thread)
    {
        var startValue = thread.Stack[3].AsInteger;
        if (!startValue.HasValue)
        {
            return VariantValue.Null;
        }

        var target = thread.Stack[0].AsString;
        var pattern = thread.Stack[1].AsString;
        var replacement = thread.Stack[2].AsString;
        var start = (int)startValue.Value - 1;

        target = StringUtils.SafeSubstring(target, start);
        var result = Regex.Replace(target, pattern, replacement);
        return new VariantValue(result);
    }

    [SafeFunction]
    [Description("Converts a blob to a base64 encoded string.")]
    [FunctionSignature("to_base64(target: blob): string")]
    [FunctionSignature("base64(target: blob): string")]
    public static async ValueTask<VariantValue> ToBase64(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var target = thread.Stack.Pop().AsBlob;
        if (target == null)
        {
            return VariantValue.Null;
        }

        await using var stream = target.GetStream();
        var buffer = new byte[target.Length];
        await stream.ReadExactlyAsync(buffer, cancellationToken);
        var result = Convert.ToBase64String(buffer);
        return new VariantValue(result);
    }

    [SafeFunction]
    [Description("Converts a base64 encoded string to a character string (BLOB).")]
    [FunctionSignature("from_base64(target: string): blob")]
    public static VariantValue FromBase64(IExecutionThread thread)
    {
        var target = thread.Stack.Pop().AsString;

        var bytes = Convert.FromBase64String(target);
        var blob = new StreamBlobData(bytes);
        return new VariantValue(blob);
    }

    internal static RegexOptions FlagsToRegexOptions(string? flags)
    {
        var options = RegexOptions.None;
        foreach (var flag in (flags ?? string.Empty).ToLower(Application.Culture))
        {
            switch (flag)
            {
                case 'i':
                    options |= RegexOptions.IgnoreCase;
                    break;
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
        functionsManager.RegisterFunction(ToBase64);
        functionsManager.RegisterFunction(FromBase64);
    }
}
