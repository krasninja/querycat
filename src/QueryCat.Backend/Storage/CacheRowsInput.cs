using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The input caches input data and after reset reads it from memory instead.
/// </summary>
public sealed class CacheRowsInput : IRowsInput
{
    private const int ChunkSize = 4096;

    private readonly IRowsInput _rowsInput;
    private readonly ChunkList<VariantValue> _cache;
    private readonly ChunkList<bool> _cacheInvalidation;
    private int _rowIndex = -1;
    private int _cacheLength = 0;

    private QueryContext _queryContext = new EmptyQueryContext();

    /// <inheritdoc />
    public Column[] Columns => _rowsInput.Columns;

    public CacheRowsInput(IRowsInput rowsInput)
    {
        _rowsInput = rowsInput;
        _cache = new ChunkList<VariantValue>(ChunkSize);
        _cacheInvalidation = new ChunkList<bool>(ChunkSize);
    }

    /// <inheritdoc />
    public void Open() => _rowsInput.Open();

    /// <inheritdoc />
    public void SetContext(QueryContext queryContext)
    {
        _queryContext = queryContext;
        _rowsInput.SetContext(queryContext);
    }

    /// <inheritdoc />
    public void Close() => _rowsInput.Close();

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        var offset = Columns.Length * _rowIndex + columnIndex;
        if (_rowIndex < _cacheLength && _cacheInvalidation[offset])
        {
            value = _cache[offset];
            return ErrorCode.OK;
        }

        var errorCode = _rowsInput.ReadValue(columnIndex, out value);
        _cache[offset] = value;
        _cacheInvalidation[offset] = true;
        return errorCode;
    }

    /// <inheritdoc />
    public bool ReadNext()
    {
        _rowIndex++;

        // Read cache data if possible.
        if (_rowIndex < _cacheLength)
        {
            return true;
        }

        var hasData = _rowsInput.ReadNext();
        if (hasData)
        {
            // Otherwise increase cache and read data.
            for (int i = 0; i < Columns.Length; i++)
            {
                _cache.Add(VariantValue.Null);
                _cacheInvalidation.Add(false);
            }
            _cacheLength++;
        }

        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _rowIndex = -1;
    }
}
