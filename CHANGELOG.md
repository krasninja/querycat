# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## Added

- Initial "follow" mode implementation.
- Add regular expressions formatter "regex".

## Fixed

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

## Fixed

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
