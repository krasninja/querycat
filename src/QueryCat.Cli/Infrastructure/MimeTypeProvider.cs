using System.Collections.Frozen;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Provides a mapping between file extensions and MIME types.
/// </summary>
public sealed class MimeTypeProvider
{
    public const string ContentTypeJson = "application/json";
    public const string ContentTypeTextPlain = "text/plain";
    public const string ContentTypeHtml = "text/html";
    public const string ContentTypeForm = "application/x-www-form-urlencoded";
    public const string ContentTypeOctetStream = "application/octet-stream";

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
            [".gz"] = "application/x-gzip",
            [".gif"] = "image/gif",
            [".htm"] = ContentTypeHtml,
            [".html"] = ContentTypeHtml,
            [".ical"] = "text/calendar",
            [".icalendar"] = "text/calendar",
            [".ico"] = "image/x-icon",
            [".jfif"] = "image/pjpeg",
            [".jpeg"] = "image/jpeg",
            [".jpg"] = "image/jpeg",
            [".js"] = "application/x-javascript",
            [".json"] = ContentTypeJson,
            [".log"] = ContentTypeTextPlain,
            [".m3u"] = "audio/x-mpegurl",
            [".m4a"] = "audio/mp4",
            [".m4v"] = "video/mp4",
            [".md"] = "text/markdown",
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
            [".oga"] = "video/ogg",
            [".ogg"] = "video/ogg",
            [".ogv"] = "video/ogg",
            [".pdf"] = "application/pdf",
            [".pem"] = "application/x-x509-ca-cert",
            [".png"] = "image/png",
            [".pps"] = "application/vnd.ms-powerpoint",
            [".ppsx"] = "application/vnd.openxmlformats-officedocument.presentationml.slideshow",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".psd"] = "application/octet-stream",
            [".rar"] = "application/x-rar-compressed",
            [".rss"] = "text/xml",
            [".rtf"] = "application/rtf",
            [".shtml"] = ContentTypeHtml,
            [".svg"] = "image/svg+xml",
            [".swf"] = "application/x-shockwave-flash",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff",
            [".ts"] = "video/mp2t",
            [".tsv"] = "text/tab-separated-values",
            [".ttf"] = "font/ttf",
            [".tts"] = "video/vnd.dlna.mpeg-tts",
            [".txt"] = ContentTypeTextPlain,
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
            [".woff"] = "application/font-woff",
            [".woff2"] = "font/woff2",
            [".xhtml"] = "application/xhtml+xml",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".xml"] = "text/xml",
            [".zip"] = "application/zip",
        }.ToFrozenDictionary();

    private readonly IReadOnlyDictionary<string, string> _additionalExtensionMimeMapping;

    private static readonly IReadOnlyDictionary<string, string> _emptyDictionary = new Dictionary<string, string>();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="mapping">Additional mappings.</param>
    public MimeTypeProvider(IReadOnlyDictionary<string, string>? mapping = null)
    {
        _additionalExtensionMimeMapping = (mapping ?? _emptyDictionary).ToFrozenDictionary();
    }

    /// <summary>
    /// Get content type by extension.
    /// </summary>
    /// <param name="extension">File extension.</param>
    /// <returns>Specified content type or default binary type.</returns>
    public string GetContentType(string extension)
    {
        return _additionalExtensionMimeMapping.TryGetValue(extension, out var mime)
            || _extensionMimeMapping.TryGetValue(extension, out mime)
            ? mime
            : ContentTypeOctetStream;
    }

    /// <summary>
    /// Try get content type by extension.
    /// </summary>
    /// <param name="extension">File extension.</param>
    /// <param name="mime">Specified content type.</param>
    /// <returns><c>True</c> if content type was found, <c>false</c> otherwise.</returns>
    public bool TryGetContentType(string extension, out string mime)
    {
        if (_additionalExtensionMimeMapping.TryGetValue(extension, out var outMime)
            || _extensionMimeMapping.TryGetValue(extension, out outMime))
        {
            mime = outMime;
            return true;
        }
        mime = string.Empty;
        return false;
    }
}
