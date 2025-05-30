using System.Collections.Concurrent;
using System.ComponentModel;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Miscellaneous functions.
/// </summary>
internal static class MiscFunctions
{
    [SafeFunction]
    [Description("The function returns a null value if value1 equals value2; otherwise it returns value1.")]
    [FunctionSignature("\"nullif\"(value1: any, value2: any): any")]
    public static VariantValue NullIf(IExecutionThread thread)
    {
        var value1 = thread.Stack[0];
        var value2 = thread.Stack[1];
        if (VariantValue.Equals(in value1, in value2, out _).AsBoolean)
        {
            return VariantValue.Null;
        }
        return value1;
    }

    [SafeFunction]
    [Description("Not operation. The function can be used to suppress output.")]
    [FunctionSignature("nop(...args: any[]): void")]
    public static VariantValue Nop(IExecutionThread thread)
    {
        return VariantValue.Null;
    }

    [SafeFunction]
    [Description("The function returns a version 4 (random) UUID.")]
    [FunctionSignature("uuid(): string")]
    public static VariantValue GetRandomGuid(IExecutionThread thread)
    {
        return new VariantValue(Guid.NewGuid().ToString("D"));
    }

    [SafeFunction]
    [Description("Converts a size in bytes into a more easily human-readable format with size units.")]
    [FunctionSignature("size_pretty(size: integer, base: integer = 1024): string")]
    public static VariantValue SizePretty(IExecutionThread thread)
    {
        if (thread.Stack[0].IsNull)
        {
            return VariantValue.Null;
        }

        var byteCount = thread.Stack[0].AsInteger;
        var @base = thread.Stack[1].AsInteger;
        if (!byteCount.HasValue || !@base.HasValue)
        {
            return VariantValue.Null;
        }

        // For reference: https://stackoverflow.com/questions/281640/how-do-i-get-a-human-readable-file-size-in-bytes-abbreviation-using-net.
        string[] suffix = ["B", "K", "M", "G", "T", "P", "E"];
        if (byteCount == 0)
        {
            return new VariantValue("0 " + suffix[0]);
        }
        var bytes = Math.Abs(byteCount.Value);
        var place = Convert.ToInt64(Math.Floor(Math.Log(bytes, @base.Value)));
        var num = Math.Round(bytes / Math.Pow(@base.Value, place), 1);
        var size = string.Concat(Math.Sign(byteCount.Value) * num, ' ', suffix[place]);

        return new VariantValue(size);
    }

    [SafeFunction]
    [Description("Returns the object itself. Needed when you need to pass variable as function call.")]
    [FunctionSignature("self(target: any): any")]
    public static VariantValue Self(IExecutionThread thread)
    {
        return thread.Stack.Pop();
    }

    [SafeFunction]
    [Description("Implements rows input caching.")]
    [FunctionSignature("cache_input(input: object<IRowsIterator>, key: string, expire?: interval := null): object<IRowsIterator>")]
    [FunctionSignature("cache_input(input: object<IRowsInput>, key: string, expire?: interval := null): object<IRowsIterator>")]
    public static VariantValue CacheInput(IExecutionThread thread)
    {
        var iterator = thread.Stack[0].AsObjectUnsafe as IRowsIterator;
        if (iterator == null && thread.Stack[0].AsObjectUnsafe is IRowsInput rowsInput)
        {
            iterator = new RowsInputIterator(rowsInput);
        }
        iterator ??= EmptyIterator.Instance;
        var key = thread.Stack[1].AsString;
        var expireTime = thread.Stack[2].AsInterval ?? TimeSpan.FromHours(1);

        var cacheIterator = _cacheStorage.AddOrUpdate(key,
            addValueFactory: (k) => new CacheRowsIterator(iterator, expiresIn: expireTime),
            updateValueFactory: (k, cache) =>
            {
                if (cache.IsExpired || !cache.RowsIterator.IsSchemaEqual(iterator.Columns))
                {
                    return new CacheRowsIterator(iterator, expiresIn: expireTime);
                }
                else
                {
                    cache.SeekCacheCursorToHead();
                    return new CacheRowsIterator(iterator, cache);
                }
            });

        return VariantValue.CreateFromObject(cacheIterator);
    }

    private static readonly ConcurrentDictionary<string, CacheRowsIterator> _cacheStorage = new();

    public static void RegisterFunctions(IFunctionsManager functionsManager)
    {
        functionsManager.RegisterFunction(NullIf);
        functionsManager.RegisterFunction(Nop);
        functionsManager.RegisterFunction(GetRandomGuid);
        functionsManager.RegisterFunction(SizePretty);
        functionsManager.RegisterFunction(Self);
        functionsManager.RegisterFunction(CacheInput);
    }
}
