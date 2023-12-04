using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli.Infrastructure;

/// <summary>
/// Simple web server that provides endpoint to run queries.
/// </summary>
internal sealed class WebServer
{
    private const string DefaultEndpointUri = "http://localhost:6789/";

    private const string PostMethod = "POST";
    private const string GetMethod = "GET";
    private const string OptionsMethod = "OPTIONS";

    private const string ContentTypeJson = "application/json";
    private const string ContentTypeTextPlain = "text/plain";
    private const string ContentTypeHtml = "text/html";
    private const string ContentTypeForm = "application/x-www-form-urlencoded";

    /// <summary>
    /// Endpoint uri.
    /// </summary>
    public string Uri { get; }

    public string AllowOrigin { get; set; } = string.Empty;

    private readonly Dictionary<string, Action<HttpListenerRequest, HttpListenerResponse>> _actions;

    private readonly ExecutionThread _executionThread;
    private readonly string? _password;

    private readonly Lazy<ILogger> _logger = new(() => Application.LoggerFactory.CreateLogger(nameof(WebServer)));

    internal sealed class WebServerReply : Dictionary<string, object>
    {
    }

    public WebServer(
        ExecutionThread executionThread,
        string? urls = null,
        string? password = null)
    {
        _actions = new Dictionary<string, Action<HttpListenerRequest, HttpListenerResponse>>
        {
            ["/"] = HandleIndexAction,
            ["/index.html"] = HandleIndexAction,
            ["/api/info"] = HandleInfoApiAction,
            ["/api/query"] = HandleQueryApiAction
        };

        _executionThread = executionThread;
        _password = password;
        Uri = urls ?? DefaultEndpointUri;
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
        Console.Out.WriteLine($"Listening on {Uri}. Use `POST /api/query` endpoint.");

        while (true)
        {
            // Common.
            var context = listener.GetContext();
            var response = context.Response;
            response.Headers["User-Agent"] = Application.GetProductFullName();
            response.StatusCode = (int)HttpStatusCode.OK;

            // CORS.
            if (!string.IsNullOrEmpty(AllowOrigin))
            {
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                if (context.Request.HttpMethod.Equals(OptionsMethod))
                {
                    response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
                    response.Headers.Add("Access-Control-Max-Age", "86400");
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    continue;
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
                    continue;
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
                    using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
                    WriteJsonMessage(jsonWriter, e.Message);
                    response.ContentType = ContentTypeJson;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                }
                catch (Exception e)
                {
                    _logger.Value.LogError(e, "Error while processing request.");
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }

            response.Close();
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void HandleQueryApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        if (request.HttpMethod != PostMethod && request.HttpMethod != GetMethod)
        {
            response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
            return;
        }

        var query = GetQueryFromRequest(request);
        var lastResult = _executionThread.Run(query);

        var iterator = lastResult.GetInternalType() == DataType.Object
            ? (IRowsIterator)lastResult.AsObject!
            : new SingleValueRowsIterator(lastResult);

        var acceptedType = request.AcceptTypes?.FirstOrDefault();
        if (string.IsNullOrEmpty(acceptedType) || acceptedType == "*/*")
        {
            acceptedType = request.ContentType;
        }

        if (acceptedType == ContentTypeHtml)
        {
            response.ContentType = ContentTypeHtml;
            using var streamWriter = new StreamWriter(response.OutputStream);
            WriteHtml(iterator, streamWriter);
        }
        else if (acceptedType == ContentTypeJson)
        {
            response.ContentType = ContentTypeJson;
            using var jsonWriter = new Utf8JsonWriter(response.OutputStream);
            WriteJson(iterator, jsonWriter);
        }
        else
        {
            response.ContentType = ContentTypeTextPlain;
            WriteText(iterator, response.OutputStream);
        }
    }

    internal sealed class QueryWrapper
    {
        public string? Query { get; set; }
    }

    private static string GetQueryFromRequest(HttpListenerRequest request)
    {
        if (request.HttpMethod == PostMethod)
        {
            using var sr = new StreamReader(request.InputStream);
            var text = sr.ReadToEnd();
            if (request.ContentType == ContentTypeTextPlain
                || request.ContentType == ContentTypeForm)
            {
                return text;
            }
            else if (request.ContentType == ContentTypeJson)
            {
                var wrapper = JsonSerializer.Deserialize(text, SourceGenerationContext.Default.QueryWrapper);
                return wrapper?.Query ?? string.Empty;
            }
        }
        else if (request.HttpMethod == GetMethod)
        {
            var query = request.QueryString.Get("q");
            if (!string.IsNullOrEmpty(query))
            {
                query = request.QueryString.Get("query");
                if (!string.IsNullOrEmpty(query))
                {
                    return query;
                }
            }
            throw new QueryCatException("Cannot parse query.");
        }
        throw new QueryCatException("Incorrect content type.");
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
        output.Write(iterator, _executionThread);
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

    private void HandleIndexAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        WriteResourceToStream(@"QueryCat.Cli.Infrastructure.WebServerIndex.html", response.OutputStream);
        var sr = new StreamWriter(response.OutputStream);
        sr.WriteLine(@"</script></body></html>");
        sr.Flush();
    }

    private static void WriteResourceToStream(string uri, Stream outputStream)
    {
        // Determine path.
        var assembly = Assembly.GetExecutingAssembly();

        // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
        using Stream? stream = assembly.GetManifestResourceStream(uri);
        if (stream != null)
        {
            stream.CopyTo(outputStream);
        }
    }

    private void HandleInfoApiAction(HttpListenerRequest request, HttpListenerResponse response)
    {
        var localPlugins = AsyncUtils.RunSync(async ()
            => await _executionThread.PluginsManager.ListAsync(localOnly: true))!;
        var dict = new WebServerReply
        {
            ["installedPlugins"] = localPlugins,
            ["version"] = Application.GetVersion(),
            ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription.Trim(),
            ["platform"] = Environment.Version,
            ["date"] = DateTimeOffset.Now,
        };
        JsonSerializer.Serialize(response.OutputStream, dict, SourceGenerationContext.Default.WebServerReply);
    }
}
