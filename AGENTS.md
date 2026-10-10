# AGENTS.md

QueryCat - a CLI tool and .NET library that queries CSV, JSON, XML and log files with SQL. Ships as a single AOT-compiled binary (`qcat`) and as NuGet packages.

## Repository Layout

- [src/](src/) - application and library projects.
    - [QueryCat.Backend.Core/](src/QueryCat.Backend.Core/) - public abstractions: `VariantValue`, `Column`, `Row`, `IRowsInput`/`IRowsOutput`/`IRowsIterator`, `IExecutionThread`, functions API, plugins API. Keep this stable - plugins depend on it.
    - [QueryCat.Backend/](src/QueryCat.Backend/) - SQL engine: ANTLR parser, AST, commands, execution, formatters, storage.
    - [QueryCat.Backend.Addons/](src/QueryCat.Backend.Addons/) - optional formatters/functions (JSON etc.) kept out of the core engine.
    - [QueryCat.Backend.ThriftPlugins/](src/QueryCat.Backend.ThriftPlugins/) - Thrift-based plugin host (default plugin mode).
    - [QueryCat.Backend.AssemblyPlugins/](src/QueryCat.Backend.AssemblyPlugins/) - reflection-based plugin host (alternative mode).
    - [QueryCat.PluginsProxy/](src/QueryCat.PluginsProxy/) - proxy executable running plugin DLLs on full .NET.
    - [QueryCat.Cli/](src/QueryCat.Cli/) - `qcat` entry point, commands, web server/REST UI.
    - [QueryCat.Plugins.Sample/](src/QueryCat.Plugins.Sample/) - reference plugin.
    - [QueryCat.Build/](src/QueryCat.Build/) - Cake (Frosting) build automation.
    - [QueryCat.UnitTests/](src/QueryCat.UnitTests/) - xUnit unit tests.
    - [QueryCat.IntegrationTests/](src/QueryCat.IntegrationTests/) - xUnit integration tests + YAML query tests.
    - [QueryCat.Tests.QueryRunner/](src/QueryCat.Tests.QueryRunner/) - shared harness for the YAML query tests.
    - [QueryCat.Benchmarks/](src/QueryCat.Benchmarks/), [QueryCat.Samples/](src/QueryCat.Samples/), [QueryCat.Tester/](src/QueryCat.Tester/), [TimeIt/](src/TimeIt/) - supporting tools.
- [sdk/](sdk/) - plugin protocol and client.
    - [QueryCat.thrift](sdk/QueryCat.thrift) - plugin protocol definition (source of truth).
    - [dotnet-sdk/](sdk/dotnet-sdk/) - Thrift-generated C# code, **do not edit by hand**.
    - [dotnet-client/](sdk/dotnet-client/) - hand-written plugin client library (`QueryCat.Plugins.Client`).
    - [openapi.yaml](sdk/openapi.yaml) - OpenAPI spec of the built-in web server REST API (`qcat serve`); keep it in sync with the CLI web server.
- [docs/](docs/) - MkDocs sources published to readthedocs.

## Architecture

Pipeline: SQL text -> ANTLR lexer/parser (`Backend/Parser`) -> AST (`Backend/Ast`) -> visitors build commands (`Backend/Commands`) -> `IRowsIterator` chain -> output. Everything is streaming and async so large inputs never load fully into memory.

Key abstractions (all in `QueryCat.Backend.Core`, documented in [docs/development/components.md](docs/development/components.md)):

- `VariantValue` - universal value type (string, number, object, null).
- `IRowsInput` - low-level value-level source used by `FROM`; `IRowsInputKeys` adds pushdown key columns, so inputs can filter at the source.
- `IRowsOutput` - sink used by `INTO`.
- `IRowsIterator` - row-level iterator; the query plan is a composition of these.
- `IExecutionThread` - execution state: scopes, variables, stack, functions manager.
- `QueryContext` - selected columns, limit/offset and shared key-value config for an input.

Functions are plain static methods with `[FunctionSignature("name(a: integer): integer")]` and `[Description]`, taking `IExecutionThread` (plus `CancellationToken` for async) and reading arguments from `thread.Stack`. Mark read-only functions `[SafeFunction]` so they work in safe mode. See [docs/development/functions.md](docs/development/functions.md).

Plugins run out-of-process over Thrift: `qcat <-> Thrift <-> proxy <-> plugin assembly`. The protocol lives in [sdk/QueryCat.thrift](sdk/QueryCat.thrift); regenerate `sdk/dotnet-sdk/` with the command in [docs/internal/thrift.md](docs/internal/thrift.md) after changing it, and never hand-edit generated files.

## Build & Test

Builds go through Cake via `./build.sh` (`build.ps1` on Windows). Requires .NET 10, plus ANTLR v4, GitVersion and Apache Thrift for the corresponding maintenance tasks.

```bash
./build.sh -t Run-Tests          # unit + integration tests (what CI runs)
./build.sh -t Build-Linux        # also Build-Windows, Build-Mac, Build-Package
./build.sh -t Build-Grammar      # regenerate C# from the ANTLR .g4 files
./build.sh -t Clean
```

`./build.sh -t Build-Linux -- --PublishAot=false --Properties='Plugin=Assembly'` switches off AOT and selects the assembly plugin host. See [docs/development/build-tasks.md](docs/development/build-tasks.md).

Tests are xUnit with `Arrange. / Act. / Assert.` comment sections and `Method_Scenario_ShouldExpectation` naming. Most SQL behavior is covered by declarative YAML cases in [src/QueryCat.IntegrationTests/Tests/](src/QueryCat.IntegrationTests/Tests/) (`query:` + `expected:`) - prefer adding a YAML case over new C# test code when testing query semantics.

## Conventions

- Style is enforced by [src/.editorconfig](src/.editorconfig) and StyleCop analyzers; 4 spaces, file-scoped namespaces, braces always, `_camelCase` private fields, usings outside the namespace with System first. Warnings aren't errors, but don't add new ones.
- Nullable reference types and implicit usings are on everywhere. Keep them satisfied rather than suppressing.
- Everything must stay **AOT-compatible**: no unbounded reflection, use the project's own `SourceGenerationContext` (Backend, Addons, ThriftPlugins and Cli each have one) for JSON serialization.
- `QueryCat.Backend.Core`, `QueryCat.Backend` and the plugin client multi-target `net8.0;net10.0`; the generated `sdk/dotnet-sdk` is `net8.0` only; the CLI, addons and plugin hosts are `net10.0` only. Don't use newer APIs in the multi-targeted projects without a guard.
- I/O is async-first: pass `CancellationToken` through and suffix async members with `Async`.
- User-facing strings and exception messages live in the project's own `Resources/Errors.resx` / `Messages.resx` (`throw new QueryCatException(Resources.Errors.NoColumns)`), avoid using inline literals.
- Public API in `.Core` gets XML doc comments; overrides use `/// <inheritdoc />`.
- Generated code is checked in but never edited by hand: ANTLR output in `src/QueryCat.Backend/Parser` (`QueryCatLexer.cs`, `QueryCatParser*.cs`, `gen/`; the other files there, like `ProgramParserVisitor.*.cs` and `AstBuilder.cs`, are hand-written), Thrift output under `sdk/dotnet-sdk/`, `*.Designer.cs` resources.

## Workflow

- Git flow: `develop` is the default branch and the PR target; `main` holds releases.
- Update [CHANGELOG.md](CHANGELOG.md) for user-visible changes; update [docs/](docs/) when behavior, commands, functions or formats change.
