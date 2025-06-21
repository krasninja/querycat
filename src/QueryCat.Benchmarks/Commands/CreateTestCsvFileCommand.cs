using System.CommandLine;
using System.Diagnostics;
using Bogus;
using Bogus.DataSets;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.Benchmarks.Commands;

internal class CreateTestCsvFileCommand : Command
{
    public const string UsersFileName = "Users.csv";

    private const int ChunkSize = 2000;
    private const int Seed = 8675309;

    /// <summary>
    /// Number of items to generate.
    /// </summary>
    public int NumberOfItems { get; set; } = 200_000;

    public CreateTestCsvFileCommand() : base("create-test-csv")
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            Randomizer.Seed = new Random(Seed);

            var rowsFrame = User.ClassBuilder.BuildRowsFrame();

            // Fill users.
            var stopwatch = new Stopwatch();
            try
            {
                stopwatch.Start();
                var filePath = Path.Combine("../../..", UsersFileName); // Create at the project root.
                var output = new DsvFormatter(',').OpenOutput(new StreamBlobData(() => File.Create(filePath)));
                await output.OpenAsync();
                output.QueryContext = new RowsOutputQueryContext(rowsFrame.Columns);
                for (var count = 0; count < NumberOfItems; count += ChunkSize)
                {
                    var usersToInsert = UsersFaker.GenerateForever().Take(ChunkSize);
                    rowsFrame.AddRows(usersToInsert);
                }
                foreach (var row in rowsFrame)
                {
                    await output.WriteValuesAsync(row.Values);
                }
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($@"Created {UsersFileName}. Overall time spent: {stopwatch.Elapsed:c}.");
            }
        });
    }

    private static readonly Faker<User> UsersFaker = new Faker<User>()
        .RuleFor(u => u.Id, f => f.UniqueIndex)
        .RuleFor(u => u.Gender, f => f.PickRandom<Gender>())
        .RuleFor(u => u.FirstName, (f, u) => f.Name.FirstName(ConvertGender(u.Gender)))
        .RuleFor(u => u.LastName, (f, u) => f.Name.LastName(ConvertGender(u.Gender)))
        .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
        .RuleFor(u => u.Address, (f, u) => f.Address.StreetAddress(useFullAddress: true))
        .RuleFor(u => u.State, (f, u) => f.Address.StateAbbr())
        .RuleFor(u => u.Zip, (f, u) => f.Address.ZipCode())
        .RuleFor(u => u.Phone, (f, u) => f.Phone.PhoneNumber())
        .RuleFor(u => u.DateOfBirth, (f, u) => f.Date.PastOffset(70, DateTime.Now.AddYears(-18)).Date)
        .RuleFor(u => u.Balance, (f, u) => f.Random.Number(-5, 1000) * 100)
        .RuleFor(u => u.Phrase, (f, u) => f.Hacker.Phrase());

    private static Name.Gender ConvertGender(Gender gender)
    {
        return gender switch
        {
            Gender.Male => Bogus.DataSets.Name.Gender.Male,
            Gender.Female => Bogus.DataSets.Name.Gender.Female,
            Gender.Unknown => Bogus.DataSets.Name.Gender.Male,
            _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, null)
        };
    }
}
