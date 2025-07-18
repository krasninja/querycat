using Xunit;
using QueryCat.Backend;
using QueryCat.Backend.Addons.Formatters;
using QueryCat.Backend.Commands.Update;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;

namespace QueryCat.IntegrationTests.Storage;

/// <summary>
/// Tests for <see cref="UpdateCommand" />.
/// </summary>
public sealed class CollectionInputTests : IDisposable
{
    private readonly CollectionInput<Employee> _employeesList;

    private sealed class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime? BirthDay { get; set; }

        public decimal Score { get; set; } = 5;

        public Employee()
        {
        }

        public Employee(int id, string name, DateTime birthDay, decimal score)
        {
            Id = id;
            Name = name;
            BirthDay = birthDay;
            Score = score;
        }
    }

    public CollectionInputTests()
    {
        var employeesList = new List<Employee>
        {
            new(1, "Russell Crowe", new DateTime(1964, 4, 7), 5.5m),
            new(2, "Albert Finney", new DateTime(1936, 5, 9), 5.5m),
            new(3, "Marion Cotillard", new DateTime(1975, 9, 30), 5.5m),
        };
        _employeesList = new CollectionInput<Employee>(employeesList);
    }

    [Fact]
    public async Task Select_ListOfEmployees_CopyToRowsFrame()
    {
        var rowsFrame = new RowsFrame(_employeesList.Columns);
        var rowsFrameSource = new RowsFrameSource(rowsFrame);
        var thread = new ExecutionThreadBootstrapper().Create();
        thread.TopScope.Variables["employees"] = VariantValue.CreateFromObject(_employeesList);
        var result = await thread.RunAsync("select * from \"employees\";");
        await rowsFrameSource.WriteAsync(result.AsRequired<IRowsIterator>());
        Assert.Equal(DataType.Integer, rowsFrame.Columns[0].DataType);
        Assert.Equal(3, rowsFrame.TotalRows);
    }

    [Fact]
    public async Task Update_ListOfEmployees_UpdateItems()
    {
        var thread = new ExecutionThreadBootstrapper().Create();
        thread.TopScope.Variables["employees"] = VariantValue.CreateFromObject(_employeesList);
        await thread.RunAsync("update employees set id = id + 1, score = 10 where id > 1;");
        Assert.Equal(1, _employeesList.TargetCollection.ElementAt(0).Id);
        Assert.Equal(3, _employeesList.TargetCollection.ElementAt(1).Id);
        Assert.Equal(10, _employeesList.TargetCollection.ElementAt(2).Score);
    }

    [Fact]
    public async Task Insert_ListOfEmployees_InsertItem()
    {
        var thread = new ExecutionThreadBootstrapper().Create();
        thread.TopScope.Variables["employees"] = VariantValue.CreateFromObject(_employeesList);
        await thread.RunAsync("insert into \"employees\" values (4, 'Abbie Cornish', '1982-08-07', 5);");
        Assert.Equal(4, _employeesList.TargetCollection.Count());
        Assert.Equal(5, _employeesList.TargetCollection.ElementAt(3).Score);
    }

    [Fact]
    public async Task Insert_ListOfEmployeesWithPartialInsert_InsertItem()
    {
        await using var thread = new ExecutionThreadBootstrapper()
            .WithStandardUriResolvers()
            .WithStandardFunctions()
            .WithRegistrations(AdditionalRegistration.Register)
            .Create();
        thread.TopScope.Variables["employees"] = VariantValue.CreateFromObject(_employeesList);
        await thread.RunAsync("insert into self(employees) (id, name) values (4, 'Abbie Cornish');");
        Assert.Equal(4, _employeesList.TargetCollection.Count());
        Assert.Equal(5, _employeesList.TargetCollection.ElementAt(3).Score);
    }

    [Fact]
    public async Task Delete_ListOfEmployees_ShouldRemoveByCondition()
    {
        await using var thread = new ExecutionThreadBootstrapper()
            .WithRegistrations(AdditionalRegistration.Register)
            .WithStandardFunctions()
            .Create();
        var rowsFrame = _employeesList.ToRowsFrame();
        var rowsInput = new RowsFrameSource(rowsFrame);
        thread.TopScope.Variables["employees"] = VariantValue.CreateFromObject(rowsInput);
        await thread.RunAsync(@"delete from self(employees) where id >= 2;");
        Assert.Equal(1, rowsFrame.TotalActiveRows);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _employeesList.Dispose();
    }
}
