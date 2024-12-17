namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The attribute is applied to functions that return formatters and specifies
/// what file extensions and MIME types it can process.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class FunctionFormattersAttribute : Attribute
{
    /// <summary>
    /// List of MIME types or file extensions.
    /// </summary>
    public string[] FormatterIds { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="formatterIds">MIME types or extensions.</param>
    public FunctionFormattersAttribute(params string[] formatterIds)
    {
        FormatterIds = formatterIds;
    }
}
