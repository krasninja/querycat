namespace QueryCat.Backend.Execution;

/// <summary>
/// Ask question model.
/// </summary>
public class AskAIRequest
{
    /// <summary>
    /// Question text.
    /// </summary>
    public string Question { get; }

    /// <summary>
    /// Input to select data from.
    /// </summary>
    public IReadOnlyDictionary<string, string> Inputs { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="question">Question text.</param>
    /// <param name="inputs">Inputs.</param>
    public AskAIRequest(string question, IReadOnlyDictionary<string, string> inputs)
    {
        Question = question;
        Inputs = inputs;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="question">Question text.</param>
    /// <param name="inputs">Inputs.</param>
    public AskAIRequest(string question, params KeyValuePair<string, string>[] inputs)
    {
        Question = question;
        Inputs = inputs.ToDictionary(k => k.Key, v => v.Value);
    }
}
