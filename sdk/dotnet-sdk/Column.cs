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

  public partial class Column : TBase
  {
    private int _id;
    private string? _description;

    public int Id
    {
      get
      {
        return _id;
      }
      set
      {
        __isset.id = true;
        this._id = value;
      }
    }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 
    /// <seealso cref="global::QueryCat.Plugins.Sdk.DataType"/>
    /// </summary>
    public global::QueryCat.Plugins.Sdk.DataType Type { get; set; } = default;

    public string? Description
    {
      get
      {
        return _description;
      }
      set
      {
        __isset.description = true;
        this._description = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool id;
      public bool description;
    }

    public Column()
    {
    }

    public Column(string name, global::QueryCat.Plugins.Sdk.DataType type) : this()
    {
      this.Name = name;
      this.Type = type;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_name = false;
        bool isset_type = false;
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
                Id = await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.String)
              {
                Name = await iprot.ReadStringAsync(cancellationToken);
                isset_name = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.I32)
              {
                Type = (global::QueryCat.Plugins.Sdk.DataType)await iprot.ReadI32Async(cancellationToken);
                isset_type = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 4:
              if (field.Type == TType.String)
              {
                Description = await iprot.ReadStringAsync(cancellationToken);
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
        if (!isset_name)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_type)
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
        var tmp24 = new TStruct("Column");
        await oprot.WriteStructBeginAsync(tmp24, cancellationToken);
        var tmp25 = new TField();
        if(__isset.id)
        {
          tmp25.Name = "id";
          tmp25.Type = TType.I32;
          tmp25.ID = 1;
          await oprot.WriteFieldBeginAsync(tmp25, cancellationToken);
          await oprot.WriteI32Async(Id, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((Name != null))
        {
          tmp25.Name = "name";
          tmp25.Type = TType.String;
          tmp25.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp25, cancellationToken);
          await oprot.WriteStringAsync(Name, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        tmp25.Name = "type";
        tmp25.Type = TType.I32;
        tmp25.ID = 3;
        await oprot.WriteFieldBeginAsync(tmp25, cancellationToken);
        await oprot.WriteI32Async((int)Type, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if((Description != null) && __isset.description)
        {
          tmp25.Name = "description";
          tmp25.Type = TType.String;
          tmp25.ID = 4;
          await oprot.WriteFieldBeginAsync(tmp25, cancellationToken);
          await oprot.WriteStringAsync(Description, cancellationToken);
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
      if (that is not Column other) return false;
      if (ReferenceEquals(this, other)) return true;
      return ((__isset.id == other.__isset.id) && ((!__isset.id) || (global::System.Object.Equals(Id, other.Id))))
        && global::System.Object.Equals(Name, other.Name)
        && global::System.Object.Equals(Type, other.Type)
        && ((__isset.description == other.__isset.description) && ((!__isset.description) || (global::System.Object.Equals(Description, other.Description))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        if(__isset.id)
        {
          hashcode = (hashcode * 397) + Id.GetHashCode();
        }
        if((Name != null))
        {
          hashcode = (hashcode * 397) + Name.GetHashCode();
        }
        hashcode = (hashcode * 397) + Type.GetHashCode();
        if((Description != null) && __isset.description)
        {
          hashcode = (hashcode * 397) + Description.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp26 = new StringBuilder("Column(");
      int tmp27 = 0;
      if(__isset.id)
      {
        if(0 < tmp27++) { tmp26.Append(", "); }
        tmp26.Append("Id: ");
        Id.ToString(tmp26);
      }
      if((Name != null))
      {
        if(0 < tmp27) { tmp26.Append(", "); }
        tmp26.Append("Name: ");
        Name.ToString(tmp26);
      }
      tmp26.Append(", Type: ");
      Type.ToString(tmp26);
      if((Description != null) && __isset.description)
      {
        tmp26.Append(", Description: ");
        Description.ToString(tmp26);
      }
      tmp26.Append(')');
      return tmp26.ToString();
    }
  }

}