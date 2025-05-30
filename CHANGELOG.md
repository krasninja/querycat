# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Add Compact Log Event Format (CLEF) formatter.
- Add functions "delay_input", "delay_output", "to_base64", "from_base64".
- Add "call" command.

### Fixed

- Postpone thread start in "buffer_input", "buffer_output".

## [0.13.0] - 2025-05-06

### Added

- Initial implementation of Delete command.
- Add functions "cache_input", "blob_from_file", "date_add".
- Extended support for BLOBs.
- Allow to set the list of allowed commands to execute.

### Changed

- "Declare" command does not need type statement anymore.

### Fixed

- Improve Update command support.
- Minor bugfixes.

### Removed

- Remove VariableResolving and VariableResolved events.

## [0.12.1] - 2025-03-16

### Added

- Add functions "buffer_input", "buffer_output", "json_array_elements".
- Support "IRowsFormatter" for Thrift plugins system.
- Add option "overwrite" to "plugin install" command.

### Fixed

- Improve and bugfix Thrift plugins support.
- Do not load plugin proxy if it already exists.

## [0.11.0] - 2025-02-17

### Added

- Initial implementation of plugins native libraries loading.
- Add new option PreventConcurrentRun.
- Add functions "retry_input", "retry_output" and "parallel_output".

### Changed

- Make IObjectSelector async.

### Removed

- Remove ConcurrencyLevel option.
- Remove SubRip formatter.

## [0.10.0] - 2025-01-28

### Changed

- Refactor functions manager interface.

### Added

- Initial async functions support.
- Add new Unset method for rows input keys.

### Fixed

- Various bugfixes.

## [0.9.0] - 2025-01-07

### Changed

- Upgrade to .NET 9.0.
- Install plugins proxy on any plugin install.
- Introduce async interfaces instead of Sync. Use AsyncEnumerableRowsInput instead of FetchRowsInput.

### Added

- New GetVariable and SetVariable Thrift methods.
- Allow to specify several plugin directories with separator (f.e. "--plugin-dirs=~/dir1:~/dir2").

### Fixed

- Support "Select alias.* from x" pattern.
- Integer parse overflow.
- Return non-zero exit code on error.
- Support keys set construction for "x in (select 1 union select 2)" case.

## [0.8.0] - 2024-11-10

### Changed

- Show error if there is invalid column in "EXCEPT" clause of "SELECT" command.
- Replace "FunctionCallInfo" by "IExecutionThread" in function signature.
- Minor improvements and optimizations.

## [0.7.0] - 2024-09-12

### Added

- Initial implementation of completion system.

### Changed

- New plugins system. Now, the special plugin proxy should be used to load plugins.

### Fixed

- Minor performance optimizations and bugfixes.
- Improve strings concatenation with other types.

### Added

- Add MaxRecursionDepth option.

### Fixed

- Fix index property access in DefaultObjectSelector.

## [0.6.9] - 2024-08-06

### Added

- Support FORMAT block for SELECT * INTO construction.
- Export to CSV and clipboard in web UI.
- Add function "json_array_length".

### Changed

- Update JSON Path package.
- Extent aggregate functions type's support.

### Fixed

- Unquote return of JSON functions "json_query" and "json_value".
- Fix GROUP BY reset bug.
- Possible concurrency problem in Run method.

## [0.6.7] - 2024-06-04

### Added

- Allow to evaluate filter on object expressions lists "users[? @.Name='John' ].Phone".
- Try to run "rc.sql" from the working directory.
- Add "except" keyword for "select" command.

### Fixed

- Fix object expressions wrong operation apply.
- Allow to specify simple expressions in FROM block.
- Fix incorrect schema detection bug with gzip files.
- Improve Thrift plugins support

### Removed

- Remove "object_query" function.

## [0.6.4] - 2024-05-20

### Added

- Initial implementation of IF command.
- Allow to order by column number.
- Allow to set up application culture "Application.Culture".

### Fixed

- Improve object selector.
- Allow to call execution thread Run recursively.

### Changed

- Refactor and update objects selector logic.

## [0.6.1] - 2024-05-14

### Added

- Show different icons for different files types in Web UI.

### Fixed

- Improve object selector.

## [0.6.0] - 2024-04-30

### Added

- Initial objects expressions support.
- Allow to use Add operator with strings. Implicit conversion from integer, float and numeric types when concat with string.
- Add "Enter" and "Q" keys to processing in "follow" mode.
- Add "tail" option.
- Experimental AST cache.

### Changed

- Change the way how quote columns. Now, instead of [] use "". Only '' can be used for string literals.

### Fixed

- Improve memory usage for web server partial ranges requests.

## [0.5.5] - 2024-03-27

### Added

- Add "Call" command to execute functions.
- Add "timeout" option.
- Unescape support for octet and hex numbers.

### Fixed

- Fix inner join bug on CTE.

## [0.5.4] - 2024-02-28

### Added

- Safe mode.
- Allow web UI access restriction by available IPs slots count.
- Support file URI scheme.

### Fixed

- Avoid double plugins loading.

## [0.5.3] - 2024-01-30

### Added

- Add breadcrumbs in files UI, overall web UI improvements.
- Add schema and reset web UI buttons.
- Allow restricting web UI access by IP addresses.

### Fixed

- Cannot apply expressions on "row_number" column.
- Fix files download 404 problem on Windows.
- Fix "size_pretty" function format.
- Fix the bug when 'q' press doesn't close the app.
- Other minor fixes.

## [0.5.0] - 2023-12-31

### Added

