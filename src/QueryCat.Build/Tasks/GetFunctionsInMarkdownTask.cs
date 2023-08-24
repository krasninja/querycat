using System.ComponentModel;
using System.Reflection;
using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Functions-Markdown")]
public class GetFunctionsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var typeName = context.Arguments.GetArgument("Type");
        var type = typeof(VariantValue).Assembly
            .GetTypes().FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        if (type == null)
        {
            context.Log.Error("Type is not found.");
            return Task.CompletedTask;
        }
        var sb = new StringBuilder()
            .AppendLine("| Name and Description |")
            .AppendLine("| --- |");
        foreach (var methodInfo in type.GetMethods().OrderBy(m => m.Name))
        {
            var functionsSignatures = methodInfo.GetCustomAttributes(typeof(FunctionSignatureAttribute), false)
                .Cast<FunctionSignatureAttribute>()
                .ToList();
            if (!functionsSignatures.Any())
            {
                continue;
            }
            sb
                .Append("| ")
                .Append(string.Join("<br />", functionsSignatures.Select(f => "`" + f.Signature + "`")));
            if (methodInfo.GetCustomAttribute(typeof(DescriptionAttribute), false) is DescriptionAttribute descriptionAttribute)
            {
                sb.Append($"<br /><br /> {descriptionAttribute.Description}");
            }
            sb.AppendLine(" |");
        }
        var file = Path.Combine(context.OutputDirectory, "functions.md");
        File.WriteAllText(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");
        Console.WriteLine(File.ReadAllText(file));
        return base.RunAsync(context);
    }
}
