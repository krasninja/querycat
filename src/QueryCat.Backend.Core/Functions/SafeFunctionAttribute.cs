namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Safe function is the function with no side effects. It means that it only reads a data
/// and does not affect system state (does not write anything).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SafeFunctionAttribute : Attribute;