- Use Native AOT compilation mode. It reduces file size and startup time.
- Migration to Thrift plugins system.
- Support hexadecimal numbers parsing.
- Initial Grok patterns support.
- Log web server queries, update dependencies.
- Add new table output style "Table2".
- Support library plugins.
- Add "ls_dir" function.
- Add glob pattern to search thru all directories. For example, "select * from '/home/user/**/*.txt'".
- Resolve '~' to user home directory.
- Implement simple HTTP files server (beta).

### Fixed

- Fix using aliases with tables from variables.

## [0.4.14] - 2023-11-15

### Added

- Initial "follow" mode implementation.
- Add regular expressions formatter "regex".

### Fixed

- JsonInput now produces warning on parse error instead of crash.
- Minor fixes and improvements.

## [0.4.12] - 2023-09-25

### Added

- Add "generate_series()" function.
- Add aliases for types integer (int, int8), float (real), numeric (decimal), boolean (bool) and string (text).

### Fixed

- Fix description column text for "_functions()" function output.

### Changed

- Use ZIP format for Windows target.

## [0.4.11] - 2023-08-31

### Added

- Move IISW3C formatter and input back from plugin to core.
- Add SubRip (.srt) files formatter.
- Add functions "string_to_table", "regexp_replace".
- Allow to use subqueries with IN clause.

### Fixed

- Create directory for file if it is not exists.
- "_functions" doesn't work correctly for formatters.

### Changed

- Improved Thrift plugins support system.
- Various refactorings to improve system modularity.

## [0.4.8] - 2023-07-30

### Changed

- Refactor query context.
- Refactor plugins subsystem.

### Fixed

- Fix log files parse.

## [0.4.7] - 2023-06-26

### Added

- Add "--no-header", "--float-format" command line arguments.
- Add "NoSpaceTable" output style.
- Support strings unescape.
- Add more arguments to function "csv".
- Allow to pass arguments to formatters. Example: "SELECT * FROM '1.csv??has_header=false'".
- Add JSON path argument to function "json".

### Fixed

- Fix file parsing without a new line at the end.
- Function arguments are case insensitive.

## [0.4.5] - 2023-05-29

### Added

- Ability to pass variables as command line arguments (`qcat --var size=10`).
- Support "yyMMdd", "yyyyMMdd" timestamp formats.

### Fixed

- Alias is not taken into account in "SELECT * FROM (VALUES (...), ...) AS a" expression.
- Fix limit-offset order in Fetcher.
- Fix async utils problem in case of exception.
- Fix incorrect empty cache reuse bug.
- Process double quotes on strings.
- Improve XML XPath support.
- Fix distinct method for all columns.
- Minor improvements.

## [0.4.0] - 2023-04-30

### Added

- Initial implementation of Update and Insert commands.
- Add function Self.

### Fixed

- Correct processing names with aliases (like "tbl.[col]").
- DsvInput reset if it has header.

### Changed

- Class renames: ClassEnumerableInput to FetchInput, ClassEnumerableInputFetch to Fetcher.
- Change logger to Microsoft.Extensions.Logging.

## [0.3.5] - 2023-03-26

### Added

- Support SIMILAR TO statement.
- Paginator now handles keys press. "A" to show all, "Q" to exit.
- Initial implementation of "string_agg", "first_value" and "last_value" aggregate functions.
- Add math "ln", "log", "asin", "acos", "atan" functions.
- Add "object_query", "_size_pretty", "_timezone_names" functions.
- Add string functions "starts_with", "split_part", "regexp_count", "regexp_substr".
- Allow to read/write from/to gzip archives.
- Add "VALUES", "AT TIME ZONE" clauses.
- Add "analyze-rows", "columns-separator", "disable-cache" options.

### Changed

- "More" keyword is cleared on every page request.
- Improve cache support.

### Fixed

- Fix files buffer reading handle.

## [0.3.0] - 2023-02-24

### Added

- New "chr" string function.
- Add JSON functions: "is_json", "json_query", "json_value", "to_json", "json_exists".
- Add "date_trunc" function.
- Initial implementation of recursive CTE.
- Make AS keyword optional for alias.
- HTTP server now has simple web UI.
- HTTP server supports CORS origin and basic authentication.
- Add "DAYOFYEAR", "WEEKDAY" timestamp parts.
- Initial implementation of XML formatter (input and output).
- Initial implementation of Window functions.

### Changed

- SQL planner refactoring, fixed various cases where columns name resolve did not work.

### Fixed

- Columns input prefetcher didn't work in some cases.
- COUNT produced NULL result on empty dataset.
- Lot of various fixes and improvements.

## [0.2.0] - 2023-01-13

### Added

- Initial plugins support.
- New commands: Declare, Set.
- Add new features to Select command: CTE (common table expressions), joins (inner, left, outer), order by nulls first/last,
case switch, distinct on, union, intersect, except.
- Implement 3VL compare logic.
- Add hash functions: "md5", "sha1", "sha256", "sha384", "sha512".
- Update logging output (migrated to Serilog library).
- Support bootstrap script "rc.sql".
- Add "quote_strings" param to csv and tsv functions.
- Add "uuid" function that generates UUID.
- Add "_typeof" function.
- Allow to override auto-detected type by Cast.
- Allow to use with shebang.
- Show the line and position where syntax error occurs.
- Add "nop" function.

### Fixed

- Various Select command improvements and fixes.
- Documentation fixes.
- Cache logic fixes.

### Changed

- The IIS W3C logs parser was moved into a plugin. Take a look at "QueryCat.Plugins.Logs" plugin.
- Improve Interval type support.

## [0.1.0] - 2022-11-17

### Added

- This is the first pre-production release of the tool. For now, it is able to parse CSV and JSON files and do simple SQL queries. As an additional feature, it can parse IISW3C logs.
