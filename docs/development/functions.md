# Functions

To extend the QueryCat functionality the functions is the essential thing. The QueryCat compliant functions must match the following delegate signature (`QueryCat.Backend.Functions` namespace):

```csharp
delegate VariantValue FunctionDelegate(FunctionCallInfo args);
```

Here is the sample function definition:

```csharp
[Description("The function sums two integers.")]
[FunctionSignature("add(a: integer, b?: integer = 2): integer")]
public static VariantValue SumIntegers(FunctionCallInfo callInfo)
{
    var a = callInfo.GetAt(0);
    var b = callInfo.GetAt(1);
    return new VariantValue(a.AsInteger + b.AsInteger);
}
```

1. The function name is `add`.
2. The function has two integer arguments (`a` and `b`), it returns `integer` and also has description. If function doesn't return anything the type should be `void`.
3. The second argument (`b`) is optional, and the default value is `2`.
4. The `callInfo` argument is used to get arguments values (use `GetAt` method).
5. The `FunctionSignature` attribute might appear several times for method.

For plugins, the reflection is used to scan all the classes and find all static methods that match the function definition signature.
