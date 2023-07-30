namespace netstd QueryCat.Plugins.Sdk
namespace cpp querycat.plugins.sdk
namespace py querycat.plugins.sdk
namespace rb querycat.plugins.sdk

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

enum ObjectType {
  ROWS_INPUT = 0,
  ROWS_ITERATOR = 1
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
  9: ObjectValue object
}

enum DataType {
  NULL = 0,
  INTEGER = 1,
  STRING = 2,
  FLOAT = 3,
  TIMESTAMP = 4,
  BOOLEAN = 5,
  NUMERIC = 6,
  INTERVAL = 7,
  OBJECT = 40
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

enum ErrorType {
  SUCCESS = 0,
  GENERIC = 1,
  INVALID_OBJECT = 2,

  INVALID_AUTH_TOKEN = 10,
  INVALID_FUNCTION = 11
}

exception QueryCatPluginException {
  1: required ErrorType type,
  2: required string error_message,
  3: optional i32 object_handle
}

/*
 * Plugins Manager
 * ---------------
 */

struct PluginData {
  // List of all functions signatures that plugin provides.
  1: required list<string> functions,
  // Plugin name.
  2: required string name,
  // Version.
  3: required string version
}

service PluginsManager {
  // Register plugin data.
  void RegisterPlugin(
    // Token for initialization.
    1: required string auth_token,
    // Callback plugin server endpoint.
    2: required string callback_uri,
    // Plugin data.
    3: required PluginData plugin_data
  ) throws (1: QueryCatPluginException e),

  // Call function.
  VariantValue CallFunction(
    1: required string function_name,
    2: required list<VariantValue> args,
    3: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Set configuration value.
  void SetConfigValue(
    1: required string key,
    2: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Get configuration value.
  VariantValue GetConfigValue(
    1: required string key
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
  1: bool has_more,
  2: required list<VariantValue> values
}

struct KeyColumn {
  1: required string name,
  2: required bool is_required,
  3: required list<string> operations
}

struct ContextQueryInfo {
  1: required list<string> columns,
  2: required i64 offset,
  3: optional i64 limit
}

service Plugin {
  // Call function.
  VariantValue CallFunction(
    1: required string function_name,
    2: required list<VariantValue> args,
    3: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Initialize plugin.
  void Initialize() throws (1: QueryCatPluginException e),

  // Shutdown plugin. This should release all objects.
  void Shutdown() throws (1: QueryCatPluginException e),

  // Get columns.
  list<Column> RowsSet_GetColumns(
    1: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Open rows set.
  void RowsSet_Open(
    1: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Close rows set.
  void RowsSet_Close(
    1: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Reset rows set.
  void RowsSet_Reset(
    1: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Set context for rows set.
  void RowsSet_SetContext(
    1: Handle object_handle,
    2: ContextQueryInfo context_query_info
  ) throws (1: QueryCatPluginException e),

  // Get rows.
  RowsList RowsSet_GetRows(
    1: Handle object_handle,
    2: i32 count
  ) throws (1: QueryCatPluginException e),

  // Get unique key. It is a list of input data (input arguments) that
  // can be used to format cache key.
  list<string> RowsSet_GetUniqueKey(
    1: Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Get key columns.
  list<KeyColumn> RowsSet_GetKeyColumns(
    1: Handle object_handle
  ),

  // Set value for a key column.
  void RowsSet_SetKeyColumnValue(
    1: Handle object_handle,
    2: required string column_name,
    3: required string operation,
    4: required VariantValue value
  ),
}
