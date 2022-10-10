using System.Text;

namespace QueryCat.Backend.Utils;

/// <summary>
/// A thin wrapper around <see cref="StringBuilder" /> that adds indent
/// before each line.
/// </summary>
public class IndentedStringBuilder
{
    private readonly int _indentSize;
    private readonly StringBuilder _stringBuilder = new();
    private int _indent = 0;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="indentSize">Indent size.</param>
    public IndentedStringBuilder(int indentSize = 2)
    {
        _indentSize = indentSize;
    }

    /// <summary>
    /// Append line.
    /// </summary>
    /// <param name="value">String value.</param>
    public IndentedStringBuilder AppendLine(string value)
    {
        var lines = value.Split(Environment.NewLine);
        foreach (var line in lines)
        {
            AppendIndent();
            _stringBuilder.AppendLine(line);
        }
        return this;
    }

    /// <summary>
    /// Increment indent.
    /// </summary>
    public IndentedStringBuilder IncreaseIndent()
    {
        _indent++;
        return this;
    }

    /// <summary>
    /// Decrement indent.
    /// </summary>
    public IndentedStringBuilder DecreaseIndent()
    {
        _indent--;
        if (_indent < 0)
        {
            _indent = 0;
        }
        return this;
    }

    private void AppendIndent()
    {
        _stringBuilder.Append(' ', _indent * _indentSize);
    }

    /// <inheritdoc />
    public override string ToString() => _stringBuilder.ToString();
}
