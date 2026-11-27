namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// The exception occurs when function cannot be found within
/// current execution context.
/// </summary>
public class CannotFindFunctionException : QueryCatException
{
    /// <summary>
    /// Function name.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Function name.</param>
    public CannotFindFunctionException(string name) :
        base(string.Format(Resources.Errors.CannotFindFunction, name))
    {
        FunctionName = name;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callArguments">Function arguments types.</param>
    /// <param name="innerException">Inner exception.</param>
    public CannotFindFunctionException(string name, FunctionCallArgumentsTypes callArguments, Exception? innerException) :
        base(string.Format(Resources.Errors.CannotFindFunctionWithArguments, name, callArguments), innerException)
    {
        FunctionName = name;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="callArguments">Function arguments types.</param>
    public CannotFindFunctionException(string name, FunctionCallArgumentsTypes callArguments) :
        this(name, callArguments, null)
    {
    }
}
