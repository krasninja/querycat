namespace QueryCat.Tests.QueryRunner;

/// <summary>
/// Test data class.
/// </summary>
public sealed class TestData
{
    /// <summary>
    /// Should the test be skipped.
    /// </summary>
    public bool Skip { get; set; }

    /// <summary>
    /// Query to run.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Expected CSV result.
    /// </summary>
    public string Expected { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment.
    /// </summary>
    public string Comment { get; set; } = string.Empty;
}
