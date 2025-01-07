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

  public partial class KeyColumn : TBase
  {

    public int ColumnIndex { get; set; } = 0;

    public bool IsRequired { get; set; } = false;

    public List<string>? Operations { get; set; }

    public KeyColumn()
    {
    }

    public KeyColumn(int column_index, bool is_required, List<string>? @operations) : this()
    {
      this.ColumnIndex = column_index;
      this.IsRequired = is_required;
      this.Operations = @operations;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_column_index = false;
        bool isset_is_required = false;
        bool isset_operations = false;
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
              if (field.Type == TType.I32)
              {
                ColumnIndex = await iprot.ReadI32Async(cancellationToken);
                isset_column_index = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.Bool)
              {
                IsRequired = await iprot.ReadBoolAsync(cancellationToken);
                isset_is_required = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.List)
              {
                {
                  var _list52 = await iprot.ReadListBeginAsync(cancellationToken);
                  Operations = new List<string>(_list52.Count);
                  for(int _i53 = 0; _i53 < _list52.Count; ++_i53)
                  {
                    string _elem54;
                    _elem54 = await iprot.ReadStringAsync(cancellationToken);
                    Operations.Add(_elem54);
                  }
                  await iprot.ReadListEndAsync(cancellationToken);
                }
                isset_operations = true;
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
        if (!isset_column_index)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_is_required)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_operations)
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
        var tmp55 = new TStruct("KeyColumn");
        await oprot.WriteStructBeginAsync(tmp55, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp56 = new TField();
        tmp56.Name = "column_index";
        tmp56.Type = TType.I32;
        tmp56.ID = 1;
        await oprot.WriteFieldBeginAsync(tmp56, cancellationToken);
        await oprot.WriteI32Async(ColumnIndex, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        tmp56.Name = "is_required";
        tmp56.Type = TType.Bool;
        tmp56.ID = 2;
        await oprot.WriteFieldBeginAsync(tmp56, cancellationToken);
        await oprot.WriteBoolAsync(IsRequired, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if((Operations != null))
        {
          tmp56.Name = "operations";
          tmp56.Type = TType.List;
          tmp56.ID = 3;
          await oprot.WriteFieldBeginAsync(tmp56, cancellationToken);
          await oprot.WriteListBeginAsync(new TList(TType.String, Operations.Count), cancellationToken);
          foreach (string _iter57 in Operations)
          {
            await oprot.WriteStringAsync(_iter57, cancellationToken);
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
      if (that is not KeyColumn other) return false;
      if (ReferenceEquals(this, other)) return true;
      return global::System.Object.Equals(ColumnIndex, other.ColumnIndex)
        && global::System.Object.Equals(IsRequired, other.IsRequired)
        && TCollections.Equals(Operations, other.Operations);
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        hashcode = (hashcode * 397) + ColumnIndex.GetHashCode();
        hashcode = (hashcode * 397) + IsRequired.GetHashCode();
        if((Operations != null))
        {
          hashcode = (hashcode * 397) + TCollections.GetHashCode(Operations);
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp58 = new StringBuilder("KeyColumn(");
      tmp58.Append(", ColumnIndex: ");
      ColumnIndex.ToString(tmp58);
      tmp58.Append(", IsRequired: ");
      IsRequired.ToString(tmp58);
      if((Operations != null))
      {
        tmp58.Append(", Operations: ");
        Operations.ToString(tmp58);
      }
      tmp58.Append(')');
      return tmp58.ToString();
    }
  }

}
