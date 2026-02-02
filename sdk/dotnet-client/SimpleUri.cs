using System;
using System.Collections.Generic;

namespace QueryCat.Plugins.Client;

/// <summary>
/// The class implements <see cref="Uri" />-like functionality
/// but without strict parsing and validation.
/// </summary>
public sealed class SimpleUri
{
    private readonly string _uri;

    /// <summary>
    /// Raw URI string (trimmed).
    /// </summary>
    public string OriginalString => _uri;

    /// <summary>
    /// Gets the scheme (text before the first ':'). Returns an empty
    /// string if no scheme is present.
    /// </summary>
    public string Scheme
    {
        get
        {
            var colonIndex = _uri.IndexOf(':');
            if (colonIndex <= 0)
            {
                return string.Empty;
            }
            return _uri.Substring(0, colonIndex);
        }
    }

    /// <summary>
    /// Gets the host part if an authority is present, otherwise an empty string.
    /// </summary>
    public string Host
    {
        get
        {
            var index = GetAfterSchemeIndex();
            if (_uri.Length - index < 2 || _uri[index] != '/' || _uri[index + 1] != '/')
            {
                return string.Empty;
            }

            index += 2; // Skip '//'.
            if (index >= _uri.Length)
            {
                return string.Empty;
            }

            var end = _uri.Length;
            for (var i = index; i < _uri.Length; i++)
            {
                var ch = _uri[i];
                if (ch == '/' || ch == '?' || ch == '#' || ch == ':')
                {
                    end = i;
                    break;
                }
            }

            return end > index ? _uri.Substring(index, end - index) : string.Empty;
        }
    }

    /// <summary>
    /// Gets the port if specified, otherwise -1.
    /// </summary>
    public int Port
    {
        get
        {
            var index = GetAfterSchemeIndex();
            if (_uri.Length - index < 2 || _uri[index] != '/' || _uri[index + 1] != '/')
            {
                return -1;
            }

            index += 2; // Skip '//'.

            // Find the ':' after the host.
            var colonIndex = -1;
            for (var i = index; i < _uri.Length; i++)
            {
                var ch = _uri[i];
                if (ch == '/' || ch == '?' || ch == '#')
                {
                    break;
                }
                if (ch == ':')
                {
                    colonIndex = i;
                    break;
                }
            }

            if (colonIndex < 0)
            {
                return -1;
            }

            var portStart = colonIndex + 1;
            var portEnd = _uri.Length;
            for (var i = portStart; i < _uri.Length; i++)
            {
                var ch = _uri[i];
                if (ch == '/' || ch == '?' || ch == '#')
                {
                    portEnd = i;
                    break;
                }
            }

            if (portEnd <= portStart)
            {
                return -1;
            }

            return int.TryParse(_uri.AsSpan(portStart, portEnd - portStart), out var port) ? port : -1;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the URI contains an authority component.
    /// </summary>
    public bool HasAuthority
    {
        get
        {
            var start = GetAfterSchemeIndex();
            return _uri.Length - start >= 2 &&
                   _uri[start] == '/' &&
                   _uri[start + 1] == '/';
        }
    }

    /// <summary>
    /// Gets the path component.
    /// </summary>
    public string Path
    {
        get
        {
            var index = GetPathStartIndex();
            if (index >= _uri.Length)
            {
                return string.Empty;
            }

            var end = _uri.Length;
            var questionIndex = _uri.IndexOf('?', index);
            if (questionIndex >= 0)
            {
                end = questionIndex;
            }
            var hashIndex = _uri.IndexOf('#', index);
            if (hashIndex >= 0 && hashIndex < end)
            {
                end = hashIndex;
            }

            return _uri.Substring(index, end - index);
        }
    }

    /// <summary>
    /// Gets an array containing the path segments that make up the specified URI.
    /// Similar to <see cref="Uri.Segments" />.
    /// </summary>
    public string[] Segments
    {
        get
        {
            var path = Path;
            if (string.IsNullOrEmpty(path))
            {
                return ["/"];
            }

            var segments = new List<string>();
            var start = 0;

            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == '/')
                {
                    segments.Add(path.Substring(start, i - start + 1));
                    start = i + 1;
                }
            }

            // Add remaining segment if any.
            if (start < path.Length)
            {
                segments.Add(path.Substring(start));
            }

            return segments.ToArray();
        }
    }

    /// <summary>
    /// Gets the query string including the leading '?', or empty string if there is no query.
    /// </summary>
    public string Query
    {
        get
        {
            var index = _uri.IndexOf('?', GetAfterSchemeIndex());
            if (index < 0)
            {
                return string.Empty;
            }
            var end = _uri.IndexOf('#', index);
            if (end < 0)
            {
                end = _uri.Length;
            }
            return _uri.Substring(index, end - index);
        }
    }

    /// <summary>
    /// Gets the fragment including the leading '#', or empty string if there is no fragment.
    /// </summary>
    public string Fragment
    {
        get
        {
            var index = _uri.IndexOf('#', GetAfterSchemeIndex());
            if (index < 0)
            {
                return string.Empty;
            }
            return _uri.Substring(index);
        }
    }

    public SimpleUri(string uri)
    {
        ArgumentException.ThrowIfNullOrEmpty(uri);
        _uri = uri.Trim();
    }

    public SimpleUri(Uri uri) : this(uri.ToString())
    {
    }

    private int GetAfterSchemeIndex()
    {
        var colonIndex = _uri.IndexOf(':');
        if (colonIndex <= 0)
        {
            return 0;
        }
        return colonIndex + 1;
    }

    private int GetPathStartIndex()
    {
        var index = GetAfterSchemeIndex();

        // Skip authority if present: "//host".
        if (_uri.Length - index >= 2 && _uri[index] == '/' && _uri[index + 1] == '/')
        {
            index += 2; // Skip '//'.

            // Skip host and optional port until next '/' or '?' or '#'.
            while (index < _uri.Length)
            {
                var ch = _uri[index];
                if (ch == '/' || ch == '?' || ch == '#')
                {
                    break;
                }
                index++;
            }
        }

        return index;
    }

    /// <inheritdoc />
    public override string ToString() => _uri;
}
