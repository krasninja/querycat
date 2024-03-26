using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Types;

internal static class StringLikeEquals
{
    private static readonly SimpleObjectPool<List<char>> _listCharPool = new(() => new List<char>());

    /// <summary>
    /// Implements SQL LIKE pattern comparision.
    /// </summary>
    /// <param name="pattern">"Like" pattern.</param>
    /// <param name="str">Target string.</param>
    /// <returns><c>True</c> if the target string matches the pattern, <c>false</c> otherwise.</returns>
    public static bool Equals(ReadOnlySpan<char> pattern, ReadOnlySpan<char> str)
    {
        // Based on this: https://stackoverflow.com/questions/5417070/c-sharp-version-of-sql-like/8583383#8583383
        bool isMatch = true,
            isWildCardOn = false,
            isCharWildCardOn = false,
            isCharSetOn = false,
            isNotCharSetOn = false,
            endOfPattern = false;
        var lastWildCard = -1;
        var patternIndex = 0;
        var set = _listCharPool.Get();
        char p = '\0';

        for (var i = 0; i < str.Length; i++)
        {
            var c = str[i];
            endOfPattern = patternIndex >= pattern.Length;
            if (!endOfPattern)
            {
                p = pattern[patternIndex];

                if (!isWildCardOn && p == '%')
                {
                    lastWildCard = patternIndex;
                    isWildCardOn = true;
                    while (patternIndex < pattern.Length
                        && pattern[patternIndex] == '%')
                    {
                        patternIndex++;
                    }
                    p = patternIndex >= pattern.Length ? '\0' : pattern[patternIndex];
                }
                else if (p == '_')
                {
                    isCharWildCardOn = true;
                    patternIndex++;
                }
                else if (p == '[')
                {
                    if (pattern[++patternIndex] == '^')
                    {
                        isNotCharSetOn = true;
                        patternIndex++;
                    }
                    else
                    {
                        isCharSetOn = true;
                    }

                    set.Clear();
                    if (pattern[patternIndex + 1] == '-' && pattern[patternIndex + 3] == ']')
                    {
                        var start = char.ToUpper(pattern[patternIndex]);
                        patternIndex += 2;
                        var end = char.ToUpper(pattern[patternIndex]);
                        if (start <= end)
                        {
                            for (char ci = start; ci <= end; ci++)
                            {
                                set.Add(ci);
                            }
                        }
                        patternIndex++;
                    }

                    while (patternIndex < pattern.Length
                           && pattern[patternIndex] != ']')
                    {
                        set.Add(pattern[patternIndex]);
                        patternIndex++;
                    }
                    patternIndex++;
                }
            }

            if (isWildCardOn)
            {
                if (char.ToUpper(c) == char.ToUpper(p))
                {
                    isWildCardOn = false;
                    patternIndex++;
                }
            }
            else if (isCharWildCardOn)
            {
                isCharWildCardOn = false;
            }
            else if (isCharSetOn || isNotCharSetOn)
            {
                bool charMatch = set.Contains(char.ToUpper(c));
                if ((isNotCharSetOn && charMatch) || (isCharSetOn && !charMatch))
                {
                    if (lastWildCard >= 0)
                    {
                        patternIndex = lastWildCard;
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }
                isNotCharSetOn = isCharSetOn = false;
            }
            else
            {
                if (char.ToUpper(c) == char.ToUpper(p))
                {
                    patternIndex++;
                }
                else
                {
                    if (lastWildCard >= 0)
                    {
                        patternIndex = lastWildCard;
                    }
                    else
                    {
                        isMatch = false;
                        break;
                    }
                }
            }
        }
        endOfPattern = patternIndex >= pattern.Length;

        if (isMatch && !endOfPattern)
        {
            bool isOnlyWildCards = true;
            for (int i = patternIndex; i < pattern.Length; i++)
            {
                if (pattern[i] != '%')
                {
                    isOnlyWildCards = false;
                    break;
                }
            }
            if (isOnlyWildCards)
            {
                endOfPattern = true;
            }
        }

        set.Clear();
        _listCharPool.Return(set);
        return isMatch && endOfPattern;
    }
}
