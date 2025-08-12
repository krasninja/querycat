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
  JSON = 14, // JSON.
  ROWS_FORMATTER = 15, // Interfaces: IRowsFormatter.
  ANSWER_AGENT = 16 // Interfaces: IAnswerAgent.
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

  INVALID_REGISTRATION_TOKEN = 10,
  INVALID_AUTH_TOKEN = 11

  INVALID_FUNCTION = 20,
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
  NO_ACTION = 10,

  CANNOT_CAST = 100,
  CANNOT_APPLY_OPERATOR = 101,
  INVALID_COLUMN_INDEX = 102,
  INVALID_INPUT_STATE = 103
}

enum CursorSeekOrigin {
  BEGIN = 0,
  CURRENT = 1,
  END = 2
}

enum CompletionKind {
  MISC = 0,
  KEYWORD = 1,
  FUNCTION = 2,
  VARIABLE = 3,
  PROPERTY = 4,
  TEXT = 5,
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
  1: required i64 token, // Authorization token.
  2: required string version, // QueryCat version.
  3: optional LogLevel min_log_level // Minimal application log level.
}

struct ExecutionScope {
  1: required i32 id,
  2: required i32 parent_id // Get parent scope, -1 if root.
}

struct ScopeVariable {
  1: required string name,
  2: required VariantValue value
}

struct CompletionTextEdit {
  1: required i32 start,
  2: required i32 end,
  3: required string new_text
}

struct CompletionResult {
  1: required CompletionKind kind,
  2: required string label,
  3: required string documentation,
  4: required double relevance,
  5: required list<CompletionTextEdit> edits
}

struct StatisticRowError {
  1: required QueryCatErrorCode error_code,
  2: required i64 row_index,
  3: required i32 column_index,
  4: optional string value
}

struct Statistic {
  1: required i64 execution_time_ms,
  2: required i64 processed_count,
  3: required i64 errors_count,
  4: required list<StatisticRowError> errors
}

struct ModelDescription {
  1: required string name,
  2: required string description
}

struct QuestionRequest {
  1: required list<QuestionMessage> messages,
  2: required string type
}

struct QuestionMessage {
  1: required string content,
  2: required string role
}

struct QuestionResponse {
  1: required string answer,
  2: required string message_id
}

