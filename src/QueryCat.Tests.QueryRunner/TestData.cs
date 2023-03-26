namespace QueryCat.Tests.QueryRunner;

/// <summary>
/// Test data class.
/// </summary>
public class TestData
{
    /// <summary>
    /// Query to run.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Expected CSV result.
    /// </summary>
    public string Expected { get; set; } = string.Empty;
}
