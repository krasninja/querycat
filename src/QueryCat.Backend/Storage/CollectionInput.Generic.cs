using System.Diagnostics.CodeAnalysis;

namespace QueryCat.Backend.Storage;

/// <summary>
/// The class that allow to represent enumerable as rows input/output.
/// </summary>
/// <typeparam name="TClass">Enumerable item type.</typeparam>
public class CollectionInput<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
                                | DynamicallyAccessedMemberTypes.PublicProperties)] TClass>
    : CollectionInput where TClass : class, new()
{
    public override IEnumerable<TClass> TargetCollection => (IEnumerable<TClass>)base.TargetCollection;

    public CollectionInput(IEnumerable<TClass> list) : base(typeof(TClass), list)
    {
    }
}
