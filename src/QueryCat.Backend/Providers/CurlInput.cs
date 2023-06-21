using System.ComponentModel;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Web request providers.
/// </summary>
internal static class CurlInput
{
    private const string ContentTypeHeader = "Content-Type";

    private static readonly HttpClient HttpClient = new();

    [Description("Read the HTTP resource.")]
    [FunctionSignature("curl(uri: string, fmt?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue WGet(FunctionCallInfo args)
    {
        var uriArgument = args.GetAt(0).AsString;
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;

        (uriArgument, var funcArgs) = QueryStringParser.ParseUri(uriArgument);

        if (!Uri.TryCreate(uriArgument, UriKind.Absolute, out var uri))
        {
            throw new QueryCatException("Invalid URI.");
        }
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = HttpClient.Send(request);

        // Try get formatter by HTTP response content type.
        if (formatter == null)
        {
            if (response.Headers.TryGetValues(ContentTypeHeader, out var contentTypes))
            {
                var contentType = contentTypes.Last();
                formatter = FormattersInfo.CreateFormatter(contentType, args.ExecutionThread, funcArgs);
            }
        }
        // Try get formatter by extension from URI.
        if (formatter == null)
        {
            var absolutePath = (request.RequestUri ?? uri).AbsolutePath;
            var extension = Path.GetExtension(absolutePath).ToLower();
            if (!string.IsNullOrEmpty(extension))
            {
                formatter = FormattersInfo.CreateFormatter(extension, args.ExecutionThread, funcArgs);
            }
        }
        formatter ??= new TextLineFormatter();

        var stream = response.Content.ReadAsStream();
        return VariantValue.CreateFromObject(formatter.OpenInput(stream));
    }
}
