/**
 * Autogenerated by Thrift Compiler (0.20.0)
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


// Thrift code generated for net8
#nullable enable                 // requires C# 8.0
#pragma warning disable IDE0079  // remove unnecessary pragmas
#pragma warning disable IDE0290  // use primary CTOR
#pragma warning disable IDE1006  // parts of the code use IDL spelling
#pragma warning disable CA1822   // empty DeepCopy() methods still non-static

namespace QueryCat.Plugins.Sdk
{

  public partial class DecimalValue : TBase
  {
    private long _units;
    private int _nanos;

    public long Units
    {
      get
      {
        return _units;
      }
      set
      {
        __isset.@units = true;
        this._units = value;
      }
    }

    public int Nanos
    {
      get
      {
        return _nanos;
      }
      set
      {
        __isset.@nanos = true;
        this._nanos = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool @units;
      public bool @nanos;
    }

    public DecimalValue()
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
              if (field.Type == TType.I64)
              {
                Units = await iprot.ReadI64Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.I32)
              {
                Nanos = await iprot.ReadI32Async(cancellationToken);
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
        var tmp0 = new TStruct("DecimalValue");
        await oprot.WriteStructBeginAsync(tmp0, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp1 = new TField();
        if(__isset.@units)
        {
          tmp1.Name = "units";
          tmp1.Type = TType.I64;
          tmp1.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp1, cancellationToken);
          await oprot.WriteI64Async(Units, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.@nanos)
        {
          tmp1.Name = "nanos";
          tmp1.Type = TType.I32;
          tmp1.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp1, cancellationToken);
          await oprot.WriteI32Async(Nanos, cancellationToken);
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
      if (that is not DecimalValue other) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.@units == other.__isset.@units) && ((!__isset.@units) || (global::System.Object.Equals(Units, other.Units))))
        && ((__isset.@nanos == other.__isset.@nanos) && ((!__isset.@nanos) || (global::System.Object.Equals(Nanos, other.Nanos))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.@units)
        {
          hashcode = (hashcode * 397) + Units.GetHashCode();
        }
        if(__isset.@nanos)
        {
          hashcode = (hashcode * 397) + Nanos.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp2 = new StringBuilder("DecimalValue(");
      int tmp3 = 0;
      if(__isset.@units)
      {
        if(0 < tmp3++) { tmp2.Append(", "); }
        tmp2.Append("Units: ");
        Units.ToString(tmp2);
      }
      if(__isset.@nanos)
      {
        if(0 < tmp3++) { tmp2.Append(", "); }
        tmp2.Append("Nanos: ");
        Nanos.ToString(tmp2);
      }
      tmp2.Append(')');
      return tmp2.ToString();
    }
  }

}
