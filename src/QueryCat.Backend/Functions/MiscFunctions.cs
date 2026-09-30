using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using QueryCat.Backend.Core;
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
    [FunctionSignature("\"nullif\"(value1: integer, value2: integer): integer")]
    [FunctionSignature("\"nullif\"(value1: string, value2: string): string")]
    [FunctionSignature("\"nullif\"(value1: float, value2: float): float")]
    [FunctionSignature("\"nullif\"(value1: timestamp, value2: timestamp): timestamp")]
    [FunctionSignature("\"nullif\"(value1: boolean, value2: boolean): boolean")]
    [FunctionSignature("\"nullif\"(value1: numeric, value2: numeric): numeric")]
    [FunctionSignature("\"nullif\"(value1: interval, value2: interval): interval")]
    [FunctionSignature("\"nullif\"(value1: blob, value2: blob): blob")]
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

    private static readonly string[] _sizeSuffixes = ["B", "K", "M", "G", "T", "P", "E"];

    [SafeFunction]
    [Description("Converts a size in bytes into a more easily human-readable format with size units.")]
    [FunctionSignature("size_pretty(size: integer, base: integer = 1024): string")]
    public static VariantValue SizePretty(IExecutionThread thread)
    {
        var byteCount = thread.Stack[0].AsInteger;
        var @base = thread.Stack[1].AsInteger;
        if (!byteCount.HasValue || !@base.HasValue)
        {
            return VariantValue.Null;
        }
        if (@base.Value < 2)
        {
            throw new QueryCatException(Resources.Errors.InvalidSizeBase);
        }

        // Convert before Abs so long.MinValue cannot overflow.
        var size = Math.Abs((double)byteCount.Value);
        var place = 0;
        while (size >= @base.Value && place < _sizeSuffixes.Length - 1)
        {
            size /= @base.Value;
            place++;
        }
        size = Math.Round(size, 1);
        // Rounding may reach the next unit (1023.96 K -> 1024.0 K -> 1 M).
        if (size >= @base.Value && place < _sizeSuffixes.Length - 1)
        {
            size /= @base.Value;
            place++;
        }

        var sign = byteCount.Value < 0 ? "-" : string.Empty;
        return new VariantValue(
            string.Concat(sign, size.ToString(CultureInfo.InvariantCulture), ' ', _sizeSuffixes[place]));
    }

    [SafeFunction]
    [Description("Returns the object itself. Needed when you need to pass variable as function call.")]
    [FunctionSignature("self(target: any): any")]
    public static VariantValue Self(IExecutionThread thread)
    {
        return thread.Stack.Pop();
    }

    [SafeFunction]
    [Description("Caches rows of the input by key. The cache expires after 'expire' (1 hour by default, interval '0' disables expiration).")]
    [FunctionSignature("cache_input(input: object<IRowsIterator>, key: string, expire?: interval := null): object<IRowsIterator>")]
    [FunctionSignature("cache_input(input: object<IRowsInput>, key: string, expire?: interval := null): object<IRowsIterator>")]
    public static VariantValue CacheInput(IExecutionThread thread)
    {
        var iterator = thread.Stack[0].AsObjectUnsafe switch
        {
            IRowsIterator rowsIterator => rowsIterator,
            IRowsInput ri => new RowsInputIterator(ri),
            _ => throw new QueryCatException(Resources.Errors.InvalidRowsInput),
        };
        var key = thread.Stack[1].AsString;
        if (string.IsNullOrEmpty(key))
        {
            throw new QueryCatException(string.Format(Resources.Errors.InvalidField, nameof(key)));
        }
        var expireTime = thread.Stack[2].AsInterval ?? TimeSpan.FromHours(1);

        var cacheStorage = GetCacheStorage(thread);
        var cacheIterator = cacheStorage.AddOrUpdate(key,
            addValueFactory: (k) => new CacheRowsIterator(iterator, expiresIn: expireTime),
            updateValueFactory: (k, cache) =>
            {
                if (cache.IsExpired || !cache.RowsIterator.IsSchemaEqual(iterator.Columns))
                {
                    return new CacheRowsIterator(iterator, expiresIn: expireTime);
                }
                else
                {
                    return new CacheRowsIterator(iterator, cache);
                }
            });

        return VariantValue.CreateFromObject(cacheIterator);
    }

    /// <summary>
    /// Store cache per threads.
    /// </summary>
    private static readonly ConditionalWeakTable<IExecutionThread, ConcurrentDictionary<string, CacheRowsIterator>> _cacheStorage = new();

    private static ConcurrentDictionary<string, CacheRowsIterator> GetCacheStorage(IExecutionThread thread)
    {
        var storage = _cacheStorage.GetValue(thread, static _ => new ConcurrentDictionary<string, CacheRowsIterator>());
        foreach (var item in storage)
        {
            if (item.Value.IsExpired)
            {
                storage.TryRemove(item);
            }
        }
        return storage;
    }

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
