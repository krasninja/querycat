# Functions

To extend the QueryCat functionality the functions is the essential thing. The QueryCat compliant functions must match the following delegate signatures (`QueryCat.Backend.Core.Functions` namespace):

```csharp
using FunctionDelegate = Func<IExecutionThread, VariantValue>;
using FunctionDelegateAsync = Func<IExecutionThread, CancellationToken, ValueTask<VariantValue>>;
```

Here is the sample function definition:

```csharp
[Description("The function sums two integers.")]
[FunctionSignature("add(a: integer, b?: integer = 2): integer")]
public static VariantValue SumIntegers(IExecutionThread thread)
{
    var a = thread.Stack[0];
    var b = thread.Stack[1];
    return new VariantValue(a.AsInteger + b.AsInteger);
}
```

1. The function name is `add`.
2. The function has two integer arguments (`a` and `b`), it returns `integer` and also has description. If function doesn't return anything the type should be `void`.
3. The second argument (`b`) is optional, and the default value is `2`.
4. The `callInfo` argument is used to get arguments values (use `GetAt` method).
5. The `FunctionSignature` attribute might appear several times for method.

Async function definition:

```csharp
[Description("The function sums two integers.")]
[FunctionSignature("add(a: integer, b: integer): integer")]
public static async ValueTask<VariantValue> CallServiceAsync(IExecutionThread thread, CancellationToken cancellationToken)
{
    var a = thread.Stack[0].AsInteger;
    var b = thread.Stack[1].AsInteger;
    var result = await remoteService.CallAsync(a, b, cancellationToken);
    return new VariantValue(result);
}
```

## Safe Functions

You can assign `SafeFunction` attribute to your function delegate. This way you tell QueryCat that function only selects data and doesn't do any updates or removes. If QueryCat is running in safe mode it can only use safe functions.
