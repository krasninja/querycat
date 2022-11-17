using System.ComponentModel;
using System.Reflection;
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
        var functions = args.ExecutionThread?.FunctionsManager.GetFunctions().OrderBy(f => f.Name)
            ?? Array.Empty<Function>().AsEnumerable();
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

    [Description("Return available plugins from repository.")]
    [FunctionSignature("_plugins(): object<IRowsIterator>")]
    public static VariantValue Plugins(FunctionCallInfo args)
    {
        var builder = new ClassRowsFrameBuilder<PluginInfo>()
            .AddProperty("name", p => p.Name)
            .AddProperty("version", p => p.Version.ToString())
            .AddProperty("uri", p => p.Uri);
        using var pluginsManager = new PluginsManager(args.ExecutionThread.PluginsManager.PluginDirectories);
        var plugins = pluginsManager.ListAsync(CancellationToken.None).GetAwaiter().GetResult();
        return VariantValue.CreateFromObject(builder.BuildIterator(plugins));
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

    public static void RegisterFunctions(FunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(Functions);
        functionsManager.RegisterFunction(Schema);
        functionsManager.RegisterFunction(Plugins);
        functionsManager.RegisterFunction(Version);
    }
}
