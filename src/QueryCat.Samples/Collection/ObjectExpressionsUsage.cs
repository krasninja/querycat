using System.Globalization;
using QueryCat.Backend;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;

namespace QueryCat.Samples.Collection;

internal class ObjectExpressionsUsage : BaseUsage
{
    private class Email
    {
        public string Body { get; init; } = string.Empty;

        public List<string> Recipients { get; set; } = new();
    }

    /// <inheritdoc />
    public override async Task RunAsync()
    {
        var executionThread = await new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .CreateAsync();
        var email = new Email
        {
            Body = "TEST",
            Recipients = ["test1@example.com", "test2@example.com", "test3@example.com"],
        };

        var result1 = await executionThread.RunAsync(
            "email.Recipients[0+1];",
            new Dictionary<string, VariantValue>
            {
                ["email"] = VariantValue.CreateFromObject(email),
            });
        Console.WriteLine(result1.ToString(CultureInfo.InvariantCulture)); // test2@example.com

        var result2 = await executionThread.RunAsync(
            "select count(*) from email.Recipients;",
            new Dictionary<string, VariantValue>
            {
                ["email"] = VariantValue.CreateFromObject(email),
            });
        var result2Frame = (await result2.AsRequired<IRowsIterator>().ToFrameAsync())
            .GetFirstValue();
        Console.WriteLine(result2Frame.ToString(CultureInfo.InvariantCulture)); // 3
    }
}
