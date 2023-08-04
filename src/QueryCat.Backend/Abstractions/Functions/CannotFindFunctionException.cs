namespace QueryCat.Backend.Abstractions.Functions;

/// <summary>
/// The exception occurs when function cannot be found within
/// current execution context.
/// </summary>
[Serializable]
#pragma warning disable CA2229
public class CannotFindFunctionException : QueryCatException
#pragma warning restore CA2229
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Function name.</param>
    public CannotFindFunctionException(string name) :
        base($"Cannot find function '{name}'.")
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Function name.</param>
    /// <param name="arguments">Function arguments types.</param>
    public CannotFindFunctionException(string name, FunctionArgumentsTypes arguments) :
        base($"Cannot find function '{name}' matches arguments '{arguments}'.")
    {
    }
}
