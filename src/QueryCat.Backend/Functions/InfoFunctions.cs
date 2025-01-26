using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
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
    [SafeFunction]
    [Description("Return all registered functions.")]
    [FunctionSignature("_functions(): object<IRowsIterator>")]
    internal static VariantValue Functions(IExecutionThread thread)
    {
        var functions = thread.FunctionsManager.GetFunctions().OrderBy(f => f.Name);
        var input = EnumerableRowsInput<IFunction>.FromSource(functions,
            builder => builder
                .AddProperty("signature", FunctionFormatter.GetSignature)
                .AddProperty("is_aggregate", p => p.IsAggregate)
                .AddProperty("description", p => p.Description)
                .AddProperty("is_safe", p => p.IsSafe)
            );
        return VariantValue.CreateFromObject(input);
    }

    [SafeFunction]
    [Description("Return row input columns information.")]
    [FunctionSignature("_schema(input: object<IRowsInput>): object<IRowsIterator>")]
    public static VariantValue Schema(IExecutionThread thread)
    {
        var obj = thread.Stack[0].AsObject;
        Column[] columns;

        if (obj is IRowsIterator iterator)
        {
            columns = iterator.Columns;
        }
        else if (obj is IRowsInput input)
        {
            AsyncUtils.RunSync(input.OpenAsync);
            columns = input.Columns;
        }
        else
        {
            throw new QueryCatException(Resources.Errors.InvalidRowsInput);
        }

        var inputResult = EnumerableRowsInput<Column>.FromSource(columns,
            builder => builder
#if DEBUG
                .AddProperty("id", f => f.Id)
#endif
                .AddProperty("name", f => f.FullName, defaultLength: 45)
                .AddProperty("type", f => f.DataType)
#if DEBUG
                .AddProperty("length", f => f.Length)
#endif
                .AddProperty("description", f => f.Description)
            );
        return VariantValue.CreateFromObject(inputResult);
    }

#if ENABLE_PLUGINS
    [SafeFunction]
    [Description("Return available plugins from repository.")]
    [FunctionSignature("_plugins(): object<IRowsInput>")]
    public static VariantValue Plugins(IExecutionThread thread)
    {
        var plugins = AsyncUtils.RunSync(ct => thread.PluginsManager.ListAsync(cancellationToken: ct));
        var input = EnumerableRowsInput<PluginInfo>.FromSource(plugins!,
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

    [SafeFunction]
    [Description("Get expression type.")]
    [FunctionSignature("_typeof(arg: any): string")]
    internal static VariantValue TypeOf(IExecutionThread thread)
    {
        var value = thread.Stack.Pop();
        var type = value.Type;
        if (type == DataType.Object && value.AsObject != null)
        {
            return new VariantValue($"object<{value.AsObject.GetType().Name}>");
        }
        return new VariantValue(type.ToString());
    }

    [SafeFunction]
    [Description("Application version.")]
    [FunctionSignature("_version(): string")]
    public static VariantValue Version(IExecutionThread thread)
    {
        return new VariantValue(Application.GetVersion());
    }

    [SafeFunction]
    [Description("Provide a list of OS time zone names.")]
    [FunctionSignature("_timezone_names(): object<IRowsIterator>")]
    internal static VariantValue TimeZoneNames(IExecutionThread thread)
    {
        var input = EnumerableRowsInput<TimeZoneInfo>.FromSource(TimeZoneInfo.GetSystemTimeZones(),
            builder => builder
                .AddProperty("id", p => p.Id, "Time zone code.")
                .AddProperty("name", p => p.StandardName, "Time zone name.")
                .AddProperty("utc_offset", p => p.BaseUtcOffset, "Offset from UTC (positive means east of Greenwich).")
                .AddProperty("is_dst", p => p.SupportsDaylightSavingTime, "True if currently observing daylight savings")
            );
        return VariantValue.CreateFromObject(input);
    }

    [SafeFunction]
    [Description("Get current running platform/OS.")]
    [FunctionSignature("_platform(): string")]
    internal static VariantValue Platform(IExecutionThread thread)
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
