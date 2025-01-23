using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

/// <summary>
/// Extensions for <see cref="IFunction" />.
/// </summary>
public static class FunctionExtensions
{
    /// <summary>
    /// Check for signature equality of two functions.
    /// </summary>
    /// <param name="function">Source function.</param>
    /// <param name="other">Other function.</param>
    /// <returns><c>True</c> if signatures are equal, <c>false</c> otherwise.</returns>
    internal static bool IsSignatureEqual(this IFunction function, IFunction other)
    {
        return function.Arguments.SequenceEqual(other.Arguments);
    }

    /// <summary>
    /// Checks if function matches to target arguments.
    /// </summary>
    /// <param name="function">Instance of <see cref="IFunction" />.</param>
    /// <param name="functionCallArgumentsTypes">Argument types.</param>
    /// <returns>Returns <c>true</c> if matches, <c>false</c> otherwise.</returns>
    internal static bool MatchesToArguments(this IFunction function, FunctionCallArgumentsTypes functionCallArgumentsTypes)
    {
        var argumentsNodes = function.Arguments;
        var variadicArgument = argumentsNodes.Length > 0 && argumentsNodes[^1].IsVariadic
            ? argumentsNodes[^1] : null;

        var typesArr = new DataType[argumentsNodes.Length];
        Array.Fill(typesArr, DataType.Void);

        // Fill function arguments types.
        for (var i = 0; i < argumentsNodes.Length; i++)
        {
            var argumentNode = argumentsNodes[i];
            if (argumentNode.HasDefaultValue || argumentNode.IsOptional)
            {
                typesArr[i] = argumentNode.Type;
            }
        }
        if (functionCallArgumentsTypes.Positional.Length > typesArr.Length && variadicArgument == null)
        {
            return false;
        }

        // Place positional arguments.
        foreach (var arg in functionCallArgumentsTypes.Positional)
        {
            if (arg.Key > typesArr.Length - 1)
            {
                if (variadicArgument != null)
                {
                    Array.Resize(ref typesArr, arg.Key + 1);
                    Array.Resize(ref argumentsNodes, arg.Key + 1);
                    argumentsNodes[^1] = (FunctionSignatureArgument)variadicArgument.Clone();
                }
                else
                {
                    return false;
                }
            }
            typesArr[arg.Key] = arg.Value;
        }

        // Place named arguments.
        foreach (var namedArgType in functionCallArgumentsTypes.Named)
        {
            var argumentPosition = GetArgumentPosition(function, namedArgType.Key);
            typesArr[argumentPosition] = namedArgType.Value;
        }

        // Make sure we resolved all arguments nodes.
        for (var i = 0; i < typesArr.Length; i++)
        {
            // For variadic we can place any number of arguments.
            if (argumentsNodes[i] == variadicArgument)
            {
                continue;
            }

            // The Dynamic data type is used with ANY argument type.
            if (argumentsNodes[i].Type != DataType.Dynamic && typesArr[i] != argumentsNodes[i].Type)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get argument position.
    /// </summary>
    /// <param name="function">Instance of <see cref="IFunction" />.</param>
    /// <param name="argumentName">Argument name.</param>
    /// <returns>Position.</returns>
    private static int GetArgumentPosition(IFunction function, string argumentName)
    {
        argumentName = argumentName.ToUpper();

        var argumentPosition = -1;
        for (var i = 0; i < function.Arguments.Length; i++)
        {
            if (function.Arguments[i].Name.Equals(argumentName, StringComparison.OrdinalIgnoreCase))
            {
                argumentPosition = i;
                break;
            }
        }
        if (argumentPosition < 0)
        {
            throw new CannotFindArgumentException(function.Name, argumentName);
        }
        return argumentPosition;
    }
}
