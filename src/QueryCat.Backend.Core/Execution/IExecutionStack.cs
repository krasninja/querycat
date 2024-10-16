using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution stack that is used to pass arguments into functions.
/// </summary>
public interface IExecutionStack : IEnumerable<VariantValue>
{
    /*
      Here is the outline view of stack:

      ...        <!-- (stack top)
      ...
      value 4
      value 3    <-- frame2 start, end of frame1
      value 2
      value 1    <-- stack start, frame1 start

     */

    /// <summary>
    /// Get the value within the current frame.
    /// </summary>
    /// <param name="index">Value index.</param>
    VariantValue this[int index] { get; set; }

    /// <summary>
    /// Current frame length.
    /// </summary>
    int FrameLength { get; }

    /// <summary>
    /// Create new frame.
    /// </summary>
    void CreateFrame();

    /// <summary>
    /// Close current frame.
    /// </summary>
    void CloseFrame();

    /// <summary>
    /// Push value to the current frame.
    /// </summary>
    /// <param name="value"></param>
    void Push(VariantValue value);

    /// <summary>
    /// Pop value from the current frame.
    /// </summary>
    /// <returns></returns>
    VariantValue Pop();
}
