/**
 * Autogenerated by Thrift Compiler (0.19.0)
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 */

#nullable enable                 // requires C# 8.0
#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE0017  // object init can be simplified
#pragma warning disable IDE0028  // collection init can be simplified
#pragma warning disable IDE1006  // parts of the code use IDL spelling
#pragma warning disable CA1822   // empty DeepCopy() methods still non-static

namespace QueryCat.Plugins.Sdk
{
  public enum DataType
  {
    @NULL = 0,
    INTEGER = 1,
    @STRING = 2,
    @FLOAT = 3,
    TIMESTAMP = 4,
    BOOLEAN = 5,
    NUMERIC = 6,
    INTERVAL = 7,
    @OBJECT = 40,
  }
}
