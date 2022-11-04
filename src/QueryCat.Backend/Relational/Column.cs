using System.ComponentModel.DataAnnotations;
using QueryCat.Backend.Types;
using DataType = QueryCat.Backend.Types.DataType;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Represents a relational column.
/// </summary>
[Serializable]
public class Column : ICloneable
{
    private static int nextId = 1;

    /// <summary>
    /// Column unique identifier.
    /// </summary>
    public int Id { get; private set; } = nextId++;

    /// <summary>
    /// Column name.
    /// </summary>
    [Required]
    public string Name { get; internal set; }

    /// <summary>
    /// Source (or table) name.
    /// </summary>
    public string SourceName { get; internal set; } = string.Empty;

    /// <summary>
    /// Full name in format "sourceName"."name".
    /// </summary>
    public string FullName => !string.IsNullOrEmpty(SourceName)
        ? $"{SourceName}.{Name}"
        : Name;

    /// <summary>
    /// Column description. Optional.
    /// </summary>
    public string Description { get; internal set; }

    /// <summary>
    /// Column data type.
    /// </summary>
    public DataType DataType { get; }

    /// <summary>
    /// The suggested column data length. The overall value
    /// size might be larger.
    /// </summary>
    public int Length { get; internal set; }

    /// <summary>
    /// Should the column be visible on output. It doesn't affect column search.
    /// </summary>
    public bool IsHidden => Name.StartsWith("__");

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(string name, DataType dataType, string? description = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }
        if (!DataTypeUtils.RowDataTypes.Contains(dataType))
        {
            throw new ArgumentOutOfRangeException(nameof(dataType));
        }

        Name = name;
        DataType = dataType;
        Description = description ?? string.Empty;
        Length = GetDefaultColumnLength();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(string name, string sourceName, DataType dataType, string? description = null) :
        this(name, dataType, description)
    {
        SourceName = sourceName;
    }

    /// <summary>
    /// Constructor. The name will be automatically generated.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(int columnIndex, DataType dataType, string? description = null) :
        this($"column{columnIndex}", dataType, description)
    {
    }

    /// <summary>
    /// Copy constructor.
    /// </summary>
    /// <param name="column">The column to copy from.</param>
    public Column(Column column)
    {
        Name = column.Name;
        SourceName = column.SourceName;
        DataType = column.DataType;
        Description = column.Description;
        Length = column.Length;
    }

    private int GetDefaultColumnLength()
    {
        var size = DataType switch
        {
            DataType.Integer => 5,
            DataType.String => 10,
            DataType.Float => 8,
            DataType.Timestamp => 14,
            DataType.Boolean => 5,
            DataType.Numeric => 8,
            _ => 4
        };
        if (Name.Length > size)
        {
            size = Name.Length;
        }
        return size;
    }

    internal static bool NameEquals(string name1, string name2)
        => name1.Equals(name2, StringComparison.OrdinalIgnoreCase);

    public static bool NameEquals(Column sourceColumn, string columnName, string? sourceName = null)
    {
        if (string.IsNullOrEmpty(sourceName))
        {
            return NameEquals(sourceColumn.Name, columnName);
        }
        return NameEquals(sourceColumn.Name, columnName) && NameEquals(sourceColumn.SourceName, sourceName);
    }

    /// <inheritdoc />
    public override string ToString()
        => string.IsNullOrEmpty(SourceName) ? $"{Name}: {DataType}" : $"{SourceName}.{Name}: {DataType}";

    /// <inheritdoc />
    public object Clone() => new Column(this);
}
