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
    public override void Run()
    {
        var executionThread = new ExecutionThreadBootstrapper()
            .WithStandardFunctions()
            .Create();
        var email = new Email
        {
            Body = "TEST",
            Recipients = ["test1@example.com", "test2@example.com", "test3@example.com"],
        };

        var result1 = executionThread.Run(
            "email.Recipients[0+1];",
            new Dictionary<string, VariantValue>
            {
                ["email"] = VariantValue.CreateFromObject(email),
            });
        Console.WriteLine(result1.ToString()); // test2@example.com

        var result2 = executionThread.Run(
            "select count(*) from email.Recipients;",
            new Dictionary<string, VariantValue>
            {
                ["email"] = VariantValue.CreateFromObject(email),
            });
        Console.WriteLine(result2.AsRequired<IRowsIterator>().ToFrame().GetFirstValue().ToString()); // 3
    }
}
