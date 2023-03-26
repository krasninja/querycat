using System.ComponentModel;
using System.Reflection;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution.Plugins;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions.StandardFunctions;

/// <summary>
/// Information functions.
/// </summary>
public static class InfoFunctions
{
    [Description("Return all registered functions.")]
    [FunctionSignature("_functions(): object<IRowsIterator>")]
    public static VariantValue Functions(FunctionCallInfo args)
    {
        var builder = new ClassRowsFrameBuilder<Function>()
            .AddProperty("signature", f => f.ToString())
            .AddProperty("description", f => f.Description);
        var functions = args.ExecutionThread.FunctionsManager.GetFunctions().OrderBy(f => f.Name);
        return VariantValue.CreateFromObject(builder.BuildIterator(functions));
    }

    [Description("Return row input columns information.")]
    [FunctionSignature("_schema(input: object<IRowsInput>): object<IRowsIterator>")]
    public static VariantValue Schema(FunctionCallInfo args)
    {
        var obj = args.GetAt(0).AsObject;
        Column[] columns;

        if (obj is IRowsIterator iterator)
        {
            columns = iterator.Columns;
        }
        else if (obj is IRowsInput input)
        {
            input.Open();
            columns = input.Columns;
        }
        else
        {
            throw new QueryCatException("Invalid rows input type.");
        }

        var builder = new ClassRowsFrameBuilder<Column>()
#if DEBUG
            .AddProperty("id", f => f.Id)
#endif
            .AddProperty("name", f => f.Name, defaultLength: 25)
            .AddProperty("type", f => f.DataType)
#if DEBUG
            .AddProperty("length", f => f.Length)
#endif
            .AddProperty("description", f => f.Description);

        return VariantValue.CreateFromObject(builder.BuildIterator(columns));
    }

#if ENABLE_PLUGINS
    [Description("Return available plugins from repository.")]
    [FunctionSignature("_plugins(): object<IRowsIterator>")]
    public static VariantValue Plugins(FunctionCallInfo args)
    {
        var builder = new ClassRowsFrameBuilder<PluginInfo>()
            .AddProperty("name", p => p.Name)
            .AddProperty("version", p => p.Version.ToString())
            .AddProperty("is_installed", p => p.IsInstalled)
            .AddProperty("uri", p => p.Uri);
        using var pluginsManager = new PluginsManager(args.ExecutionThread.PluginsManager.PluginDirectories);
        var plugins = pluginsManager.ListAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        return VariantValue.CreateFromObject(builder.BuildIterator(plugins));
    }
#endif

    [Description("Get expression type.")]
    [FunctionSignature("_typeof(arg: any): string")]
    public static VariantValue TypeOf(FunctionCallInfo args)
    {
        var value = args.GetAt(0);
        var type = value.GetInternalType();
        if (type == DataType.Object && value.AsObject != null)
        {
            return new VariantValue($"object<{value.AsObject.GetType().Name}>");
        }
        return new VariantValue(type.ToString());
    }

    public static string GetVersion()
        => typeof(VariantValue).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;

    [Description("Application version.")]
    [FunctionSignature("_version(): string")]
    public static VariantValue Version(FunctionCallInfo args)
    {
        return new VariantValue(GetVersion());
    }

    [Description("Provide a list of OS time zone names.")]
    [FunctionSignature("_timezone_names(): object<IRowsIterator>")]
    public static VariantValue TimeZoneNames(FunctionCallInfo args)
    {
        var builder = new ClassRowsFrameBuilder<TimeZoneInfo>()
            .AddProperty("id", p => p.Id, "Time zone code.")
            .AddProperty("name", p => p.StandardName, "Time zone name.")
            .AddProperty("utc_offset", p => p.BaseUtcOffset, "Offset from UTC (positive means east of Greenwich).")
            .AddProperty("is_dst", p => p.SupportsDaylightSavingTime, "True if currently observing daylight savings");
        return VariantValue.CreateFromObject(builder.BuildIterator(TimeZoneInfo.GetSystemTimeZones()));
    }

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Functions);
        functionsManager.RegisterFunction(Schema);
#if ENABLE_PLUGINS
        functionsManager.RegisterFunction(Plugins);
#endif
        functionsManager.RegisterFunction(TypeOf);
        functionsManager.RegisterFunction(Version);
        functionsManager.RegisterFunction(TimeZoneNames);
    }
}
