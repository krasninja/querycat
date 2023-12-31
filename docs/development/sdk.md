# SDK

You can use QueryCat directly in your .NET app. Install the following NuGet package:

```
Install-Package QueryCat
```

## Basic Usage

The main object is `ExecutionThread` that allows to execute queries. Basic usage:

```csharp
var executionThread = new ExecutionThread();
var result = executionThread.Run("1+1");
```

Result is the `VariantValue` returned by the last statement in the script.

To be able to use standard functions use `ExecutionThreadBootstrapper` bootstraper. Example:

```csharp
new ExecutionThreadBootstrapper().Bootstrap(executionThread);
var result = executionThread.Run('uuid()'); // 63af7231-f182-4d29-816c-b83b9dc9cff5
```

## Execution Thread Options

`ExecutionOptions` class that allows to customize execution thread:

- `DefaultRowsOutput: IRowsOutput`. Default output target if INTO clause is not specified.
- `AddRowNumberColumn: bool`. Adds `row_number` column with the current row number.
- `ShowDetailedStatistic: bool`. Fills more informations about query.
- `MaxErrors: int`. Max number of errors before query abort.
- `AnalyzeRowsCount: int`. How many rows to analyze for types detection. 10 by default.
- `DisableCache: bool`. Do not use cache for subqueries. False by default.

## More Examples

- [Basic Usage](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/BasicUsage.cs)
- [Custom Functions](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/CustomFunctionUsage.cs)
- [Variables](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/VariablesUsage.cs)
- [Collections](https://github.com/krasninja/querycat/blob/develop/src/QueryCat.Samples/Collection/CollectionsUsage.cs)
