using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The class is the delegate implementation for QueryCat project. It contains all the data
/// needed to run the node delegate and get the result.
/// </summary>
public interface IFuncUnit
{
    /// <summary>
    /// The delegate output type.
    /// </summary>
    DataType OutputType { get; }

    /// <summary>
    /// Get metadata object.
    /// </summary>
    /// <param name="index">Metadata key.</param>
    /// <returns>Object instance or null.</returns>
    object? GetData(int index);

    /// <summary>
    /// Set metadata object.
    /// </summary>
    /// <param name="index">Object index.</param>
    /// <param name="obj">Object instance.</param>
    void SetData(int index, object obj);

    /// <summary>
    /// Invoke and get value.
    /// </summary>
    /// <returns>Value.</returns>
    VariantValue Invoke();
}
