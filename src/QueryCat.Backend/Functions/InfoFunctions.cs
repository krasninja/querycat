using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Plugins;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Information functions.
/// </summary>
public static class InfoFunctions
{
    [Description("Return all registered functions.")]
    [FunctionSignature("_functions(): object<IRowsIterator>")]
    internal static VariantValue Functions(FunctionCallInfo args)
    {
        var functions = args.ExecutionThread.FunctionsManager.GetFunctions().OrderBy(f => f.Name);
        var input = new EnumerableRowsInput<IFunction>(functions,
            builder => builder
                .AddProperty("signature", p => p.ToString())
                .AddProperty("description", f => f.Description)
            );
        return VariantValue.CreateFromObject(input);
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

        var inputResult = new EnumerableRowsInput<Column>(columns,
            builder => builder
#if DEBUG
                .AddProperty("id", f => f.Id)
#endif
                .AddProperty("name", f => f.Name, defaultLength: 25)
                .AddProperty("type", f => f.DataType)
#if DEBUG
                .AddProperty("length", f => f.Length)
#endif
                .AddProperty("description", f => f.Description)
            );
        return VariantValue.CreateFromObject(inputResult);
    }

#if ENABLE_PLUGINS
    [Description("Return available plugins from repository.")]
    [FunctionSignature("_plugins(): object<IRowsInput>")]
    public static VariantValue Plugins(FunctionCallInfo args)
    {
        var plugins = AsyncUtils.RunSync(async ct =>
        {
            return await args.ExecutionThread.PluginsManager.ListAsync(cancellationToken: ct);
        });
        var input = new EnumerableRowsInput<PluginInfo>(plugins!,
            builder => builder
                .AddProperty("name", p => p.Name)
                .AddProperty("version", p => p.Version.ToString())
                .AddProperty("is_installed", p => p.IsInstalled)
                .AddProperty("platform", p => p.Platform)
                .AddProperty("arch", p => p.Architecture)
                .AddProperty("uri", p => p.Uri)
        );
        return VariantValue.CreateFromObject(input);
    }
#endif

    [Description("Get expression type.")]
    [FunctionSignature("_typeof(arg: any): string")]
    internal static VariantValue TypeOf(FunctionCallInfo args)
    {
        var value = args.GetAt(0);
        var type = value.GetInternalType();
        if (type == DataType.Object && value.AsObject != null)
        {
            return new VariantValue($"object<{value.AsObject.GetType().Name}>");
        }
        return new VariantValue(type.ToString());
    }

    [Description("Application version.")]
    [FunctionSignature("_version(): string")]
    public static VariantValue Version(FunctionCallInfo args)
    {
        return new VariantValue(Application.GetVersion());
    }

    [Description("Provide a list of OS time zone names.")]
    [FunctionSignature("_timezone_names(): object<IRowsIterator>")]
    internal static VariantValue TimeZoneNames(FunctionCallInfo args)
    {
        var input = new EnumerableRowsInput<TimeZoneInfo>(TimeZoneInfo.GetSystemTimeZones(),
            builder => builder
                .AddProperty("id", p => p.Id, "Time zone code.")
                .AddProperty("name", p => p.StandardName, "Time zone name.")
                .AddProperty("utc_offset", p => p.BaseUtcOffset, "Offset from UTC (positive means east of Greenwich).")
                .AddProperty("is_dst", p => p.SupportsDaylightSavingTime, "True if currently observing daylight savings")
            );
        return VariantValue.CreateFromObject(input);
    }

    [Description("Get current running platform/OS.")]
    [FunctionSignature("_platform(): string")]
    internal static VariantValue Platform(FunctionCallInfo args)
    {
        return new VariantValue(Application.GetPlatform());
    }

    internal static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Functions);
        functionsManager.RegisterFunction(Schema);
#if ENABLE_PLUGINS
        functionsManager.RegisterFunction(Plugins);
#endif
        functionsManager.RegisterFunction(TypeOf);
        functionsManager.RegisterFunction(Version);
        functionsManager.RegisterFunction(TimeZoneNames);
        functionsManager.RegisterFunction(Platform);
    }
}