service PluginsManager {
  // Register plugin with all its data.
  RegistrationResult RegisterPlugin(
    // Token for initialization. It is provided thru command line arguments.
    1: required string registration_token,
    // Callback plugin server endpoint.
    // The endpoint is used to call plugin functions by qcat host.
    2: required string callback_uri,
    // Plugin information.
    3: required PluginData plugin_data
  ) throws (1: QueryCatPluginException e),

  // Call function.
  VariantValue CallFunction(
    1: required i64 token, // Authorization token.
    2: required string function_name,
    3: required list<VariantValue> args,
    4: Handle object_handle // Optional. It is used to call function of a specific object.
  ) throws (1: QueryCatPluginException e),

  // Run the query and return the last result.
  VariantValue RunQuery(
    1: required i64 token, // Authorization token.
    2: required string query,
    3: map<string, VariantValue> parameters
  ) throws (1: QueryCatPluginException e),

  // Set the configuration value.
  void SetConfigValue(
    1: required i64 token, // Authorization token.
    2: required string key,
    3: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Get configuration value.
  VariantValue GetConfigValue(
    1: required i64 token, // Authorization token.
    2: required string key
  ) throws (1: QueryCatPluginException e),

  // Get variable value. If variable doesn't exist - the NULL will be returned.
  VariantValue GetVariable(
    1: required i64 token, // Authorization token.
    2: required string name
  ) throws (1: QueryCatPluginException e),

  // Set the variable value. The new variable will be created or the existing value will
  // be overriden.
  VariantValue SetVariable(
    1: required i64 token, // Authorization token.
    2: required string name,
    3: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  list<ScopeVariable> GetVariables(
    1: required i64 token, // Authorization token.
    2: required i32 scope_id
  ) throws (1: QueryCatPluginException e),

  // Create the new variables and execution scope based on top of the current one.
  ExecutionScope PushScope(
    1: required i64 token // Authorization token.
  ) throws (1: QueryCatPluginException e),

  // Pop the current execution scope for stack and return it.
  ExecutionScope PopScope(
    1: required i64 token // Authorization token.
  ) throws (1: QueryCatPluginException e),

  // Get current top scope.
  ExecutionScope PeekTopScope(
    1: required i64 token // Authorization token.
  ) throws (1: QueryCatPluginException e),

  list<CompletionResult> GetCompletions(
    1: required i64 token, // Authorization token.
    2: required string text,
    3: required i32 position
  ),

  // Read binary data.
  binary Blob_Read(
    1: required i64 token, // Authorization token.
    2: required Handle object_blob_handle,
    3: required i32 offset,
    4: required i32 count
  ) throws (1: QueryCatPluginException e),

  // Write binary data.
  i64 Blob_Write(
    1: required i64 token, // Authorization token.
    2: required Handle object_blob_handle,
    3: required binary bytes
  ) throws (1: QueryCatPluginException e),

  // Get total binary length.
  i64 Blob_GetLength(
    1: required i64 token, // Authorization token.
    2: required Handle object_blob_handle
  ) throws (1: QueryCatPluginException e),

  // Get binary MIME content type.
  string Blob_GetContentType(
    1: required i64 token, // Authorization token.
    2: required Handle object_blob_handle
  ) throws (1: QueryCatPluginException e),

  // Logging.
  void Log(
    1: required i64 token, // Authorization token.
    2: required LogLevel level,
    3: required string message,
    4: list<string> arguments
  ) throws (1: QueryCatPluginException e),

  // Get query execution statistic.
  Statistic GetStatistic(
    1: required i64 token // Authorization token.
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

// Contains the general information.
struct ContextInfo {
  1: required i32 preread_rows_count,
  2: required bool skip_if_no_columns
}

service Plugin {
  // Call function.
  VariantValue CallFunction(
    1: required string function_name,
    2: required list<VariantValue> args,
    3: Handle object_handle // Optional. It is used to call function of a specific object.
  ) throws (1: QueryCatPluginException e),

  // Shutdown plugin. This should release all objects.
  void Shutdown() throws (1: QueryCatPluginException e),

  // Get columns of a rows set.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR, ROWS_OUTPUT.
  list<Column> RowsSet_GetColumns(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Open rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_Open(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Close rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_Close(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Reset rows set.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR, ROWS_OUTPUT.
  void RowsSet_Reset(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Current cursor position.
  // Supported objects: ROWS_ITERATOR with cursor support (ICursorRowsIterator).
  i32 RowsSet_Position(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Total rows.
  // Supported objects: ROWS_ITERATOR with cursor support (ICursorRowsIterator).
  i32 RowsSet_TotalRows(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Move cursor to the specific position. -1 is the special initial position.
  // Supported objects: ROWS_ITERATOR with cursor support (ICursorRowsIterator).
  void RowsSet_Seek(
    1: required Handle object_rows_set_handle,
    2: required i32 offset,
    3: required CursorSeekOrigin origin
  ) throws (1: QueryCatPluginException e),

  // Set context for rows set.
  // Supported objects: ROWS_INPUT, ROWS_OUTPUT.
  void RowsSet_SetContext(
    1: required Handle object_rows_set_handle,
    2: required ContextQueryInfo context_query_info,
    3: required ContextInfo context_info
  ) throws (1: QueryCatPluginException e),

  // Get rows.
  // Supported objects: ROWS_INPUT, ROWS_ITERATOR.
  RowsList RowsSet_GetRows(
    1: required Handle object_rows_set_handle,
    2: i32 count
  ) throws (1: QueryCatPluginException e),

  // Get unique key. It is a list of input data (input arguments) that
  // can be used to format cache key.
  // Supported objects: ROWS_INPUT.
  list<string> RowsSet_GetUniqueKey(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Get key columns.
  // Supported objects: ROWS_INPUT with keys columns support (IRowsInputKeys).
  list<KeyColumn> RowsSet_GetKeyColumns(
    1: Handle object_rows_set_handle
  ),

  // Set value for a key column.
  // Supported objects: ROWS_INPUT with keys columns support (IRowsInputKeys).
  void RowsSet_SetKeyColumnValue(
    1: required Handle object_rows_set_handle,
    2: required i32 column_index,
    3: required string operation,
    4: required VariantValue value
  ),

  // Unset value for a key column.
  // Supported objects: ROWS_INPUT with keys columns support (IRowsInputKeys).
  void RowsSet_UnsetKeyColumnValue(
    1: required Handle object_rows_set_handle,
    2: required i32 column_index,
    3: required string operation
  ),

  // Update the rows set value.
  // Supported objects: ROWS_INPUT with rows update support.
  QueryCatErrorCode RowsSet_UpdateValue(
    1: required Handle object_rows_set_handle,
    2: required i32 column_index,
    3: required VariantValue value
  ) throws (1: QueryCatPluginException e),

  // Write new row (values) to rows set.
  // Supported objects: ROWS_OUTPUT.
  QueryCatErrorCode RowsSet_WriteValues(
    1: required Handle object_rows_set_handle,
    2: required list<VariantValue> values // Should match columns count.
  ) throws (1: QueryCatPluginException e),

  // Delete the current row.
  QueryCatErrorCode RowsSet_DeleteRow(
    1: required Handle object_rows_set_handle
  ) throws (1: QueryCatPluginException e),

  // Get rows set model description.
  ModelDescription RowsSet_GetDescription(
    1: required Handle object_handle
  ) throws (1: QueryCatPluginException e),

  // Create input from BLOB.
  Handle RowsFormatter_OpenInput(
    1: required Handle object_rows_formatter_handle, // Rows formatter object handle.
    2: required Handle object_blob_handle, // BLOB object handle.
    3: string key // Unique key.
  ) throws (1: QueryCatPluginException e),

  // Create output with BLOB.
  Handle RowsFormatter_OpenOutput(
    1: required Handle object_rows_formatter_handle, // Rows formatter object handle.
    2: required Handle object_blob_handle // BLOB object handle.
  ) throws (1: QueryCatPluginException e),

  // Read binary data.
  binary Blob_Read(
    1: required Handle object_blob_handle,
    2: required i32 offset,
    3: required i32 count
  ) throws (1: QueryCatPluginException e),

  // Write binary data.
  i64 Blob_Write(
    1: required Handle object_blob_handle,
    2: required binary bytes
  ) throws (1: QueryCatPluginException e),

  // Get total binary length.
  i64 Blob_GetLength(
    1: required Handle object_blob_handle
  ) throws (1: QueryCatPluginException e),

  // Get binary MIME content type.
  string Blob_GetContentType(
    1: required Handle object_blob_handle
  ) throws (1: QueryCatPluginException e),

  // The method is called to ask client to start new server so QueryCat host can make additional connection.
  string Serve(
  ) throws (1: QueryCatPluginException e),

  // Get an answer from agent based on question.
  QuestionResponse AnswerAgent_Ask(
    1: required Handle object_answer_agent_handle,
    2: required QuestionRequest request
  ) throws (1: QueryCatPluginException e)
}
