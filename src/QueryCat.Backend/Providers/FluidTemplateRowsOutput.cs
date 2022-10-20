using System.ComponentModel;
using Fluid;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Providers;

public class FluidTemplateRowsOutput : IRowsOutput
{
    [Description("Writes data to a template.")]
    [FunctionSignature("fluid_template(template: string, out: string, var_name: string = 'rows'): object<IRowsOutput>")]
    public static VariantValue FluidTemplate(FunctionCallInfo args)
    {
        var templateFile = args.GetAt(0).AsString;
        var outputFile = args.GetAt(1).AsString;
        var variableName = args.GetAt(2).AsString;

        return VariantValue.CreateFromObject(new FluidTemplateRowsOutput(templateFile, outputFile, variableName));
    }

    private readonly string _templateFile;
    private readonly string _outFile;
    private readonly string _varName;
    private readonly TemplateOptions _templateOptions;
    private readonly List<Row> _rows = new();

    private static readonly FluidParser Parser = new();

    public FluidTemplateRowsOutput(string templateFile, string outFile, string varName)
    {
        _templateFile = templateFile;
        _outFile = outFile;
        _varName = varName;

        _templateOptions = new TemplateOptions();
        _templateOptions.MemberAccessStrategy.Register<VariantValue, object>((obj, name) =>
        {
            return name switch
            {
                nameof(VariantValue.AsInteger) => obj.AsInteger,
                nameof(VariantValue.AsString) => obj.AsString,
                nameof(VariantValue.AsFloat) => obj.AsFloat,
                nameof(VariantValue.AsTimestamp) => obj.AsTimestamp,
                nameof(VariantValue.AsInterval) => obj.AsInterval,
                nameof(VariantValue.AsBoolean) => obj.AsBoolean,
                nameof(VariantValue.AsNumeric) => obj.AsNumeric,
                _ => obj.ToString(),
            };
        });
        _templateOptions.MemberAccessStrategy.Register<Row>();
        _templateOptions.MemberAccessStrategy.Register<Row, object>((obj, name) => obj[name]);
    }

    /// <inheritdoc />
    public void Open()
    {
    }

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
    }

    /// <inheritdoc />
    public void Close()
    {
        var templateText = File.ReadAllText(_templateFile);
        if (Parser.TryParse(templateText, out var template, out var error))
        {
            var context = new TemplateContext(template, _templateOptions);
            context.SetValue(_varName, _rows);
            File.WriteAllText(_outFile, template.Render(context));
        }
        else
        {
            throw new QueryCatException($"Cannot parse template: {error}.");
        }
    }

    /// <inheritdoc />
    public void Write(Row row)
    {
        // Cache here and write all on close.
        _rows.Add(row);
    }
}
