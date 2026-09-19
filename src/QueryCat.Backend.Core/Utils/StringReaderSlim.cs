using System.Runtime.CompilerServices;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple implementation of <see cref="StringReader" />.
/// </summary>
internal ref struct StringReaderSlim
{
    private readonly ReadOnlySpan<char> _str;
    private int _pos;

    /// <summary>
    /// Is at the end.
    /// </summary>
    public bool IsEnd => _pos == _str.Length;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="str">Target string.</param>
    public StringReaderSlim(ReadOnlySpan<char> str)
    {
        _str = str;
        _pos = 0;
    }

    /// <summary>
    /// Returns the next available character without actually reading it.
    /// </summary>
    /// <returns>Char code or -1 if no next char.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Peek()
    {
        if (!IsEnd)
        {
            return _str[_pos];
        }
        return -1;
    }

    /// <summary>
    /// Reads the next character from the underlying string.
    /// </summary>
    /// <returns>Char code or -1 if no next char.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read()
    {
        if (!IsEnd)
        {
            var pos = _pos;
            _pos++;
            return _str[pos];
        }
        return -1;
    }

    /// <summary>
    /// Reset the state.
    /// </summary>
    public void Reset()
    {
        _pos = 0;
    }
}
