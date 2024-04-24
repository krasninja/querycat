using System.Collections.Frozen;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Simple web server that provides endpoint to run queries.
/// </summary>
internal sealed partial class WebServer
{
    private const string DefaultEndpointUri = "http://localhost:6789/";

    /// <summary>
    /// Endpoint URI.
    /// </summary>
    public string Uri { get; }

    public string AllowOrigin { get; set; } = string.Empty;

    private readonly IDictionary<string, Action<HttpListenerRequest, HttpListenerResponse>> _actions;

    private readonly IExecutionThread _executionThread;
    private readonly string? _password;
    private readonly string? _filesRoot;
    private readonly HashSet<IPAddress> _allowedAddresses;
    private readonly MimeTypeProvider _mimeTypeProvider = new();
    private int? _allowedAddressesSlots;
    private readonly object _lockObj = new();

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(WebServer));

    internal sealed class WebServerReply : Dictionary<string, object>;

    public WebServer(ExecutionThread executionThread, WebServerOptions options)
    {
        _actions = new Dictionary<string, Action<HttpListenerRequest, HttpListenerResponse>>
        {
            ["/"] = HandleIndexAction,
            ["/index.html"] = HandleIndexAction,
            ["/index.js"] = HandleIndexJsAction,
            ["/api/info"] = HandleInfoApiAction,
            ["/api/query"] = HandleQueryApiAction,
            ["/api/schema"] = HandleSchemaApiAction,
            ["/api/files"] = Files_HandleFilesApiAction,
        }.ToFrozenDictionary();

        _executionThread = executionThread;
        _password = options.Password;
        _filesRoot = options.FilesRoot;
        _allowedAddresses = new HashSet<IPAddress>(options.AllowedAddresses);
        _allowedAddressesSlots = options.AllowedAddressesSlots;
        Uri = options.Urls ?? DefaultEndpointUri;
    }

    /// <summary>
    /// Run web server.
    /// </summary>
    public void Run()
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(Uri);
        if (!string.IsNullOrEmpty(_password))
        {
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
        }
        listener.Start();
        Console.Out.WriteLine(Resources.Messages.WebServerListen, Uri);

        while (true)
        {
            SemaphoreSlim semaphore = new(0, 1);
            listener.GetContextAsync()
                .ContinueWith(t =>
                {
                    semaphore.Release();
                    semaphore.Dispose();
                    HandleRequest(t.Result);
                })
                .ConfigureAwait(false);
            semaphore.Wait();
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var response = context.Response;
        response.Headers["User-Agent"] = Application.GetProductFullName();
        response.Headers["Accept-Ranges"] = "bytes";
        response.StatusCode = (int)HttpStatusCode.OK;

        // CORS.
        if (!string.IsNullOrEmpty(AllowOrigin))
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            if (context.Request.HttpMethod.Equals(HttpMethod.Options.Method))
            {
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                response.Headers.Add("Access-Control-Max-Age", "86400");
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }
        }

        // Validate IP.
        if ((_allowedAddresses.Any() || _allowedAddressesSlots.HasValue)
            && !_allowedAddresses.Contains(context.Request.RemoteEndPoint.Address))
        {
            if (_allowedAddressesSlots > 0)
            {
                lock (_lockObj)
                {
                    if (_allowedAddressesSlots > 0)
                    {
                        _allowedAddresses.Add(context.Request.RemoteEndPoint.Address);
                        _allowedAddressesSlots--;
                        _logger.LogInformation($"[{context.Request.RemoteEndPoint.Address}]: added to authorized list.");
                    }
                }
            }
            else
            {
                _logger.LogInformation($"[{context.Request.RemoteEndPoint.Address}]: unauthorized access.");
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Close();
                return;
            }
        }

        // Auth.
        if (context.User?.Identity != null)
        {
            var identity = (HttpListenerBasicIdentity)context.User.Identity;
            if (identity.Password != _password)
            {
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Close();
                return;
            }
        }

        // Find action by path.
        var path = context.Request.Url?.LocalPath ?? string.Empty;
        if (_actions.TryGetValue(path, out var action))
        {
            try
            {
                action.Invoke(context.Request, response);
            }
            catch (QueryCatException e)
            {
                response.ContentType = MimeTypeProvider.ContentTypeJson;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
                WriteJsonMessage(jsonWriter, e.Message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while processing request.");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        response.Close();
    }

    #region Handles

    private void HandleIndexAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        WriteResourceToStream(@"QueryCat.Cli.Infrastructure.WebServerIndex.html", response);
    }

    private void HandleIndexJsAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        WriteResourceToStream(@"QueryCat.Cli.Infrastructure.WebServerPage.js", response);
    }

    private void HandleQueryApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod != HttpMethod.Post.Method && request.HttpMethod != HttpMethod.Get.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        var queryData = GetQueryDataFromRequest(request);
        _logger.LogInformation($"[{request.RemoteEndPoint.Address}] Query: {queryData}");
        var lastResult = _executionThread.Run(queryData.Query, queryData.ParametersAsDict);

        WriteIterator(ExecutionThreadUtils.ConvertToIterator(lastResult), request, response);
    }

    private void HandleSchemaApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod != HttpMethod.Post.Method && request.HttpMethod != HttpMethod.Get.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        var query = GetQueryDataFromRequest(request);
        _logger.LogInformation($"[{request.RemoteEndPoint.Address}] Schema: {query}");

        var thread = (ExecutionThread)_executionThread;
        void ThreadOnStatementExecuted(object? sender, ExecuteEventArgs e)
        {
            var result = thread.LastResult;
            if (!result.IsNull && result.GetInternalType() == DataType.Object
                && result.AsObject is IRowsSchema rowsSchema)
            {
                var schema = thread.CallFunction(Backend.Functions.InfoFunctions.Schema, rowsSchema);
                WriteIterator(ExecutionThreadUtils.ConvertToIterator(schema), request, response);
                e.ContinueExecution = false;
            }
        }

        try
        {
            thread.StatementExecuted += ThreadOnStatementExecuted;
            _executionThread.Run(query.Query, query.ParametersAsDict);
        }
        finally
        {
            thread.StatementExecuted -= ThreadOnStatementExecuted;
        }
    }

    #endregion

    private void WriteIterator(
        IRowsIterator iterator,
        HttpListenerRequest request,
        HttpListenerResponse response)
    {
        var acceptedType = request.AcceptTypes?.FirstOrDefault();
        if (string.IsNullOrEmpty(acceptedType) || acceptedType == "*/*")
        {
            acceptedType = request.ContentType;
        }

        if (acceptedType == MimeTypeProvider.ContentTypeHtml)
        {
            response.ContentType = MimeTypeProvider.ContentTypeHtml;
            using var streamWriter = new StreamWriter(response.OutputStream);
            WriteHtml(iterator, streamWriter);
        }
        else if (acceptedType == MimeTypeProvider.ContentTypeJson)
        {
            response.ContentType = MimeTypeProvider.ContentTypeJson;
            using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
            WriteJson(iterator, jsonWriter);
        }
        else
        {
            response.ContentType = MimeTypeProvider.ContentTypeTextPlain;
            WriteText(iterator, response.OutputStream);
        }
    }

    internal sealed class WebServerQueryData
    {
        public string Query { get; set; } = string.Empty;

        public List<WebServerQueryDataParameter> Parameters { get; set; } = new();

        public IDictionary<string, VariantValue> ParametersAsDict => Parameters.ToDictionary(k => k.Key, k => k.Value);

        public WebServerQueryData()
        {
        }

        public WebServerQueryData(string query)
        {
            Query = query;
        }

        /// <inheritdoc />
        public override string ToString() => Query;
    }

    internal class WebServerQueryDataParameter
    {
        public string Key { get; set; } = string.Empty;

        [JsonConverter(typeof(VariantValueJsonConverter))]
        public VariantValue Value { get; set; }
    }

    private static WebServerQueryData GetQueryDataFromRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod == HttpMethod.Post.Method)
        {
            using var sr = new StreamReader(request.InputStream);
            var text = sr.ReadToEnd();
            if (request.ContentType == MimeTypeProvider.ContentTypeTextPlain
                || request.ContentType == MimeTypeProvider.ContentTypeForm)
            {
                return new WebServerQueryData(text);
            }
            else if (request.ContentType == MimeTypeProvider.ContentTypeJson)
            {
                return JsonSerializer.Deserialize(text, SourceGenerationContext.Default.WebServerQueryData)
                    ?? new WebServerQueryData();
            }
        }
        else if (request.HttpMethod == HttpMethod.Get.Method)
        {
            var query = request.QueryString.Get("q");
            if (!string.IsNullOrEmpty(query))
            {
                return new WebServerQueryData(query);
            }
            return new WebServerQueryData(request.QueryString.Get("query") ?? string.Empty);
        }
        throw new QueryCatException(Resources.Errors.InvalidContentType);
    }

    private static void WriteHtml(IRowsIterator iterator, StreamWriter streamWriter)
    {
        streamWriter.WriteLine("<!DOCTYPE html><HTML>");
        streamWriter.WriteLine("<HEAD>");
        streamWriter.WriteLine("<META CHARSET=\"utf-8\">");
        streamWriter.WriteLine("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/bulma@0.9.4/css/bulma.min.css\">");
        streamWriter.WriteLine("</HEAD>");
        streamWriter.WriteLine("<BODY><TABLE class=\"table qcat-table\">");

        streamWriter.WriteLine("<TR>");
        foreach (var column in iterator.Columns)
        {
            if (column.IsHidden)
            {
                continue;
            }
            streamWriter.WriteLine($"<TH>{column.Name}</TH>");
        }
        streamWriter.WriteLine("</TR>");

        while (iterator.MoveNext())
        {
            streamWriter.WriteLine("<TR>");
            for (var i = 0; i < iterator.Columns.Length; i++)
            {
                if (iterator.Columns[i].IsHidden)
                {
                    continue;
                }
                streamWriter.WriteLine($"<TD>{iterator.Current[i]}</TD>");
            }
            streamWriter.WriteLine("</TR>");
        }

        streamWriter.WriteLine("</TABLE></BODY></HTML>");
    }

    private void WriteText(IRowsIterator iterator, Stream stream)
    {
        var formatter = new TextTableFormatter();
        var output = formatter.OpenOutput(stream);
        output.Write(iterator, adjustColumnsLengths: true);
    }

    private static void WriteJson(IRowsIterator iterator, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteStartObject();
        WriteJsonSchema(iterator.Columns, jsonWriter);
        WriteJsonData(iterator, jsonWriter);
        jsonWriter.WriteEndObject();
    }

    private static void WriteJsonSchema(Column[] columns, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WritePropertyName("schema");
        jsonWriter.WriteStartArray();
        foreach (var column in columns)
        {
            if (column.IsHidden)
            {
                continue;
            }
            jsonWriter.WriteStartObject();
            jsonWriter.WritePropertyName("name");
            jsonWriter.WriteStringValue(column.Name);
            jsonWriter.WritePropertyName("type");
            jsonWriter.WriteStringValue(column.DataType.ToString());
            jsonWriter.WritePropertyName("description");
            jsonWriter.WriteStringValue(column.Description);
            jsonWriter.WriteEndObject();
        }
        jsonWriter.WriteEndArray();
    }

    private static void WriteJsonData(IRowsIterator iterator, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WritePropertyName("data");
        jsonWriter.WriteStartArray();
        while (iterator.MoveNext())
        {
            jsonWriter.WriteStartObject();
            for (var i = 0; i < iterator.Columns.Length; i++)
            {
                if (iterator.Columns[i].IsHidden)
                {
                    continue;
                }
                jsonWriter.WritePropertyName(iterator.Columns[i].Name);
                WriteJsonVariantValue(jsonWriter, iterator.Current[i]);
            }
            jsonWriter.WriteEndObject();
        }
        jsonWriter.WriteEndArray();
    }

    private static void WriteJsonVariantValue(Utf8JsonWriter jsonWriter, in VariantValue value)
    {
        if (value.IsNull)
        {
            jsonWriter.WriteNullValue();
            return;
        }

        switch (value.GetInternalType())
        {
            case DataType.Integer:
                jsonWriter.WriteNumberValue(value.AsInteger);
                break;
            case DataType.Float:
                jsonWriter.WriteNumberValue(value.AsFloat);
                break;
            case DataType.Numeric:
                jsonWriter.WriteNumberValue(value.AsNumeric);
                break;
            case DataType.String:
                jsonWriter.WriteStringValue(value.AsString);
                break;
            case DataType.Boolean:
                jsonWriter.WriteBooleanValue(value.AsBoolean);
                break;
            default:
                jsonWriter.WriteStringValue(value.ToString());
                break;
        }
    }

    private static void WriteJsonMessage(Utf8JsonWriter jsonWriter, string message)
    {
        jsonWriter.WriteStartObject();
        jsonWriter.WritePropertyName("message");
        jsonWriter.WriteStringValue(message);
        jsonWriter.WriteEndObject();
    }

    private void WriteResourceToStream(string uri, HttpListenerResponse response)
    {
        // Determine path.
        var assembly = Assembly.GetExecutingAssembly();

        // Set content type.
        var extension = Path.GetExtension(uri);
        response.ContentType = _mimeTypeProvider.GetContentType(extension);

        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        using Stream? stream = assembly.GetManifestResourceStream(uri);
        stream?.CopyTo(response.OutputStream);
    }
}
