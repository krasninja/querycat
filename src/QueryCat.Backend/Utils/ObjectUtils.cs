namespace QueryCat.Backend.Utils;

/// <summary>
/// Object utils.
/// </summary>
internal static class ObjectUtils
{
    /// <summary>
    /// Change object type. The method takes into account also nullable types.
    /// </summary>
    /// <param name="value">Object to type change.</param>
    /// <param name="conversionType">Conversion type.</param>
    /// <returns>New object with target type.</returns>
    public static object? ChangeType(object? value, Type conversionType)
    {
        if (value == null)
        {
            return null;
        }

        if (Nullable.GetUnderlyingType(conversionType) != null)
        {
            conversionType = Nullable.GetUnderlyingType(conversionType) ?? conversionType;
        }
        return Convert.ChangeType(value, conversionType);
    }
}
