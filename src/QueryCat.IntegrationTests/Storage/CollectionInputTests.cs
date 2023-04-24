using Xunit;
using QueryCat.Backend.Commands.Update;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.IntegrationTests.Storage;

/// <summary>
/// Tests for <see cref="UpdateCommand" />.
/// </summary>
public class CollectionInputTests
{
    private readonly CollectionInput<Employee> _employeesList;

    private sealed class Employee
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime? BirthDay { get; set; }

        public decimal Score { get; set; }

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
    public void Select_ListOfEmployees_CopyToRowsFrame()
    {
        var rowsFrame = new RowsFrame(_employeesList.Columns);
        var thread = new ExecutionThread(new ExecutionOptions
        {
            DefaultRowsOutput = new RowsFrameOutput(rowsFrame),
        });
        thread.TopScope.DefineVariable("employees", DataType.Object, VariantValue.CreateFromObject(_employeesList));
        thread.Run("select * from [employees];");
        Assert.Equal(DataType.Integer, rowsFrame.Columns[0].DataType);
        Assert.Equal(3, rowsFrame.TotalRows);
    }

    [Fact]
    public void Update_ListOfEmployees_UpdateItems()
    {
        var thread = new ExecutionThread(new ExecutionOptions
        {
            DefaultRowsOutput = NullRowsOutput.Instance,
        });
        thread.TopScope.DefineVariable("employees", DataType.Object, VariantValue.CreateFromObject(_employeesList));
        thread.Run("update [employees] set id = id + 1, score = 10 where id > 1;");
        Assert.Equal(1, _employeesList.TargetCollection.ElementAt(0).Id);
        Assert.Equal(3, _employeesList.TargetCollection.ElementAt(1).Id);
        Assert.Equal(10, _employeesList.TargetCollection.ElementAt(2).Score);
    }

    [Fact]
    public void Insert_ListOfEmployees_InsertItem()
    {
        var thread = new ExecutionThread(new ExecutionOptions
        {
            DefaultRowsOutput = NullRowsOutput.Instance,
        });
        thread.TopScope.DefineVariable("employees", DataType.Object, VariantValue.CreateFromObject(_employeesList));
        thread.Run("insert into [employees] values (4, 'Abbie Cornish', '1982-08-07', 5);");
        Assert.Equal(4, _employeesList.TargetCollection.Count());
        Assert.Equal(5, _employeesList.TargetCollection.ElementAt(3).Score);
    }
}
