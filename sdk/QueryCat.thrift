namespace netstd QueryCat.Plugins.Sdk
namespace cpp querycat.plugins.sdk
namespace py querycat.plugins.sdk
namespace rb querycat.plugins.sdk
namespace js QueryCat.Plugins.Sdk

/*
 * Common Types
 * ------------
 */

typedef i64 Timestamp; // Unix timestamp.
typedef i64 Duration;
typedef i32 Handle;

// Decimal value. See the doc below for more info:
// https://learn.microsoft.com/en-us/dotnet/architecture/grpc-for-wcf-developers/protobuf-data-types#creating-a-custom-decimal-type-for-protobuf
struct DecimalValue {
  1: i64 units,
  2: i32 nanos
}

// Supported plugins objects types.
enum ObjectType {
  GENERIC = 0,
  ROWS_INPUT = 10, // Interfaces: IRowsInput, IRowsSource, IRowsInputKeys, IRowsSchema.
  ROWS_ITERATOR = 11, // Interfaces: IRowsIterator.
  ROWS_OUTPUT = 12, // Interfaces: IRowsOutput, IRowsSource.
  BLOB = 13, // Binary data.
  JSON = 14 // JSON.
}

// To refer to objects we use special identifiers: handles.
struct ObjectValue {
  1: required ObjectType type,
  2: required i32 handle,
  3: required string name
}

union VariantValue {
  1: bool isNull,
  2: i64 integer,
  3: string string,
  4: double float,
  5: Timestamp timestamp,
  6: bool boolean,
  7: DecimalValue decimal,
  8: Duration interval,
  9: ObjectValue object,
  10: string json
}

enum DataType {
  VOID = -1,
  NULL = 0,
  INTEGER = 1,
  STRING = 2,
  FLOAT = 3,
  TIMESTAMP = 4,
  BOOLEAN = 5,
  NUMERIC = 6,
  INTERVAL = 7,
  BLOB = 8,
  OBJECT = 40, // See ObjectType.
  DYNAMIC = 41
}

enum LogLevel {
  TRACE = 0,
  DEBUG = 1,
  INFORMATION = 2,
  WARNING = 3,
  ERROR = 4,
  CRITICAL = 5,
  NONE = 6
}

// Exception error types.
enum ErrorType {
  SUCCESS = 0,
  GENERIC = 1,
  INVALID_OBJECT = 2,
  NOT_SUPPORTED = 3,
  INTERNAL = 4,
  ARGUMENT = 5,

  INVALID_AUTH_TOKEN = 10,
  INVALID_FUNCTION = 11
}

// The error code that is returned by several QueryCat methods.
enum QueryCatErrorCode {
  OK = 0,
  ERROR = 1,
  DELETED = 2,
  NO_DATA = 3,
  NOT_SUPPORTED = 4,
  NOT_INITIALIZED = 5,
  ACCESS_DENIED = 6,
  INVALID_ARGUMENTS = 7,
  ABORTED = 8,
  CLOSED = 9,

  CANNOT_CAST = 100,
  CANNOT_APPLY_OPERATOR = 101,
  INVALID_COLUMN_INDEX = 102,
  INVALID_INPUT_STATE = 103
}

exception QueryCatPluginException {
  1: required ErrorType type,
  2: required string error_message,
  3: optional i32 object_handle,
  4: optional string exception_type,
  5: optional string exception_stack_trace,
  6: optional QueryCatPluginException exception_nested
}

/*
 * Plugins Manager
 * ---------------
 */

struct Function {
  // Function call signature.
  1: required string signature,
  2: required string description,
  // Is it for aggregate queries?
  3: required bool is_aggregate,
  // The function is safe if it only reads data.
  4: optional bool is_safe,
  // Register formatter for a specific file extensions or MIME types.
  5: optional list<string> formatter_ids
}

struct PluginData {
  // List of all functions signatures that plugin provides.
  1: required list<Function> functions,
  // Plugin name.
  2: required string name,
  // Version. Format is MAJOR.MINOR.PATCH .
  3: required string version
}

struct RegistrationResult {
  1: required string version, // QueryCat version.
  2: required list<i32> functions_ids // Registered functions identifiers.
}

