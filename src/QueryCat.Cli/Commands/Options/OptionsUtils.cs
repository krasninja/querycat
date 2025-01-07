using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Invocation;

namespace QueryCat.Cli.Commands.Options;

/// <summary>
/// Utils for command line options.
/// </summary>
internal static class OptionsUtils
{
    /// <summary>
    /// Get value for an option.
    /// </summary>
    /// <param name="symbol">Option.</param>
    /// <param name="context">Command line invocation context.</param>
    /// <typeparam name="T">Option type.</typeparam>
    /// <returns>Option value.</returns>
    public static T GetValueForOption<T>(IValueDescriptor<T> symbol, InvocationContext context)
    {
        if (symbol is IValueSource valueSource &&
            valueSource.TryGetValue(symbol, context.BindingContext, out var boundValue) &&
            boundValue is T value)
        {
            return value;
        }
        else
        {
            return symbol switch
            {
                Argument<T> argument => context.ParseResult.GetValueForArgument(argument),
                Option<T> option => context.ParseResult.GetValueForOption(option)!,
                _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
            };
        }
    }
}
