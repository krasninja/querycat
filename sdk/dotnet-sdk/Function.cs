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

  public partial class Function : TBase
  {
    private bool _is_safe;
    private List<string>? _formatter_ids;

    public string Signature { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsAggregate { get; set; } = false;

    public bool IsSafe
    {
      get
      {
        return _is_safe;
      }
      set
      {
        __isset.is_safe = true;
        this._is_safe = value;
      }
    }

    public List<string>? FormatterIds
    {
      get
      {
        return _formatter_ids;
      }
      set
      {
        __isset.formatter_ids = true;
        this._formatter_ids = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool is_safe;
      public bool formatter_ids;
    }

    public Function()
    {
    }

    public Function(string @signature, string @description, bool is_aggregate) : this()
    {
      this.Signature = @signature;
      this.Description = @description;
      this.IsAggregate = is_aggregate;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_signature = false;
        bool isset_description = false;
        bool isset_is_aggregate = false;
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
              if (field.Type == TType.String)
              {
                Signature = await iprot.ReadStringAsync(cancellationToken);
                isset_signature = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.String)
              {
                Description = await iprot.ReadStringAsync(cancellationToken);
                isset_description = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.Bool)
              {
                IsAggregate = await iprot.ReadBoolAsync(cancellationToken);
                isset_is_aggregate = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 4:
              if (field.Type == TType.Bool)
              {
                IsSafe = await iprot.ReadBoolAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 5:
              if (field.Type == TType.List)
              {
                {
                  var _list16 = await iprot.ReadListBeginAsync(cancellationToken);
                  FormatterIds = new List<string>(_list16.Count);
                  for(int _i17 = 0; _i17 < _list16.Count; ++_i17)
                  {
                    string _elem18;
                    _elem18 = await iprot.ReadStringAsync(cancellationToken);
                    FormatterIds.Add(_elem18);
                  }
                  await iprot.ReadListEndAsync(cancellationToken);
                }
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
        if (!isset_signature)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_description)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_is_aggregate)
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
        var tmp19 = new TStruct("Function");
        await oprot.WriteStructBeginAsync(tmp19, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp20 = new TField();
        if((Signature != null))
        {
          tmp20.Name = "signature";
          tmp20.Type = TType.String;
          tmp20.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp20, cancellationToken);
          await oprot.WriteStringAsync(Signature, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Description != null))
        {
          tmp20.Name = "description";
          tmp20.Type = TType.String;
          tmp20.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp20, cancellationToken);
          await oprot.WriteStringAsync(Description, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        tmp20.Name = "is_aggregate";
        tmp20.Type = TType.Bool;
        tmp20.ID = 3;
        await oprot.WriteFieldBeginAsync(tmp20, cancellationToken);
        await oprot.WriteBoolAsync(IsAggregate, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if(__isset.is_safe)
        {
          tmp20.Name = "is_safe";
          tmp20.Type = TType.Bool;
          tmp20.ID = 4;
          await oprot.WriteFieldBeginAsync(tmp20, cancellationToken);
          await oprot.WriteBoolAsync(IsSafe, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((FormatterIds != null) && __isset.formatter_ids)
        {
          tmp20.Name = "formatter_ids";
          tmp20.Type = TType.List;
          tmp20.ID = 5;
          await oprot.WriteFieldBeginAsync(tmp20, cancellationToken);
          await oprot.WriteListBeginAsync(new TList(TType.String, FormatterIds.Count), cancellationToken);
          foreach (string _iter21 in FormatterIds)
          {
            await oprot.WriteStringAsync(_iter21, cancellationToken);
          }
          await oprot.WriteListEndAsync(cancellationToken);
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
      if (that is not Function other) return false;
      if (ReferenceEquals(this, other)) return true;
      return global::System.Object.Equals(Signature, other.Signature)
        && global::System.Object.Equals(Description, other.Description)
        && global::System.Object.Equals(IsAggregate, other.IsAggregate)
        && ((__isset.is_safe == other.__isset.is_safe) && ((!__isset.is_safe) || (global::System.Object.Equals(IsSafe, other.IsSafe))))
        && ((__isset.formatter_ids == other.__isset.formatter_ids) && ((!__isset.formatter_ids) || (TCollections.Equals(FormatterIds, other.FormatterIds))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if((Signature != null))
        {
          hashcode = (hashcode * 397) + Signature.GetHashCode();
        }
        if((Description != null))
        {
          hashcode = (hashcode * 397) + Description.GetHashCode();
        }
        hashcode = (hashcode * 397) + IsAggregate.GetHashCode();
        if(__isset.is_safe)
        {
          hashcode = (hashcode * 397) + IsSafe.GetHashCode();
        }
        if((FormatterIds != null) && __isset.formatter_ids)
        {
          hashcode = (hashcode * 397) + TCollections.GetHashCode(FormatterIds);
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp22 = new StringBuilder("Function(");
      if((Signature != null))
      {
        tmp22.Append(", Signature: ");
        Signature.ToString(tmp22);
      }
      if((Description != null))
      {
        tmp22.Append(", Description: ");
        Description.ToString(tmp22);
      }
      tmp22.Append(", IsAggregate: ");
      IsAggregate.ToString(tmp22);
      if(__isset.is_safe)
      {
        tmp22.Append(", IsSafe: ");
        IsSafe.ToString(tmp22);
      }
      if((FormatterIds != null) && __isset.formatter_ids)
      {
        tmp22.Append(", FormatterIds: ");
        FormatterIds.ToString(tmp22);
      }
      tmp22.Append(')');
      return tmp22.ToString();
    }
  }

}
