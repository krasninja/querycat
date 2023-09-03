/**
 * Autogenerated by Thrift Compiler (0.18.1)
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


#nullable enable                 // requires C# 8.0
#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE0017  // object init can be simplified
#pragma warning disable IDE0028  // collection init can be simplified
#pragma warning disable IDE1006  // parts of the code use IDL spelling
#pragma warning disable CA1822   // empty DeepCopy() methods still non-static

namespace QueryCat.Plugins.Sdk
{

  public partial class RowsList : TBase
  {
    private bool _has_more;

    public bool HasMore
    {
      get
      {
        return _has_more;
      }
      set
      {
        __isset.has_more = true;
        this._has_more = value;
      }
    }

    public List<global::QueryCat.Plugins.Sdk.VariantValue>? Values { get; set; }


    public Isset __isset;
    public struct Isset
    {
      public bool has_more;
    }

    public RowsList()
    {
    }

    public RowsList(List<global::QueryCat.Plugins.Sdk.VariantValue>? values) : this()
    {
      this.Values = values;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_values = false;
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
              if (field.Type == TType.Bool)
              {
                HasMore = await iprot.ReadBoolAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.List)
              {
                {
                  var _list40 = await iprot.ReadListBeginAsync(cancellationToken);
                  Values = new List<global::QueryCat.Plugins.Sdk.VariantValue>(_list40.Count);
                  for(int _i41 = 0; _i41 < _list40.Count; ++_i41)
                  {
                    global::QueryCat.Plugins.Sdk.VariantValue _elem42;
                    _elem42 = new global::QueryCat.Plugins.Sdk.VariantValue();
                    await _elem42.ReadAsync(iprot, cancellationToken);
                    Values.Add(_elem42);
                  }
                  await iprot.ReadListEndAsync(cancellationToken);
                }
                isset_values = true;
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
        if (!isset_values)
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
        var tmp43 = new TStruct("RowsList");
        await oprot.WriteStructBeginAsync(tmp43, cancellationToken);
        var tmp44 = new TField();
        if(__isset.has_more)
        {
          tmp44.Name = "has_more";
          tmp44.Type = TType.Bool;
          tmp44.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp44, cancellationToken);
          await oprot.WriteBoolAsync(HasMore, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Values != null))
        {
          tmp44.Name = "values";
          tmp44.Type = TType.List;
          tmp44.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp44, cancellationToken);
          await oprot.WriteListBeginAsync(new TList(TType.Struct, Values.Count), cancellationToken);
          foreach (global::QueryCat.Plugins.Sdk.VariantValue _iter45 in Values)
          {
            await _iter45.WriteAsync(oprot, cancellationToken);
          }
          await oprot.WriteListEndAsync(cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
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
      if (that is not RowsList other) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.has_more == other.__isset.has_more) && ((!__isset.has_more) || (global::System.Object.Equals(HasMore, other.HasMore))))
        && TCollections.Equals(Values, other.Values);
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.has_more)
        {
          hashcode = (hashcode * 397) + HasMore.GetHashCode();
        }
        if((Values != null))
        {
          hashcode = (hashcode * 397) + TCollections.GetHashCode(Values);
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp46 = new StringBuilder("RowsList(");
      int tmp47 = 0;
      if(__isset.has_more)
      {
        if(0 < tmp47++) { tmp46.Append(", "); }
        tmp46.Append("HasMore: ");
        HasMore.ToString(tmp46);
      }
      if((Values != null))
      {
        if(0 < tmp47) { tmp46.Append(", "); }
        tmp46.Append("Values: ");
        Values.ToString(tmp46);
      }
      tmp46.Append(')');
      return tmp46.ToString();
    }
  }

}
