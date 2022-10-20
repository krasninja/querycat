using System.ComponentModel;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

/// <summary>
/// Web request providers.
/// </summary>
public static class CurlInput
{
    private const string ContentTypeHeader = "Content-Type";

    private static readonly HttpClient HttpClient = new();

    [Description("Read the HTTP resource.")]
    [FunctionSignature("curl(uri: string, formatter?: object<IRowsFormatter>): object<IRowsInput>")]
    public static VariantValue WGet(FunctionCallInfo args)
    {
        var uriArgument = args.GetAt(0);
        var formatter = args.GetAt(1).AsObject as IRowsFormatter;

        if (!Uri.TryCreate(uriArgument, UriKind.Absolute, out Uri? uri))
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
                formatter = FormatUtils.GetFormatterByContentType(contentType);
            }
        }
        // Try get formatter by extension from URI.
        if (formatter == null)
        {
            var absolutePath = (request.RequestUri ?? uri).AbsolutePath;
            var extension = Path.GetExtension(absolutePath);
            if (!string.IsNullOrEmpty(extension))
            {
                formatter = FormatUtils.GetFormatterByExtension(extension);
            }
        }
        if (formatter == null)
        {
            formatter = new TextLineFormatter();
        }

        var stream = response.Content.ReadAsStream();
        return VariantValue.CreateFromObject(formatter.OpenInput(stream));
    }
}
