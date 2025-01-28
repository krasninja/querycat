namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Methods to parser TimeSpans/Intervals.
/// </summary>
internal static class IntervalParser
{
    /// <summary>
    /// Parse interval from string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <param name="result">Parsed interval result.</param>
    /// <returns><c>True</c> if parsed successfully, <c>false</c> otherwise.</returns>
    internal static bool TryParseInterval(string target, out TimeSpan result)
    {
        var interval = ParseIntervalInternal(target, throwExceptions: false);
        if (!interval.HasValue)
        {
            result = default;
            return false;
        }
        result = interval.Value;
        return true;
    }

    /// <summary>
    /// Parse interval from string.
    /// </summary>
    /// <param name="target">Target string.</param>
    /// <returns>Time span.</returns>
    internal static TimeSpan ParseInterval(string target)
        => ParseIntervalInternal(target, throwExceptions: true)!.Value;

    private static TimeSpan? ParseIntervalInternal(string target, bool throwExceptions = true)
    {
        if (TimeSpan.TryParse(target, out var resultTimeSpan))
        {
            return resultTimeSpan;
        }

        var result = TimeSpan.Zero;
        var arr = target.ToUpper().Split(' ',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < arr.Length; i++)
        {
            string intervalType;
            var intervalString = arr[i];

            // Put a space between number and part (like 1h -> 1 h, 2sec -> 2 sec).
            var firstLetterIndex = GetFirstLetterIndex(intervalString);
            if (firstLetterIndex > -1)
            {
                intervalType = arr[i].Substring(firstLetterIndex);
                intervalString = arr[i].Substring(0, firstLetterIndex).Trim();
            }
            // Standard case like "1 min".
            else if (i < arr.Length - 1)
            {
                intervalType = arr[++i];
            }
            else
            {
                if (throwExceptions)
                {
                    throw new FormatException(Resources.Errors.InvalidNumberOfItems);
                }
                return null;
            }

            // First must be double.
            if (!double.TryParse(intervalString, out var intervalDouble))
            {
                if (throwExceptions)
                {
                    throw new FormatException(Resources.Errors.CannotParseInterval);
                }
                return null;
            }

            var timeSpan = ParseIntervalType(intervalDouble, intervalType);
            if (!timeSpan.HasValue)
            {
                return null;
            }
            result += timeSpan.Value;
        }

        return result;
    }

    private static int GetFirstLetterIndex(string str)
    {
        for (var i = 0; i < str.Length; i++)
        {
            if (char.IsLetter(str[i]))
            {
                return i;
            }
        }
        return -1;
    }

    private static TimeSpan? ParseIntervalType(double value, string type, bool throwExceptions = true)
    {
        switch (type)
        {
            case "MS":
            case "MILLISECOND":
            case "MILLISECONDS":
                return TimeSpan.FromMilliseconds(value);
            case "S":
            case "SEC":
            case "SECOND":
            case "SECONDS":
                return TimeSpan.FromSeconds(value);
            case "M":
            case "MIN":
            case "MINUTE":
            case "MINUTES":
                return TimeSpan.FromMinutes(value);
            case "H":
            case "HOUR":
            case "HOURS":
                return TimeSpan.FromHours(value);
            case "D":
            case "DAY":
            case "DAYS":
                return TimeSpan.FromDays(value);
        }
        if (throwExceptions)
        {
            throw new FormatException(Resources.Errors.CannotParseInterval);
        }
        return null;
    }
}