service PluginsManager {
  // Register plugin with all its data.
  RegistrationResult RegisterPlugin(
    // Token for initialization. It is provided thru command line arguments.
    1: required string auth_token,
    // Callback plugin server endpoint.
    // The endpoint is used to call plugin functions by qcat host.
    2: required string callback_uri,
    // Plugin information.
    3: required PluginData plugin_data
  ) throws (1: QueryCatPluginException e),

  // Call function.
  VariantValue CallFunction(
    1: required string function_name,
    2: required list<VariantValue> args,
    3: Handle object_handle // Optional. It is used to call function of a specific object.
  ) throws (1: QueryCatPluginException e),

  // Run the query and return the last result.
  VariantValue RunQuery(
    1: required string query,
    2: map<string, VariantValue> parameters
  ) throws (1: QueryCatPluginException e),

  // Set the configuration value.
  void SetConfigValue(
    1: required string key,
    2: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Get configuration value.
  VariantValue GetConfigValue(
    1: required string key
  ) throws (1: QueryCatPluginException e),

  // Get variable value. If variable doesn't exist - the NULL will be returned.
  VariantValue GetVariable(
    1: required string name
  ) throws (1: QueryCatPluginException e),

  // Set the variable value. The new variable will be created or the existing value will
  // be overriden.
  VariantValue SetVariable(
    1: required string name,
    2: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Logging.
  void Log(
    1: required LogLevel level,
    2: required string message,
    3: list<string> arguments
  ) throws (1: QueryCatPluginException e)
}

/*
 * Plugin
 * ------
 */

struct Column {
  1: i32 id,
  2: required string name,
  3: required DataType type,
  4: optional string description
}

struct RowsList {
  1: bool has_more, // True if has more values. If false - no need to call MoveNext() method.
  2: required list<VariantValue> values // Values, in total should be ColumnsCount * BatchSize.
}

struct KeyColumn {
  1: required i32 column_index, // Column index in rows iterator or table.
  2: required bool is_required, // If required - user must specify the column in WHERE block.
  3: required list<string> operations // Supported operations.
}

// Contains the information about the executing query. Can be used for optimization.
struct ContextQueryInfo {
  1: required list<Column> columns,
  2: required i64 offset,
  3: optional i64 limit
}

service Plugin {
  // Call function.
  VariantValue CallFunction(
    1: required string function_name,
    2: required list<VariantValue> args,
    3: Handle object_handle // Optional. It is used to call function of a specific object.
  ) throws (1: QueryCatPluginException e),

  // Initialize plugin.
  void Initialize() throws (1: QueryCatPluginException e),

  // Shutdown plugin. This should release all objects.
  void Shutdown() throws (1: QueryCatPluginException e),

  // Get columns of a rows set.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR, ROWS_OUTPUT.
  list<Column> RowsSet_GetColumns(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Open rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_Open(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Close rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_Close(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Reset rows set.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR, ROWS_OUTPUT.
  void RowsSet_Reset(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Set context for rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_SetContext(
    1: required Handle object_handle,
    2: required ContextQueryInfo context_query_info
  ) throws (1: QueryCatPluginException e),

  // Get rows.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR.
  RowsList RowsSet_GetRows(
    1: required Handle object_handle,
    2: i32 count
  ) throws (1: QueryCatPluginException e),

  // Get unique key. It is a list of input data (input arguments) that
  // can be used to format cache key.
  // Supported objects: ROWS_INPUT.
  list<string> RowsSet_GetUniqueKey(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Get key columns.
  // Supported objects: ROWS_INPUT with keys columns support (IRowsInputKeys).
  list<KeyColumn> RowsSet_GetKeyColumns(
    1: Handle object_handle
  ),

  // Set value for a key column.
  // Supported objects: ROWS_INPUT with keys columns support (IRowsInputKeys).
  void RowsSet_SetKeyColumnValue(
    1: required Handle object_handle,
    2: required i32 column_index,
    3: required string operation,
    4: required VariantValue value
  ),

  // Update the rows set value.
  // Supported objects: ROWS_INPUT with rows update support.
  QueryCatErrorCode RowsSet_UpdateValue(
    1: required Handle object_handle,
    2: required i32 column_index,
    3: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Write new row (values) to rows set.
  // Supported objects: ROWS_OUTPUT.
  QueryCatErrorCode RowsSet_WriteValues(
    1: required Handle object_handle,
    2: required list<VariantValue> values // Should match columns count.
  ) throws (1: QueryCatPluginException e),

  // Delete the current row.
  QueryCatErrorCode RowsSet_DeleteRow(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Read binary data.
  binary Blob_Read(
    1: required Handle object_handle,
    2: required i32 offset,
    3: required i32 count
  ) throws (1: QueryCatPluginException e),

  // Get total binary length.
  i64 Blob_GetLength(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e)
}
