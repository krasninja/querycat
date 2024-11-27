namespace QueryCat.Backend.Commands.Select.KeyConditionValue;

/// <summary>
/// The values generation strategy that uses the same keys rows iterator to return multiple
/// results sets. For example, the query "SELECT * FROM source() WHERE key IN (1, 2, 3)" should
/// be executed 3 times with conditions "key = 1", "key = 2" and "key = 3".
/// </summary>
internal interface IKeyConditionMultipleValuesGenerator : IKeyConditionSingleValueGenerator
{
    /// <summary>
    /// Move to the next value.
    /// </summary>
    /// <returns>Returns <c>True</c> if the value is available. Otherwise, <c>false</c>.</returns>
    bool MoveNext();

    /// <summary>
    /// Reset the position to the initial element.
    /// </summary>
    void Reset();

    /// <summary>
    /// Current cursor position. -1 is the initial.
    /// </summary>
    int Position { get; }
}
