# SDK

You can use QueryCat directly in your .NET app. Install the following [NuGet](https://www.nuget.org/packages/QueryCat) package:

```
Install-Package QueryCat
```

or

```
$ dotnet add package QueryCat
```

## Basic Usage

The main object is `ExecutionThread` that allows to execute queries. Basic usage:

```csharp
var executionThread = new ExecutionThreadBootstrapper()
    .WithStandardFunctions()
    .WithStandardUriResolvers()
    .Create();
var result1 = executionThread.Run("1+1"); // 2
var result2 = executionThread.Run('uuid()'); // 63af7231-f182-4d29-816c-b83b9dc9cff5
```

Result is the `VariantValue` returned by the last statement in the script.

## Execution Thread Options

`ExecutionOptions` class that allows to customize execution thread:

- `DefaultRowsOutput: IRowsOutput`. Default output target if INTO clause is not specified.
- `AddRowNumberColumn: bool`. Adds `row_number` column with the current row number.
- `ShowDetailedStatistic: bool`. Fills more informations about query.
- `MaxErrors: int`. Max number of errors before query abort.
- `AnalyzeRowsCount: int`. How many rows to analyze for types detection. 10 by default.
- `DisableCache: bool`. Do not use cache for subqueries. False by default.
- `FollowTimeout: TimeSpan`. Write appended data as source grows. Specifies check timeout. 0 means do not follow.
- `QueryTimeout: TimeSpan`. Throw time out exception if query hasn't been executed within the time.

## More Examples

- [Basic Usage](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/BasicUsage.cs)
- [Custom Functions](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/CustomFunctionUsage.cs)
- [Variables](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/VariablesUsage.cs)
- [Collections](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/CollectionsUsage.cs)
