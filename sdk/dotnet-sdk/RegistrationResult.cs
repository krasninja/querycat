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

  public partial class RegistrationResult : TBase
  {

    public long Token { get; set; } = 0;

    public string Version { get; set; } = string.Empty;

    public RegistrationResult()
    {
    }

    public RegistrationResult(long @token, string @version) : this()
    {
      this.Token = @token;
      this.Version = @version;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_token = false;
        bool isset_version = false;
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
                Token = await iprot.ReadI64Async(cancellationToken);
                isset_token = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
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
            default: 
              await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              break;
          }

          await iprot.ReadFieldEndAsync(cancellationToken);
        }

        await iprot.ReadStructEndAsync(cancellationToken);
        if (!isset_token)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_version)
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
        var tmp32 = new TStruct("RegistrationResult");
        await oprot.WriteStructBeginAsync(tmp32, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp33 = new TField();
        tmp33.Name = "token";
        tmp33.Type = TType.I64;
        tmp33.ID = 1;
        await oprot.WriteFieldBeginAsync(tmp33, cancellationToken);
        await oprot.WriteI64Async(Token, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if((Version != null))
        {
          tmp33.Name = "version";
          tmp33.Type = TType.String;
          tmp33.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp33, cancellationToken);
          await oprot.WriteStringAsync(Version, cancellationToken);
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
      if (that is not RegistrationResult other) return false;
      if (ReferenceEquals(this, other)) return true;
      return global::System.Object.Equals(Token, other.Token)
        && global::System.Object.Equals(Version, other.Version);
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        hashcode = (hashcode * 397) + Token.GetHashCode();
        if((Version != null))
        {
          hashcode = (hashcode * 397) + Version.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp34 = new StringBuilder("RegistrationResult(");
      tmp34.Append(", Token: ");
      Token.ToString(tmp34);
      if((Version != null))
      {
        tmp34.Append(", Version: ");
        Version.ToString(tmp34);
      }
      tmp34.Append(')');
      return tmp34.ToString();
    }
  }

}
