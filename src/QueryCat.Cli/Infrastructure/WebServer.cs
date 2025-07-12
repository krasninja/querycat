using System.Collections.Frozen;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Utils;

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

    private readonly IDictionary<string, Func<HttpListenerRequest, HttpListenerResponse, CancellationToken, Task>> _actions;

    private readonly IExecutionThread _executionThread;
    private readonly string? _password;
    private readonly string? _filesRoot;
    private readonly HashSet<IPAddress> _allowedAddresses;
    private readonly MimeTypesProvider _mimeTypesProvider = new();
    private int? _allowedAddressesSlots;
    private readonly Lock _lockObj = new();
    private readonly int _acceptConnections;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(WebServer));

    internal sealed class WebServerReply : Dictionary<string, object>;

    public WebServer(DefaultExecutionThread executionThread, WebServerOptions options)
    {
        _actions = new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, CancellationToken, Task>>
        {
            ["/"] = HandleIndexActionAsync,
            ["/index.html"] = HandleIndexActionAsync,
            ["/index.js"] = HandleIndexJsActionAsync,
            ["/api/info"] = HandleInfoApiActionAsync,
            ["/api/query"] = HandleQueryApiAction,
            ["/api/schema"] = HandleSchemaApiActionAsync,
            ["/api/files"] = Files_HandleFilesApiActionAsync,
        }.ToFrozenDictionary();

        _executionThread = executionThread;
        _password = options.Password;
        _filesRoot = options.FilesRoot;
        _allowedAddresses = new HashSet<IPAddress>(options.AllowedAddresses);
        _allowedAddressesSlots = options.AllowedAddressesSlots;
        _acceptConnections = Environment.ProcessorCount;
        Uri = options.Urls ?? DefaultEndpointUri;
    }

    /// <summary>
    /// Run web server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add(Uri);
        if (!string.IsNullOrEmpty(_password))
        {
            listener.AuthenticationSchemes = AuthenticationSchemes.Basic;
        }
        listener.IgnoreWriteExceptions = true;
        listener.Start();
        Console.Out.WriteLine(Resources.Messages.WebServerListen, Uri);

        var semaphore = new SemaphoreSlim(_acceptConnections, _acceptConnections);
        while (true)
        {
            try
            {
                await semaphore.WaitAsync(cancellationToken);
                await listener.GetContextAsync()
                    .ContinueWith(async t =>
                    {
                        semaphore.Release();
                        await HandleRequestAsync(t.Result, cancellationToken);
                    }, cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
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
                        _logger.LogInformation("[{Address}]: added to authorized list.", context.Request.RemoteEndPoint.Address);
                    }
                }
            }
            else
            {
                _logger.LogInformation("[{Address}]: unauthorized access.", context.Request.RemoteEndPoint.Address);
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
                await action.Invoke(context.Request, response, cancellationToken);
            }
            catch (QueryCatException e)
            {
                response.ContentType = MimeTypesProvider.ContentTypeJson;
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
                WriteJsonMessage(jsonWriter, e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                _logger.LogError(e, "Unauthorized access.");
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
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

    private Task HandleIndexActionAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        return WriteResourceToStream(@"QueryCat.Cli.Infrastructure.WebServerIndex.html", response, cancellationToken);
    }

    private Task HandleIndexJsActionAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        return WriteResourceToStream(@"QueryCat.Cli.Infrastructure.WebServerPage.js", response, cancellationToken);
    }

    private async Task HandleQueryApiAction(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (request.HttpMethod != HttpMethod.Post.Method && request.HttpMethod != HttpMethod.Get.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        var queryData = GetQueryDataFromRequest(request);
        _logger.LogInformation("[{Address}] Query: {QueryData}", request.RemoteEndPoint.Address, queryData);
        var lastResult = await _executionThread.RunAsync(queryData.Query, queryData.ParametersAsDict, cancellationToken);

        await WriteValueAsync(lastResult, request, response, cancellationToken);
    }

    private async Task HandleSchemaApiActionAsync(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        if (request.HttpMethod != HttpMethod.Post.Method && request.HttpMethod != HttpMethod.Get.Method)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        var query = GetQueryDataFromRequest(request);
        _logger.LogInformation("[{Address}] Schema: {Query}", request.RemoteEndPoint.Address, query);

        var thread = (DefaultExecutionThread)_executionThread;
        async void ThreadOnStatementExecuted(object? sender, ExecuteEventArgs e)
        {
            if (!e.Result.IsNull && e.Result.Type == DataType.Object
                && e.Result.AsObject is IRowsSchema rowsSchema)
            {
                var schema = await FunctionCaller.CallWithArgumentsAsync(Backend.Functions.InfoFunctions.Schema, thread,
                    [rowsSchema], cancellationToken);
                await WriteValueAsync(schema, request, response, cancellationToken);
                e.ContinueExecution = false;
            }
        }

        try
        {
            thread.StatementExecuted += ThreadOnStatementExecuted;
            await _executionThread.RunAsync(query.Query, query.ParametersAsDict, cancellationToken);
        }
        finally
        {
            thread.StatementExecuted -= ThreadOnStatementExecuted;
        }
    }

    #endregion

    private async Task WriteValueAsync(
        VariantValue value,
        HttpListenerRequest request,
        HttpListenerResponse response,
        CancellationToken cancellationToken)
    {
        if (value.Type == DataType.Blob && value.AsObjectUnsafe is IBlobData blobData)
        {
            response.ContentType = blobData.ContentType;
            await using var blobStream = blobData.GetStream();
            await blobStream.CopyToAsync(response.OutputStream, cancellationToken);
            await blobStream.FlushAsync(cancellationToken);
        }
        else
        {
            var acceptedType = request.AcceptTypes?.FirstOrDefault();
            if (string.IsNullOrEmpty(acceptedType) || acceptedType == "*/*")
            {
                acceptedType = request.ContentType;
            }

            var iterator = RowsIteratorConverter.Convert(value);
            if (acceptedType == MimeTypesProvider.ContentTypeHtml)
            {
                response.ContentType = MimeTypesProvider.ContentTypeHtml;
                await using var streamWriter = new StreamWriter(response.OutputStream);
                await WriteHtmlAsync(iterator, streamWriter, cancellationToken);
            }
            else if (acceptedType == MimeTypesProvider.ContentTypeJson)
            {
                response.ContentType = MimeTypesProvider.ContentTypeJson;
                await using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
                await WriteJsonAsync(iterator, jsonWriter, cancellationToken);
            }
            else
            {
                response.ContentType = MimeTypesProvider.ContentTypeTextPlain;
                await WriteTextAsync(iterator, response.OutputStream, cancellationToken);
            }
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
            if (request.ContentType == MimeTypesProvider.ContentTypeTextPlain
                || request.ContentType == MimeTypesProvider.ContentTypeForm)
            {
                return new WebServerQueryData(text);
            }
            else if (request.ContentType == MimeTypesProvider.ContentTypeJson)
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

    private static async Task WriteHtmlAsync(IRowsIterator iterator, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        await streamWriter.WriteLineAsync("""
                                          <!DOCTYPE html><HTML>
                                          <HEAD>
                                          <META CHARSET="utf-8">
                                          <LINK REL="stylesheet" HREF="https://cdn.jsdelivr.net/npm/bulma@1.0.3/css/bulma.min.css">
                                          </HEAD>
                                          <BODY><TABLE CLASS="table qcat-table">
                                          """);

        await streamWriter.WriteLineAsync("<TR>");
        foreach (var column in iterator.Columns)
        {
            if (column.IsHidden)
            {
                continue;
            }
            await streamWriter.WriteLineAsync($"<TH>{column.Name}</TH>");
        }
        await streamWriter.WriteLineAsync("</TR>");

        while (await iterator.MoveNextAsync(cancellationToken))
        {
            await streamWriter.WriteLineAsync("<TR>");
            for (var i = 0; i < iterator.Columns.Length; i++)
            {
                if (iterator.Columns[i].IsHidden)
                {
                    continue;
                }
                await streamWriter.WriteLineAsync($"<TD>{iterator.Current[i]}</TD>");
            }
            await streamWriter.WriteLineAsync("</TR>");
        }

        await streamWriter.WriteLineAsync("</TABLE></BODY></HTML>");
    }

    private async Task WriteTextAsync(IRowsIterator iterator, Stream stream, CancellationToken cancellationToken)
    {
        var formatter = new TextTableFormatter();
        var blobStream = new StreamBlobData(() => stream);
        var output = formatter.OpenOutput(blobStream);
        await output.WriteAsync(iterator, adjustColumnsLengths: true, _executionThread.ConfigStorage,
            cancellationToken: cancellationToken);
    }

    private static async Task WriteJsonAsync(IRowsIterator iterator, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WriteStartObject();
        WriteJsonSchema(iterator.Columns, jsonWriter);
        await WriteJsonDataAsync(iterator, jsonWriter, cancellationToken);
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

    private static async Task WriteJsonDataAsync(IRowsIterator iterator, Utf8JsonWriter jsonWriter, CancellationToken cancellationToken)
    {
        jsonWriter.WritePropertyName("data");
        jsonWriter.WriteStartArray();
        while (await iterator.MoveNextAsync(cancellationToken))
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

        switch (value.Type)
        {
            case DataType.Integer:
                jsonWriter.WriteNumberValue(value.AsIntegerUnsafe);
                break;
            case DataType.Float:
                jsonWriter.WriteNumberValue(value.AsFloatUnsafe);
                break;
            case DataType.Numeric:
                jsonWriter.WriteNumberValue(value.AsNumericUnsafe);
                break;
            case DataType.String:
                jsonWriter.WriteStringValue(value.AsString);
                break;
            case DataType.Boolean:
                jsonWriter.WriteBooleanValue(value.AsBoolean);
                break;
            default:
                jsonWriter.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
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

    private async Task WriteResourceToStream(string uri, HttpListenerResponse response, CancellationToken cancellationToken)
    {
        // Determine path.
        var assembly = Assembly.GetExecutingAssembly();

        // Set content type.
        var extension = Path.GetExtension(uri);
        response.ContentType = _mimeTypesProvider.GetContentTypeByExtension(extension);

        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        await using Stream? stream = assembly.GetManifestResourceStream(uri);
        await stream?.CopyToAsync(response.OutputStream, cancellationToken)!;
    }
}
