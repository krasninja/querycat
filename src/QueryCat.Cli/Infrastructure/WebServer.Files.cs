using System.Buffers;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli.Infrastructure;

internal partial class WebServer
{
    private static readonly string _selectFilesQuery = @"select *, size_pretty(size) as 'size_pretty' from ls_dir(path);";
    private static readonly ArrayPool<byte> _readBufferPool = ArrayPool<byte>.Create();

    [DebuggerDisplay("{Start}-{End}")]
    private readonly struct Range
    {
        public long Start { get; }

        public long End { get; }

        public long Size => End - Start;

        public Range(long start, long end)
        {
            Start = start > 0 && start <= end ? start : 0;
            End = end > 0 && end > start ? end : start;
        }
    }

    private async Task Files_HandleFilesApiActionAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (request.HttpMethod != HttpMethod.Get.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }
        if (string.IsNullOrEmpty(_filesRoot))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        var query = GetQueryDataFromRequest(request).Query;

        // Get absolute path.
        query = query.Replace("..", string.Empty);
        while (query.StartsWith("/"))
        {
            query = query.Substring(1, query.Length - 1);
        }
        query = query.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(_filesRoot, query);

        if (Directory.Exists(path))
        {
            await Files_ServeDirectory(path, request, response, cancellationToken);
        }
        else if (File.Exists(path))
        {
            await Files_ServeFile(path, request, response, cancellationToken);
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    private async Task Files_ServeDirectory(string path, HttpListenerRequest request, HttpListenerResponse response,
        CancellationToken cancellationToken)
    {
        _executionThread.TopScope.Variables["path"] = new VariantValue(path);
        var result = await _executionThread.RunAsync(_selectFilesQuery, cancellationToken: cancellationToken);
        _logger.LogInformation("[{Address}] Dir: {Path}", request.RemoteEndPoint.Address, path);
        await WriteValueAsync(result, request, response, cancellationToken);
    }

    private async Task Files_ServeFile(string file, HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        await using var fileInput = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
        var maxLength = fileInput.Length;
        Span<Range> ranges = stackalloc Range[1];
        var isRangeRequest = Files_ParseRange(request.Headers["Range"], maxLength, ranges) > 0;
        var range = isRangeRequest ? ranges[0] : new Range(0, maxLength);
        _logger.LogTrace("Start range {Start}-{End}", range.Start, range.End);

        response.AddHeader("Date", DateTime.Now.ToString("r"));
        response.AddHeader("Last-Modified", File.GetLastWriteTime(file).ToString("r"));
        response.AddHeader(
            "Content-Disposition", $"filename={System.Web.HttpUtility.UrlEncode(Path.GetFileName(file))}");
        response.ContentType = _mimeTypesProvider.GetContentTypeByExtension(Path.GetExtension(file));
        response.ContentLength64 = range.Size;
        if (isRangeRequest)
        {
            response.Headers["Content-Range"] = $"bytes {range.Start}-{range.End}/{maxLength}";
        }
        response.StatusCode = isRangeRequest ? (int)HttpStatusCode.PartialContent : (int)HttpStatusCode.OK;

        var buffer = _readBufferPool.Rent(1024 * 8);
        int totalBytesRead = 0, totalBytesWrite = 0;
        _logger.LogInformation("[{Address}] File: {File}", request.RemoteEndPoint.Address, file);
        try
        {
            fileInput.Seek(range.Start, SeekOrigin.Begin);
            var finish = false;
            int bytesRead;
            while (!finish && (bytesRead = await fileInput.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                totalBytesRead += bytesRead;

                // Read only final range.
                if (totalBytesRead >= range.Size)
                {
                    bytesRead -= totalBytesRead - (int)range.Size;
                    finish = true;
                }
                try
                {
                    if (!response.OutputStream.CanWrite)
                    {
                        break;
                    }
                    await response.OutputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    totalBytesWrite += bytesRead;
                }
                catch (HttpListenerException e)
                {
                    _logger.LogDebug(e, "Cannot write to output stream: {Error}", e.Message);
                    finish = true;
                }
            }
        }
        finally
        {
            fileInput.Close();
            await response.OutputStream.FlushAsync(cancellationToken);
            ArrayPool<byte>.Shared.Return(buffer);
        }
        _logger.LogTrace("End range {Start}-{End}, Total: {TotalWrite}", range.Start, range.End, totalBytesWrite);
    }

    private static int Files_ParseRange(string? rangeValue, long maxLength, Span<Range> result)
    {
        if (string.IsNullOrEmpty(rangeValue))
        {
            return 0;
        }

        const string bytesHeader = "bytes=";
        var rangeValueSpan = rangeValue.StartsWith(bytesHeader, StringComparison.OrdinalIgnoreCase)
            ? rangeValue.AsSpan(bytesHeader.Length)
            : rangeValue.AsSpan();
        Span<System.Range> ranges = stackalloc System.Range[7];
        var rangeValueSpanCount = rangeValueSpan.Split(
            ranges,
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var i = 0;
        foreach (var range in ranges)
        {
            if (i >= result.Length || i >= rangeValueSpanCount)
            {
                break;
            }

            // Parse start and end.
            var rangeSpan = rangeValueSpan[range];
            var dashIndex = rangeSpan.IndexOf('-');
            if (dashIndex < 0)
            {
                continue;
            }

            var startSpan = rangeSpan[..dashIndex].Trim();
            var endSpan = rangeSpan[(dashIndex + 1)..].Trim();
            long start = 0, end = maxLength;

            // Range: <unit>=<range-start>-<range-end>.
            if (startSpan.Length > 0 && endSpan.Length > 0)
            {
                long.TryParse(startSpan, out start);
                long.TryParse(endSpan, out end);
            }
            // Range: <unit>=-<suffix-length>.
            else if (startSpan.Length < 1 && endSpan.Length > 0)
            {
                long.TryParse(endSpan, out start);
                start = maxLength - start;
            }
            // Range: <unit>=<range-start>-.
            else if (startSpan.Length > 0 && endSpan.Length < 1)
            {
                long.TryParse(startSpan, out start);
            }

            // Validation.
            if (start > end)
            {
                start = end;
            }
            var rangeResult = new Range(start, end);
            if (rangeResult.Size > maxLength)
            {
                rangeResult = new Range(start, maxLength);
            }
            if (rangeResult.Size > maxLength - rangeResult.Start)
            {
                rangeResult = new Range(start, maxLength - rangeResult.Start);
            }
            result[i++] = rangeResult;
        }

        return i;
    }
}
