/**
 * Autogenerated by Thrift Compiler (0.19.0)
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

  public partial class VariantValue : TBase
  {
    private bool _isNull;
    private long _integer;
    private string? _string;
    private double _float;
    private long _timestamp;
    private bool _boolean;
    private global::QueryCat.Plugins.Sdk.DecimalValue? _decimal;
    private long _interval;
    private global::QueryCat.Plugins.Sdk.ObjectValue? _object;
    private string? _json;

    public bool IsNull
    {
      get
      {
        return _isNull;
      }
      set
      {
        __isset.isNull = true;
        this._isNull = value;
      }
    }

    public long Integer
    {
      get
      {
        return _integer;
      }
      set
      {
        __isset.@integer = true;
        this._integer = value;
      }
    }

    public string? String
    {
      get
      {
        return _string;
      }
      set
      {
        __isset.@string = true;
        this._string = value;
      }
    }

    public double Float
    {
      get
      {
        return _float;
      }
      set
      {
        __isset.@float = true;
        this._float = value;
      }
    }

    public long Timestamp
    {
      get
      {
        return _timestamp;
      }
      set
      {
        __isset.@timestamp = true;
        this._timestamp = value;
      }
    }

    public bool Boolean
    {
      get
      {
        return _boolean;
      }
      set
      {
        __isset.@boolean = true;
        this._boolean = value;
      }
    }

    public global::QueryCat.Plugins.Sdk.DecimalValue? Decimal
    {
      get
      {
        return _decimal;
      }
      set
      {
        __isset.@decimal = true;
        this._decimal = value;
      }
    }

    public long Interval
    {
      get
      {
        return _interval;
      }
      set
      {
        __isset.@interval = true;
        this._interval = value;
      }
    }

    public global::QueryCat.Plugins.Sdk.ObjectValue? Object
    {
      get
      {
        return _object;
      }
      set
      {
        __isset.@object = true;
        this._object = value;
      }
    }

    public string? Json
    {
      get
      {
        return _json;
      }
      set
      {
        __isset.@json = true;
        this._json = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool isNull;
      public bool @integer;
      public bool @string;
      public bool @float;
      public bool @timestamp;
      public bool @boolean;
      public bool @decimal;
      public bool @interval;
      public bool @object;
      public bool @json;
    }

    public VariantValue()
    {
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
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
                IsNull = await iprot.ReadBoolAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.I64)
              {
                Integer = await iprot.ReadI64Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.String)
              {
                String = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 4:
              if (field.Type == TType.Double)
              {
                Float = await iprot.ReadDoubleAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 5:
              if (field.Type == TType.I64)
              {
                Timestamp = await iprot.ReadI64Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 6:
              if (field.Type == TType.Bool)
              {
                Boolean = await iprot.ReadBoolAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 7:
              if (field.Type == TType.Struct)
              {
                Decimal = new global::QueryCat.Plugins.Sdk.DecimalValue();
                await Decimal.ReadAsync(iprot, cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 8:
              if (field.Type == TType.I64)
              {
                Interval = await iprot.ReadI64Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 9:
              if (field.Type == TType.Struct)
              {
                Object = new global::QueryCat.Plugins.Sdk.ObjectValue();
                await Object.ReadAsync(iprot, cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 10:
              if (field.Type == TType.String)
              {
                Json = await iprot.ReadStringAsync(cancellationToken);
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
        var tmp8 = new TStruct("VariantValue");
        await oprot.WriteStructBeginAsync(tmp8, cancellationToken);
        var tmp9 = new TField();
        if(__isset.isNull)
        {
          tmp9.Name = "isNull";
          tmp9.Type = TType.Bool;
          tmp9.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteBoolAsync(IsNull, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@integer)
        {
          tmp9.Name = "integer";
          tmp9.Type = TType.I64;
          tmp9.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteI64Async(Integer, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((String != null) && __isset.@string)
        {
          tmp9.Name = "string";
          tmp9.Type = TType.String;
          tmp9.ID = 3;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteStringAsync(String, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@float)
        {
          tmp9.Name = "float";
          tmp9.Type = TType.Double;
          tmp9.ID = 4;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteDoubleAsync(Float, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@timestamp)
        {
          tmp9.Name = "timestamp";
          tmp9.Type = TType.I64;
          tmp9.ID = 5;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteI64Async(Timestamp, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@boolean)
        {
          tmp9.Name = "boolean";
          tmp9.Type = TType.Bool;
          tmp9.ID = 6;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteBoolAsync(Boolean, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Decimal != null) && __isset.@decimal)
        {
          tmp9.Name = "decimal";
          tmp9.Type = TType.Struct;
          tmp9.ID = 7;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await Decimal.WriteAsync(oprot, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@interval)
        {
          tmp9.Name = "interval";
          tmp9.Type = TType.I64;
          tmp9.ID = 8;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteI64Async(Interval, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Object != null) && __isset.@object)
        {
          tmp9.Name = "object";
          tmp9.Type = TType.Struct;
          tmp9.ID = 9;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await Object.WriteAsync(oprot, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Json != null) && __isset.@json)
        {
          tmp9.Name = "json";
          tmp9.Type = TType.String;
          tmp9.ID = 10;
          await oprot.WriteFieldBeginAsync(tmp9, cancellationToken);
          await oprot.WriteStringAsync(Json, cancellationToken);
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
      if (that is not VariantValue other) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.isNull == other.__isset.isNull) && ((!__isset.isNull) || (global::System.Object.Equals(IsNull, other.IsNull))))
        && ((__isset.@integer == other.__isset.@integer) && ((!__isset.@integer) || (global::System.Object.Equals(Integer, other.Integer))))
        && ((__isset.@string == other.__isset.@string) && ((!__isset.@string) || (global::System.Object.Equals(String, other.String))))
        && ((__isset.@float == other.__isset.@float) && ((!__isset.@float) || (global::System.Object.Equals(Float, other.Float))))
        && ((__isset.@timestamp == other.__isset.@timestamp) && ((!__isset.@timestamp) || (global::System.Object.Equals(Timestamp, other.Timestamp))))
        && ((__isset.@boolean == other.__isset.@boolean) && ((!__isset.@boolean) || (global::System.Object.Equals(Boolean, other.Boolean))))
        && ((__isset.@decimal == other.__isset.@decimal) && ((!__isset.@decimal) || (global::System.Object.Equals(Decimal, other.Decimal))))
        && ((__isset.@interval == other.__isset.@interval) && ((!__isset.@interval) || (global::System.Object.Equals(Interval, other.Interval))))
        && ((__isset.@object == other.__isset.@object) && ((!__isset.@object) || (global::System.Object.Equals(Object, other.Object))))
        && ((__isset.@json == other.__isset.@json) && ((!__isset.@json) || (global::System.Object.Equals(Json, other.Json))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.isNull)
        {
          hashcode = (hashcode * 397) + IsNull.GetHashCode();
        }
        if(__isset.@integer)
        {
          hashcode = (hashcode * 397) + Integer.GetHashCode();
        }
        if((String != null) && __isset.@string)
        {
          hashcode = (hashcode * 397) + String.GetHashCode();
        }
        if(__isset.@float)
        {
          hashcode = (hashcode * 397) + Float.GetHashCode();
        }
        if(__isset.@timestamp)
        {
          hashcode = (hashcode * 397) + Timestamp.GetHashCode();
        }
        if(__isset.@boolean)
        {
          hashcode = (hashcode * 397) + Boolean.GetHashCode();
        }
        if((Decimal != null) && __isset.@decimal)
        {
          hashcode = (hashcode * 397) + Decimal.GetHashCode();
        }
        if(__isset.@interval)
        {
          hashcode = (hashcode * 397) + Interval.GetHashCode();
        }
        if((Object != null) && __isset.@object)
        {
          hashcode = (hashcode * 397) + Object.GetHashCode();
        }
        if((Json != null) && __isset.@json)
        {
          hashcode = (hashcode * 397) + Json.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp10 = new StringBuilder("VariantValue(");
      int tmp11 = 0;
      if(__isset.isNull)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("IsNull: ");
        IsNull.ToString(tmp10);
      }
      if(__isset.@integer)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Integer: ");
        Integer.ToString(tmp10);
      }
      if((String != null) && __isset.@string)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("String: ");
        String.ToString(tmp10);
      }
      if(__isset.@float)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Float: ");
        Float.ToString(tmp10);
      }
      if(__isset.@timestamp)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Timestamp: ");
        Timestamp.ToString(tmp10);
      }
      if(__isset.@boolean)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Boolean: ");
        Boolean.ToString(tmp10);
      }
      if((Decimal != null) && __isset.@decimal)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Decimal: ");
        Decimal.ToString(tmp10);
      }
      if(__isset.@interval)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Interval: ");
        Interval.ToString(tmp10);
      }
      if((Object != null) && __isset.@object)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Object: ");
        Object.ToString(tmp10);
      }
      if((Json != null) && __isset.@json)
      {
        if(0 < tmp11++) { tmp10.Append(", "); }
        tmp10.Append("Json: ");
        Json.ToString(tmp10);
      }
      tmp10.Append(')');
      return tmp10.ToString();
    }
  }

}
