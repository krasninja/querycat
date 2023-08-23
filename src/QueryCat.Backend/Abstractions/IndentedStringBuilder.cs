using System.Text;

namespace QueryCat.Backend.Abstractions;

/// <summary>
/// A thin wrapper around <see cref="StringBuilder" /> that adds indent
/// before each line.
/// </summary>
public class IndentedStringBuilder
{
    private readonly int _indentSize;
    private readonly StringBuilder _stringBuilder = new();
    private int _indent;
    private bool _isEmpty; // The marker to indicate that there were no output in the current line.

    /// <summary>
    /// Indent size.
    /// </summary>
    public int IndentSize => _indentSize;

    /// <summary>
    /// Current indent level.
    /// </summary>
    public int Indent => _indent;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="indentSize">Indent size.</param>
    /// <param name="skipFirstLineIndent">Skip indent for the first line.</param>
    public IndentedStringBuilder(int indentSize = 2, bool skipFirstLineIndent = false)
    {
        _indentSize = indentSize;
        _isEmpty = !skipFirstLineIndent;
    }

    /// <summary>
    /// Append line.
    /// </summary>
    /// <param name="value">String value.</param>
    public IndentedStringBuilder AppendLine(string value)
    {
        if (!value.Contains(Environment.NewLine))
        {
            AppendIndent();
            _stringBuilder.AppendLine(value);
        }
        else
        {
            var lines = value.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                AppendIndent();
                _isEmpty = true;
                _stringBuilder.AppendLine(line);
            }
        }

        _isEmpty = !value.EndsWith(Environment.NewLine);
        return this;
    }

    /// <summary>
    /// Append new line.
    /// </summary>
    public IndentedStringBuilder AppendLine()
    {
        _stringBuilder.AppendLine();
        _isEmpty = true;
        return this;
    }

    #region Overloads

    public IndentedStringBuilder Append(string value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        if (value == Environment.NewLine)
        {
            _isEmpty = true;
        }
        return this;
    }

    public IndentedStringBuilder Append(IndentedStringBuilder stringBuilder)
    {
        AppendIndent();
        _stringBuilder.Append(stringBuilder._stringBuilder);
        if (stringBuilder._stringBuilder[^1] == '\n')
        {
            _isEmpty = true;
        }
        return this;
    }

    public IndentedStringBuilder Append(StringBuilder stringBuilder)
    {
        AppendIndent();
        _stringBuilder.Append(stringBuilder);
        if (stringBuilder[^1] == '\n')
        {
            _isEmpty = true;
        }
        return this;
    }

    public IndentedStringBuilder Append(sbyte value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(byte value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(short value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(int value)
    {
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(long value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(float value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(double value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(decimal value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(ushort value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(char value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(uint value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    public IndentedStringBuilder Append(ulong value)
    {
        AppendIndent();
        _stringBuilder.Append(value);
        return this;
    }

    #endregion

    /// <summary>
    /// Increment indent.
    /// </summary>
    public IndentedStringBuilder IncreaseIndent()
    {
        _indent++;
        return this;
    }

    /// <summary>
    /// Increment indent.
    /// </summary>
    public IndentedStringBuilder IncreaseIndent(int level)
    {
        _indent += level * _indentSize;
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
        if (_isEmpty)
        {
            _stringBuilder.Append(' ', _indent * _indentSize);
            _isEmpty = false;
        }
    }

    /// <inheritdoc />
    public override string ToString() => _stringBuilder.ToString();
}
