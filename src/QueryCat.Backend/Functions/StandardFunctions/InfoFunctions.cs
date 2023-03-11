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
        var plugins = pluginsManager.ListAsync().GetAwaiter().GetResult();
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

    [Description("Converts a size in bytes into a more easily human-readable format with size units.")]
    [FunctionSignature("_size_pretty(size: integer, base: integer = 1024): string")]
    public static VariantValue SizePretty(FunctionCallInfo args)
    {
        var byteCount = args.GetAt(0).AsInteger;
        var @base = args.GetAt(1).AsInteger;

        // For reference: https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net.
        string[] suffix = { "B", "K", "M", "G", "T", "P", "E" };
        if (byteCount == 0)
        {
            return new VariantValue("0" + suffix[0]);
        }
        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, @base)));
        var num = Math.Round(bytes / Math.Pow(@base, place), 1);
        var size = Math.Sign(byteCount) * num + suffix[place];

        return new VariantValue(size);
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
        functionsManager.RegisterFunction(SizePretty);
    }
}
