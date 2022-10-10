using Xunit;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.UnitTests.Relational;

/// <summary>
/// Tests for <see cref="ClassBuilder{T}" />.
/// </summary>
public class ClassBuilderTests
{
    private class User
    {
        public int Id { get; }

        public string Name { get; }

        public User(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void Build_UserClass_CorrectRowsFrame()
    {
        // Arrange.
        var builder = new ClassBuilder<User>();
        builder.AddProperty("Id", u => u.Id);
        builder.AddProperty(u => u.Name);
        var users = new List<User>
        {
            new(10, "Ivan"),
            new(20, "Marina")
        };

        // Act.
        var frame = builder.BuildRowsFrame();
        frame.AddRows(users);

        // Assert.
        Assert.Equal(2, frame.Columns.Length);
        Assert.Equal(2, frame.TotalRows);
        Assert.Equal(DataType.Integer, frame.Columns[0].DataType);
    }
}
