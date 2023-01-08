# Changelog

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

### Fixed

- Various Select command improvements and fixes.
- Documentation fixes.
- Cache logic fixes.

### Changed

- The IIS W3C logs parser was moved into a plugin. Take a look at "QueryCat.Plugins.Logs" plugin.

## [0.1.0] - 2022-11-17

### Added

This is the first pre-production release of the tool. For now, it is able to parse CSV and JSON files and do simple SQL queries. As an additional feature, it can parse IISW3C logs.
