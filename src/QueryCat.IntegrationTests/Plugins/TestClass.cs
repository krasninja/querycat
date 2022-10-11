namespace QueryCat.IntegrationTests.Plugins;

/// <summary>
/// Test class with only one property.
/// </summary>
public class TestClass
{
    public long Key { get; }

    public TestClass(long key)
    {
        Key = key;
    }

    /// <inheritdoc />
    public override string ToString() => Key.ToString();
}
