using QueryCat.Backend.Types;

namespace QueryCat.Backend.Abstractions.Functions;

/// <summary>
/// QueryCat function signature.
/// </summary>
public delegate VariantValue FunctionDelegate(FunctionCallInfo args);
