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

  public partial class RegistrationResult : TBase
  {

    public string Version { get; set; } = string.Empty;

    public List<int>? FunctionsIds { get; set; }

    public RegistrationResult()
    {
    }

    public RegistrationResult(string @version, List<int>? functions_ids) : this()
    {
      this.Version = @version;
      this.FunctionsIds = functions_ids;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_version = false;
        bool isset_functions_ids = false;
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
                Version = await iprot.ReadStringAsync(cancellationToken);
                isset_version = true;
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
                  var _list28 = await iprot.ReadListBeginAsync(cancellationToken);
                  FunctionsIds = new List<int>(_list28.Count);
                  for(int _i29 = 0; _i29 < _list28.Count; ++_i29)
                  {
                    int _elem30;
                    _elem30 = await iprot.ReadI32Async(cancellationToken);
                    FunctionsIds.Add(_elem30);
                  }
                  await iprot.ReadListEndAsync(cancellationToken);
                }
                isset_functions_ids = true;
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
        if (!isset_version)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_functions_ids)
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
        var tmp31 = new TStruct("RegistrationResult");
        await oprot.WriteStructBeginAsync(tmp31, cancellationToken);
        var tmp32 = new TField();
        if((Version != null))
        {
          tmp32.Name = "version";
          tmp32.Type = TType.String;
          tmp32.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp32, cancellationToken);
          await oprot.WriteStringAsync(Version, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((FunctionsIds != null))
        {
          tmp32.Name = "functions_ids";
          tmp32.Type = TType.List;
          tmp32.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp32, cancellationToken);
          await oprot.WriteListBeginAsync(new TList(TType.I32, FunctionsIds.Count), cancellationToken);
          foreach (int _iter33 in FunctionsIds)
          {
            await oprot.WriteI32Async(_iter33, cancellationToken);
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
      if (that is not RegistrationResult other) return false;
      if (ReferenceEquals(this, other)) return true;
      return global::System.Object.Equals(Version, other.Version)
        && TCollections.Equals(FunctionsIds, other.FunctionsIds);
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if((Version != null))
        {
          hashcode = (hashcode * 397) + Version.GetHashCode();
        }
        if((FunctionsIds != null))
        {
          hashcode = (hashcode * 397) + TCollections.GetHashCode(FunctionsIds);
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp34 = new StringBuilder("RegistrationResult(");
      if((Version != null))
      {
        tmp34.Append(", Version: ");
        Version.ToString(tmp34);
      }
      if((FunctionsIds != null))
      {
        tmp34.Append(", FunctionsIds: ");
        FunctionsIds.ToString(tmp34);
      }
      tmp34.Append(')');
      return tmp34.ToString();
    }
  }

}
