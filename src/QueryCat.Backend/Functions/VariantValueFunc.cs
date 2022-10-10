using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// The delegate that is used to execute specific parts of query. For example,
/// SELECT filter, grouping, etc. It is specifically generated for every AST node.
/// </summary>
public delegate VariantValue VariantValueFunc(VariantValueFuncData data);
