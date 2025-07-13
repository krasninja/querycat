namespace QueryCat.Backend;

/// <summary>
/// The interface adds arguments to functions.
/// </summary>
internal interface IFuncUnitArguments : IFuncUnit
{
    /// <summary>
    /// Functions to get argument values.
    /// </summary>
    IFuncUnit[] ArgumentsUnits { get; }
}
