using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Provides a mapping between file extensions and MIME types.
/// </summary>
internal sealed class MimeTypesProvider
{
    /// <summary>
    /// MIME types conversion table.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> _extensionMimeMapping =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".7z"] = "application/x-7z-compressed",
            [".aac"] = "audio/aac",
            [".asf"] = "video/x-ms-asf",
            [".asx"] = "video/x-ms-asf",
            [".avi"] = "video/x-msvideo",
            [".bmp"] = "image/bmp",
            [".bz"] = "application/x-bzip",
            [".bz2"] = "application/x-bzip2",
            [".css"] = "text/css",
            [".csv"] = "text/csv",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".dot"] = "application/msword",
            [".eml"] = "message/rfc822",
            [".epub"] = "application/epub+zip",
            [".flv"] = "video/x-flv",
            [".gz"] = "application/gzip",
            [".gif"] = "image/gif",
            [".htm"] = System.Net.Mime.MediaTypeNames.Text.Html,
            [".html"] = System.Net.Mime.MediaTypeNames.Text.Html,
            [".ical"] = "text/calendar",
            [".icalendar"] = "text/calendar",
            [".ico"] = "image/x-icon",
            [".jfif"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".jpg"] = "image/jpeg",
            [".js"] = "text/javascript",
            [".json"] = System.Net.Mime.MediaTypeNames.Application.Json,
            [".log"] = System.Net.Mime.MediaTypeNames.Text.Plain,
            [".m3u"] = "audio/x-mpegurl",
            [".m4a"] = "audio/mp4",
            [".m4v"] = "video/mp4",
            [".md"] = System.Net.Mime.MediaTypeNames.Text.Markdown,
            [".mka"] = "audio/x-matroska",
            [".mkv"] = "video/x-matroska",
            [".mov"] = "video/quicktime",
            [".mp3"] = "audio/mpeg",
            [".mp4"] = "video/mp4",
            [".mp4v"] = "video/mp4",
            [".mpeg"] = "video/mpeg",
            [".mpg"] = "video/mpeg",
            [".odp"] = "application/vnd.oasis.opendocument.presentation",
            [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".oga"] = "audio/ogg",
            [".ogg"] = "audio/ogg",
            [".ogv"] = "video/ogg",
            [".pdf"] = "application/pdf",
            [".pem"] = "application/x-x509-ca-cert",
            [".png"] = "image/png",
            [".pps"] = "application/vnd.ms-powerpoint",
            [".ppsx"] = "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".rar"] = "application/x-rar-compressed",
            [".rss"] = "application/rss+xml",
            [".rtf"] = "application/rtf",
            [".shtml"] = System.Net.Mime.MediaTypeNames.Text.Html,
            [".svg"] = "image/svg+xml",
            [".swf"] = "application/x-shockwave-flash",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".ts"] = "video/mp2t",
            [".tsv"] = "text/tab-separated-values",
            [".ttf"] = "font/ttf",
            [".tts"] = "video/vnd.dlna.mpeg-tts",
            [".txt"] = System.Net.Mime.MediaTypeNames.Text.Plain,
            [".vsd"] = "application/vnd.visio",
            [".vst"] = "application/vnd.visio",
            [".vsx"] = "application/vnd.visio",
            [".wasm"] = "application/wasm",
            [".wav"] = "audio/wav",
            [".wbmp"] = "image/vnd.wap.wbmp",
            [".weba"] = "audio/webm",
            [".webm"] = "video/webm",
            [".webp"] = "image/webp",
            [".wma"] = "audio/x-ms-wma",
            [".wmv"] = "video/x-ms-wmv",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".xhtml"] = "application/xhtml+xml",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".xml"] = "application/xml",
            [".zip"] = "application/zip",
        }.ToFrozenDictionary();

    private readonly ConcurrentDictionary<string, string> _additionalExtensionMimeMapping;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="mapping">Additional mappings.</param>
    public MimeTypesProvider(IReadOnlyDictionary<string, string>? mapping = null)
    {
        _additionalExtensionMimeMapping = mapping != null
            ? new ConcurrentDictionary<string, string>(mapping, StringComparer.OrdinalIgnoreCase)
            : new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get content type by extension.
    /// </summary>
    /// <param name="extension">File extension.</param>
    /// <returns>Specified content type or default binary type.</returns>
    public string GetContentTypeByExtension(string extension)
    {
        return TryGetContentTypeByExtension(extension, out var mime)
            ? mime
            : System.Net.Mime.MediaTypeNames.Application.Octet;
    }

    /// <summary>
    /// Try to get content type by extension.
    /// </summary>
    /// <param name="extension">File extension.</param>
    /// <param name="mime">Specified content type.</param>
    /// <returns><c>True</c> if content type was found, <c>false</c> otherwise.</returns>
    public bool TryGetContentTypeByExtension(string extension, out string mime)
    {
        if (!extension.StartsWith('.'))
        {
            extension = '.' + extension;
        }
        if (_additionalExtensionMimeMapping.TryGetValue(extension, out var outMime)
            || _extensionMimeMapping.TryGetValue(extension, out outMime))
        {
            mime = outMime;
            return true;
        }
        mime = string.Empty;
        return false;
    }

    /// <summary>
    /// Add or set the new mapping of extension to MIME type.
    /// </summary>
    /// <param name="extension">File extension (like .avi).</param>
    /// <param name="mime">MIME type.</param>
    public void AddOrUpdate(string extension, string mime)
    {
        ArgumentException.ThrowIfNullOrEmpty(extension);
        ArgumentException.ThrowIfNullOrEmpty(mime);

        if (!extension.StartsWith('.'))
        {
            extension = '.' + extension;
        }
        _additionalExtensionMimeMapping[extension] = mime;
    }
}
