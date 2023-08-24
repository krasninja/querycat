using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// QueryCat function signature.
/// </summary>
public delegate VariantValue FunctionDelegate(FunctionCallInfo args);
