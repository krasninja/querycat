using System.ComponentModel.DataAnnotations;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using DataType = QueryCat.Backend.Core.Types.DataType;

namespace QueryCat.Backend.Core.Data;

/// <summary>
/// Represents a relational column.
/// </summary>
public class Column : ICloneable, IModelDescription
{
    /// <summary>
    /// The title can be used for a single result, or when there is only one column in result.
    /// </summary>
    public const string ValueColumnTitle = "value";

    #region Attributes

    /// <summary>
    /// The attribute should be used to mark unique identifier columns.
    /// No value required.
    /// </summary>
    public const string AttributeId = "@id";

    /// <summary>
    /// The attribute should be used to mark unique identifier columns.
    /// No value required.
    /// </summary>
    public static ColumnAttribute ColumnAttributeId { get; } = new(AttributeId);

    /// <summary>
    /// The attribute should be used to mark timestamp fields that identifies
    /// of event or log creation date and time.
    /// </summary>
    public const string AttributeTimestamp = "@ts";

    /// <summary>
    /// The attribute should be used to mark timestamp fields that identifies
    /// of event or log creation date and time.
    /// </summary>
    public static ColumnAttribute ColumnAttributeTimestamp { get; } = new(AttributeTimestamp);

    /// <summary>
    /// The attribute should be used to mark currency numeric fields.
    /// </summary>
    public const string AttributeCurrency = "@currency";

    /// <summary>
    /// The attribute should be used to mark currency numeric fields.
    /// </summary>
    public static ColumnAttribute ColumnAttributeCurrency { get; } = new(AttributeCurrency);

    /// <summary>
    /// The attribute should be used to mark raw data (JSON, string) representation fields.
    /// </summary>
    public const string AttributeRawData = "@raw";

    /// <summary>
    /// The attribute should be used to mark raw data (JSON, string) representation fields.
    /// </summary>
    public static ColumnAttribute ColumnAttributeRawData { get; } = new(AttributeRawData);

    /// <summary>
    /// The attribute should be used to mark string fields that contain
    /// error message or exception.
    /// </summary>
    public const string AttributeError = "@error";

    /// <summary>
    /// The attribute should be used to mark string fields that contain
    /// error message or exception.
    /// </summary>
    public static ColumnAttribute ColumnAttributeError { get; } = new(AttributeError);

    /// <summary>
    /// The attribute should be used to mark string columns which have a specific format
    /// (file names, URI, JSON, etc.).
    /// </summary>
    public const string AttributeFormat = "@fmt";

    public const string AttributeFormatJson = "json";
    public const string AttributeFormatXml = "xml";
    public const string AttributeFormatEmail = "email";
    public const string AttributeFormatPhone = "phone";
    public const string AttributeFormatRegularExpression = "regexp";
    public const string AttributeFormatUri = "uri";
    public const string AttributeFormatFile = "file";
    public const string AttributeFormatBase64 = "base64";
    public const string AttributeFormatCreditCard = "credit_card";
    public const string AttributeFormatPassword = "password";
    public const string AttributeFormatType = "type";
    public const string AttributeFormatIpAddress = "ip_address";

    #endregion

    private static int _nextId = 1;

    /// <summary>
    /// Column unique identifier.
    /// </summary>
    public int Id { get; private set; } = Interlocked.Increment(ref _nextId);

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
    public DataType DataType { get; internal set; }

    /// <summary>
    /// The suggested column data length. The overall value
    /// size might be larger.
    /// </summary>
    public int Length { get; internal set; }

    /// <summary>
    /// Column attribute.
    /// </summary>
    /// <param name="key">Attribute key.</param>
    /// <param name="value">Attribute value.</param>
    public readonly struct ColumnAttribute(string key, VariantValue value)
    {
        /// <summary>
        /// Key.
        /// </summary>
        public string Key { get; } = key;

        /// <summary>
        /// Value.
        /// </summary>
        public VariantValue Value { get; } = value;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key">Attribute key.</param>
        public ColumnAttribute(string key) : this(key, VariantValue.Null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="key">Attribute key.</param>
        /// <param name="value">Attribute value.</param>
        public ColumnAttribute(string key, string value) : this(key, new VariantValue(value))
        {
        }
    }

    private SmallDictionary<string, VariantValue>? _attributes;

    /// <summary>
    /// Column metadata attributes.
    /// </summary>
    public IReadOnlyDictionary<string, VariantValue> Attributes
    {
        get => _attributes ??= new SmallDictionary<string, VariantValue>();
    }

    /// <summary>
    /// <c>True</c> if column has attribute.
    /// </summary>
    public bool HasAttributes => _attributes != null && _attributes.Count > 0;

    /// <summary>
    /// Should the column be visible on output. It doesn't affect column search.
    /// </summary>
    public bool IsHidden => Name.StartsWith("__", StringComparison.Ordinal);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    /// <param name="attributes">Attributes.</param>
    public Column(string name, DataType dataType, string? description = null, params ColumnAttribute[]? attributes)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (!DataTypeUtils.RowDataTypes.Contains(dataType))
        {
            throw new ArgumentOutOfRangeException(nameof(dataType),
                string.Format(Resources.Errors.InvalidColumnType, dataType, name));
        }

        Name = name;
        DataType = dataType;
        Description = description ?? string.Empty;
        Length = GetDefaultColumnLength();
        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                SetAttribute(attribute.Key, attribute.Value);
            }
        }
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(string name, DataType dataType, string? description = null) :
        this(name, dataType, description, null)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    /// <param name="attributes">Attributes.</param>
    public Column(string name, string sourceName, DataType dataType, string? description = null,
        params ColumnAttribute[]? attributes) :
        this(name, dataType, description, attributes)
    {
        SourceName = sourceName;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Column name.</param>
    /// <param name="sourceName">Source name.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(string name, string sourceName, DataType dataType, string? description = null) :
        this(name, dataType, description, null)
    {
        SourceName = sourceName;
    }

    /// <summary>
    /// Constructor. The name will be automatically generated.
    /// </summary>
    /// <param name="columnIndex">Column index.</param>
    /// <param name="dataType">Column type.</param>
    /// <param name="description">Description.</param>
    public Column(
        int columnIndex,
        DataType dataType,
        string? description = null) :
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
        if (column._attributes != null)
        {
            foreach (var attribute in column._attributes)
            {
                SetAttribute(attribute.Key, attribute.Value);
            }
        }
    }

    /// <summary>
    /// Set column metadata attribute. The existing value will be overridden.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Attribute value.</param>
    public void SetAttribute(string name, VariantValue value = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        _attributes = _attributes ??= new SmallDictionary<string, VariantValue>();
        _attributes[name] = value;
    }

    /// <summary>
    /// Set column metadata attribute. The existing value will be overridden.
    /// </summary>
    /// <param name="name">Attribute name.</param>
    /// <param name="value">Attribute value.</param>
    public void SetAttribute(string name, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        SetAttribute(name, new VariantValue(value));
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
            DataType.Blob => 25,
            DataType.Array => 45,
            DataType.Map => 55,
            _ => 4
        };
        if (Name.Length > size)
        {
            size = Name.Length;
        }
        return size;
    }

    public static bool NameEquals(string name1, string name2)
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
