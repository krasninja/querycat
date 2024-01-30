using System.Buffers;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Infrastructure;

internal partial class WebServer
{
    [DebuggerDisplay("{Start}-{End}")]
    private sealed class Range
    {
        private long _start;

        public long Start
        {
            get => _start;
            set => _start = value > 0 && value <= _end ? value : 0;
        }

        private long _end;

        public long End
        {
            get => _end;
            set => _end = value > 0 && value > _start ? value : _start;
        }

        public long Size => End - Start;

        public Range(long start, long end)
        {
            End = end;
            Start = start;
        }
    }

    private void Files_HandleFilesApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod != GetMethod)
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
            Files_ServeDirectory(path, request, response);
        }
        else if (File.Exists(path))
        {
            Files_ServeFile(path, request, response);
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }
    }

    private void Files_ServeDirectory(string path, HttpListenerRequest request, HttpListenerResponse response)
    {
        _executionThread.TopScope.Variables["path"] = new VariantValue(path);
        var result = _executionThread.Run("select *, size_pretty(size) as 'size_pretty' from ls_dir(path);");
        _logger.LogInformation($"[{request.RemoteEndPoint.Address}] Dir: {path}");
        WriteIterator(ExecutionThreadUtils.ConvertToIterator(result), request, response);
    }

    private void Files_ServeFile(string file, HttpListenerRequest request, HttpListenerResponse response)
    {
        using var fileInput = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
        var maxLength = fileInput.Length;
        var range = Files_ParseRange(request.Headers["Range"], maxLength).FirstOrDefault();
        var isRangeRequest = range != null;
        range ??= new Range(0, maxLength);
        _logger.LogTrace("Start range {Start}-{End}", range.Start, range.End);

        response.AddHeader("Date", DateTime.Now.ToString("r"));
        response.AddHeader("Last-Modified", File.GetLastWriteTime(file).ToString("r"));
        response.AddHeader(
            "Content-Disposition", $"filename={System.Web.HttpUtility.UrlEncode(Path.GetFileName(file))}");
        response.ContentType = _mimeTypeProvider.GetContentType(Path.GetExtension(file));
        response.ContentLength64 = range.Size;
        if (isRangeRequest)
        {
            response.Headers["Content-Range"] = $"bytes {range.Start}-{range.End}/{maxLength}";
        }
        response.StatusCode = isRangeRequest ? (int)HttpStatusCode.PartialContent : (int)HttpStatusCode.OK;

        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
        int totalBytesRead = 0, totalBytesWrite = 0;
        _logger.LogInformation($"[{request.RemoteEndPoint.Address}] File: {file}");
        try
        {
            fileInput.Seek(range.Start, SeekOrigin.Begin);
            var finish = false;
            int bytesRead;
            while (!finish && (bytesRead = fileInput.Read(buffer, 0, buffer.Length)) > 0)
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
                    response.OutputStream.Write(buffer, 0, bytesRead);
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
            response.OutputStream.Flush();
            ArrayPool<byte>.Shared.Return(buffer);
        }
        _logger.LogTrace("End range {Start}-{End}, Total: {TotalWrite}", range.Start, range.End, totalBytesWrite);
    }

    private static IEnumerable<Range> Files_ParseRange(string? rangeValue, long maxLength)
    {
        if (string.IsNullOrEmpty(rangeValue))
        {
            yield break;
        }

        rangeValue = rangeValue.Replace("bytes=", string.Empty, StringComparison.OrdinalIgnoreCase);
        var ranges = rangeValue.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var range in ranges)
        {
            // Parse start and end.
            var startEnd = range.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (startEnd.Length < 1)
            {
                continue;
            }
            long.TryParse(startEnd[0], out var start);
            long end = maxLength - 1;
            if (startEnd.Length > 1)
            {
                long.TryParse(startEnd[1], out end);
            }

            // Validation.
            var rangeResult = new Range(start, end);
            if (rangeResult.Size > maxLength)
            {
                rangeResult.End = maxLength;
            }
            if (rangeResult.Size > maxLength - rangeResult.Start)
            {
                rangeResult.End = maxLength - 1;
            }
            yield return rangeResult;
        }
    }
}
