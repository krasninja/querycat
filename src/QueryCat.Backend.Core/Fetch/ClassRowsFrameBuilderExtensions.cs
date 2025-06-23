using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Fetch;

/// <summary>
/// Extensions for <see cref="ClassRowsFrameBuilder{TClass}" />.
/// </summary>
public static class ClassRowsFrameBuilderExtensions
{
    /// <summary>
    /// Add all public properties as columns.
    /// </summary>
    /// <returns>The instance of <see cref="ClassRowsFrameBuilder{TClass}" />.</returns>
    public static ClassRowsFrameBuilder<TClass> AddPublicProperties
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TClass>(
        this ClassRowsFrameBuilder<TClass> builder) where TClass : class
    {
        AddPublicProperties(builder, null, out _);
        return builder;
    }

    internal static void AddPublicProperties
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TClass>(
        this ClassRowsFrameBuilder<TClass> builder,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type? type,
        out List<PropertyInfo> properties) where TClass : class
    {
        var props = (type ?? typeof(TClass)).GetProperties().Where(p => p.CanRead).ToArray();
        properties = new List<PropertyInfo>(capacity: props.Length);
        foreach (var propertyInfo in props)
        {
            var descriptionAttribute = propertyInfo.GetCustomAttribute(typeof(DescriptionAttribute), inherit: false)
                as DescriptionAttribute;
            var description = descriptionAttribute != null ? descriptionAttribute.Description : string.Empty;
            var dataType = Converter.ConvertFromSystem(propertyInfo.PropertyType);
            var columnAttribute = propertyInfo.GetCustomAttribute(typeof(ColumnAttribute), inherit: false)
                as ColumnAttribute;
            var name = columnAttribute != null ? columnAttribute.Name : propertyInfo.Name;
            builder.AddProperty(
                name ?? propertyInfo.Name,
                dataType,
                obj => VariantValue.CreateFromObject(propertyInfo.GetValue(obj)), description);
            properties.Add(propertyInfo);
        }
    }
}
