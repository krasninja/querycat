# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Support SIMILAR TO statement.
- Paginator now handles keys press. "A" to show all, "Q" to exit.
- Initial implementation of "string_agg", "first_value" and "last_value" aggregate functions.
- Add math "ln", "log", "asin", "acos", "atan" functions.
- Add "object_query", "_size_pretty", "_timezone_names" functions.
- Add string functions "starts_with", "split_part", "regexp_count", "regexp_substr".
- Allow to read/write from/to gzip archives.
- Add "VALUES", "AT TIME ZONE" clauses.
- Add options "analyze-rows", "columns-separator".

### Changed

- "More" keyword is cleared on every page request.
- Improve cache support.

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
