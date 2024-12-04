/**
 * Autogenerated by Thrift Compiler (0.21.0)
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Thrift;
using Thrift.Collections;
using Thrift.Protocol;
using Thrift.Protocol.Entities;
using Thrift.Protocol.Utilities;
using Thrift.Transport;
using Thrift.Transport.Client;
using Thrift.Transport.Server;
using Thrift.Processor;


// targeting net 8
#if( !NET8_0_OR_GREATER)
#error Unexpected target platform. See 'thrift --help' for details.
#endif

// Thrift code generated for net8
#nullable enable                 // requires C# 8.0
#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE0290  // use primary CTOR
#pragma warning disable IDE1006  // parts of the code use IDL spelling
#pragma warning disable CA1822   // empty DeepCopy() methods still non-static

namespace QueryCat.Plugins.Sdk
{

  public partial class ContextQueryInfo : TBase
  {
    private long _limit;

    public List<global::QueryCat.Plugins.Sdk.Column>? Columns { get; set; }

    public long Offset { get; set; } = 0;

    public long Limit
    {
      get
      {
        return _limit;
      }
      set
      {
        __isset.@limit = true;
        this._limit = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool @limit;
    }

    public ContextQueryInfo()
    {
    }

    public ContextQueryInfo(List<global::QueryCat.Plugins.Sdk.Column>? @columns, long @offset) : this()
    {
      this.Columns = @columns;
      this.Offset = @offset;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_columns = false;
        bool isset_offset = false;
        TField field;
        await iprot.ReadStructBeginAsync(cancellationToken);
        while (true)
        {
          field = await iprot.ReadFieldBeginAsync(cancellationToken);
          if (field.Type == TType.Stop)
          {
            break;
          }

          switch (field.ID)
          {
            case 1:
              if (field.Type == TType.List)
              {
                {
                  var _list56 = await iprot.ReadListBeginAsync(cancellationToken);
                  Columns = new List<global::QueryCat.Plugins.Sdk.Column>(_list56.Count);
                  for(int _i57 = 0; _i57 < _list56.Count; ++_i57)
                  {
                    global::QueryCat.Plugins.Sdk.Column _elem58;
                    _elem58 = new global::QueryCat.Plugins.Sdk.Column();
                    await _elem58.ReadAsync(iprot, cancellationToken);
                    Columns.Add(_elem58);
                  }
                  await iprot.ReadListEndAsync(cancellationToken);
                }
                isset_columns = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.I64)
              {
                Offset = await iprot.ReadI64Async(cancellationToken);
                isset_offset = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.I64)
              {
                Limit = await iprot.ReadI64Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            default: 
              await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              break;
          }

          await iprot.ReadFieldEndAsync(cancellationToken);
        }

        await iprot.ReadStructEndAsync(cancellationToken);
        if (!isset_columns)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_offset)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
      }
      finally
      {
        iprot.DecrementRecursionDepth();
      }
    }

    public async global::System.Threading.Tasks.Task WriteAsync(TProtocol oprot, CancellationToken cancellationToken)
    {
      oprot.IncrementRecursionDepth();
      try
      {
        var tmp59 = new TStruct("ContextQueryInfo");
        await oprot.WriteStructBeginAsync(tmp59, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp60 = new TField();
        if((Columns != null))
        {
          tmp60.Name = "columns";
          tmp60.Type = TType.List;
          tmp60.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp60, cancellationToken);
          await oprot.WriteListBeginAsync(new TList(TType.Struct, Columns.Count), cancellationToken);
          foreach (global::QueryCat.Plugins.Sdk.Column _iter61 in Columns)
          {
            await _iter61.WriteAsync(oprot, cancellationToken);
          }
          await oprot.WriteListEndAsync(cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        tmp60.Name = "offset";
        tmp60.Type = TType.I64;
        tmp60.ID = 2;
        await oprot.WriteFieldBeginAsync(tmp60, cancellationToken);
        await oprot.WriteI64Async(Offset, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if(__isset.@limit)
        {
          tmp60.Name = "limit";
          tmp60.Type = TType.I64;
          tmp60.ID = 3;
          await oprot.WriteFieldBeginAsync(tmp60, cancellationToken);
          await oprot.WriteI64Async(Limit, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        #pragma warning restore IDE0017  // simplified init
        await oprot.WriteFieldStopAsync(cancellationToken);
        await oprot.WriteStructEndAsync(cancellationToken);
      }
      finally
      {
        oprot.DecrementRecursionDepth();
      }
    }

    public override bool Equals(object? that)
    {
      if (that is not ContextQueryInfo other) return false;
      if (ReferenceEquals(this, other)) return true;
      return TCollections.Equals(Columns, other.Columns)
        && global::System.Object.Equals(Offset, other.Offset)
        && ((__isset.@limit == other.__isset.@limit) && ((!__isset.@limit) || (global::System.Object.Equals(Limit, other.Limit))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if((Columns != null))
        {
          hashcode = (hashcode * 397) + TCollections.GetHashCode(Columns);
        }
        hashcode = (hashcode * 397) + Offset.GetHashCode();
        if(__isset.@limit)
        {
          hashcode = (hashcode * 397) + Limit.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp62 = new StringBuilder("ContextQueryInfo(");
      if((Columns != null))
      {
        tmp62.Append(", Columns: ");
        Columns.ToString(tmp62);
      }
      tmp62.Append(", Offset: ");
      Offset.ToString(tmp62);
      if(__isset.@limit)
      {
        tmp62.Append(", Limit: ");
        Limit.ToString(tmp62);
      }
      tmp62.Append(')');
      return tmp62.ToString();
    }
  }

}
