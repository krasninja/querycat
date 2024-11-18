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

  public partial class QueryCatPluginException : TException, TBase
  {
    private int _object_handle;
    private string? _exception_type;
    private string? _exception_stack_trace;
    private global::QueryCat.Plugins.Sdk.QueryCatPluginException? _exception_nested;

    /// <summary>
    /// 
    /// <seealso cref="global::QueryCat.Plugins.Sdk.ErrorType"/>
    /// </summary>
    public global::QueryCat.Plugins.Sdk.ErrorType Type { get; set; } = default;

    public string ErrorMessage { get; set; } = string.Empty;

    public int ObjectHandle
    {
      get
      {
        return _object_handle;
      }
      set
      {
        __isset.object_handle = true;
        this._object_handle = value;
      }
    }

    public string? ExceptionType
    {
      get
      {
        return _exception_type;
      }
      set
      {
        __isset.exception_type = true;
        this._exception_type = value;
      }
    }

    public string? ExceptionStackTrace
    {
      get
      {
        return _exception_stack_trace;
      }
      set
      {
        __isset.exception_stack_trace = true;
        this._exception_stack_trace = value;
      }
    }

    public global::QueryCat.Plugins.Sdk.QueryCatPluginException? ExceptionNested
    {
      get
      {
        return _exception_nested;
      }
      set
      {
        __isset.exception_nested = true;
        this._exception_nested = value;
      }
    }


    public Isset __isset;
    public struct Isset
    {
      public bool object_handle;
      public bool exception_type;
      public bool exception_stack_trace;
      public bool exception_nested;
    }

    public QueryCatPluginException()
    {
    }

    public QueryCatPluginException(global::QueryCat.Plugins.Sdk.ErrorType @type, string error_message) : this()
    {
      this.Type = @type;
      this.ErrorMessage = error_message;
    }

    public async global::System.Threading.Tasks.Task ReadAsync(TProtocol iprot, CancellationToken cancellationToken)
    {
      iprot.IncrementRecursionDepth();
      try
      {
        bool isset_type = false;
        bool isset_error_message = false;
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
                Type = (global::QueryCat.Plugins.Sdk.ErrorType)await iprot.ReadI32Async(cancellationToken);
                isset_type = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 2:
              if (field.Type == TType.String)
              {
                ErrorMessage = await iprot.ReadStringAsync(cancellationToken);
                isset_error_message = true;
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 3:
              if (field.Type == TType.I32)
              {
                ObjectHandle = await iprot.ReadI32Async(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 4:
              if (field.Type == TType.String)
              {
                ExceptionType = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 5:
              if (field.Type == TType.String)
              {
                ExceptionStackTrace = await iprot.ReadStringAsync(cancellationToken);
              }
              else
              {
                await TProtocolUtil.SkipAsync(iprot, field.Type, cancellationToken);
              }
              break;
            case 6:
              if (field.Type == TType.Struct)
              {
                ExceptionNested = new global::QueryCat.Plugins.Sdk.QueryCatPluginException();
                await ExceptionNested.ReadAsync(iprot, cancellationToken);
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
        if (!isset_type)
        {
          throw new TProtocolException(TProtocolException.INVALID_DATA);
        }
        if (!isset_error_message)
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
        var tmp12 = new TStruct("QueryCatPluginException");
        await oprot.WriteStructBeginAsync(tmp12, cancellationToken);
        #pragma warning disable IDE0017  // simplified init
        var tmp13 = new TField();
        tmp13.Name = "type";
        tmp13.Type = TType.I32;
        tmp13.ID = 1;
        await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
        await oprot.WriteI32Async((int)Type, cancellationToken);
        await oprot.WriteFieldEndAsync(cancellationToken);
        if((ErrorMessage != null))
        {
          tmp13.Name = "error_message";
          tmp13.Type = TType.String;
          tmp13.ID = 2;
          await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
          await oprot.WriteStringAsync(ErrorMessage, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if(__isset.object_handle)
        {
          tmp13.Name = "object_handle";
          tmp13.Type = TType.I32;
          tmp13.ID = 3;
          await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
          await oprot.WriteI32Async(ObjectHandle, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((ExceptionType != null) && __isset.exception_type)
        {
          tmp13.Name = "exception_type";
          tmp13.Type = TType.String;
          tmp13.ID = 4;
          await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
          await oprot.WriteStringAsync(ExceptionType, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((ExceptionStackTrace != null) && __isset.exception_stack_trace)
        {
          tmp13.Name = "exception_stack_trace";
          tmp13.Type = TType.String;
          tmp13.ID = 5;
          await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
          await oprot.WriteStringAsync(ExceptionStackTrace, cancellationToken);
          await oprot.WriteFieldEndAsync(cancellationToken);
        }
        if((ExceptionNested != null) && __isset.exception_nested)
        {
          tmp13.Name = "exception_nested";
          tmp13.Type = TType.Struct;
          tmp13.ID = 6;
          await oprot.WriteFieldBeginAsync(tmp13, cancellationToken);
          await ExceptionNested.WriteAsync(oprot, cancellationToken);
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
      if (that is not QueryCatPluginException other) return false;
      if (ReferenceEquals(this, other)) return true;
      return global::System.Object.Equals(Type, other.Type)
        && global::System.Object.Equals(ErrorMessage, other.ErrorMessage)
        && ((__isset.object_handle == other.__isset.object_handle) && ((!__isset.object_handle) || (global::System.Object.Equals(ObjectHandle, other.ObjectHandle))))
        && ((__isset.exception_type == other.__isset.exception_type) && ((!__isset.exception_type) || (global::System.Object.Equals(ExceptionType, other.ExceptionType))))
        && ((__isset.exception_stack_trace == other.__isset.exception_stack_trace) && ((!__isset.exception_stack_trace) || (global::System.Object.Equals(ExceptionStackTrace, other.ExceptionStackTrace))))
        && ((__isset.exception_nested == other.__isset.exception_nested) && ((!__isset.exception_nested) || (global::System.Object.Equals(ExceptionNested, other.ExceptionNested))));
    }

    public override int GetHashCode() {
      int hashcode = 157;
      unchecked {
        hashcode = (hashcode * 397) + Type.GetHashCode();
        if((ErrorMessage != null))
        {
          hashcode = (hashcode * 397) + ErrorMessage.GetHashCode();
        }
        if(__isset.object_handle)
        {
          hashcode = (hashcode * 397) + ObjectHandle.GetHashCode();
        }
        if((ExceptionType != null) && __isset.exception_type)
        {
          hashcode = (hashcode * 397) + ExceptionType.GetHashCode();
        }
        if((ExceptionStackTrace != null) && __isset.exception_stack_trace)
        {
          hashcode = (hashcode * 397) + ExceptionStackTrace.GetHashCode();
        }
        if((ExceptionNested != null) && __isset.exception_nested)
        {
          hashcode = (hashcode * 397) + ExceptionNested.GetHashCode();
        }
      }
      return hashcode;
    }

    public override string ToString()
    {
      var tmp14 = new StringBuilder("QueryCatPluginException(");
      tmp14.Append(", Type: ");
      Type.ToString(tmp14);
      if((ErrorMessage != null))
      {
        tmp14.Append(", ErrorMessage: ");
        ErrorMessage.ToString(tmp14);
      }
      if(__isset.object_handle)
      {
        tmp14.Append(", ObjectHandle: ");
        ObjectHandle.ToString(tmp14);
      }
      if((ExceptionType != null) && __isset.exception_type)
      {
        tmp14.Append(", ExceptionType: ");
        ExceptionType.ToString(tmp14);
      }
      if((ExceptionStackTrace != null) && __isset.exception_stack_trace)
      {
        tmp14.Append(", ExceptionStackTrace: ");
        ExceptionStackTrace.ToString(tmp14);
      }
      if((ExceptionNested != null) && __isset.exception_nested)
      {
        tmp14.Append(", ExceptionNested: ");
        ExceptionNested.ToString(tmp14);
      }
      tmp14.Append(')');
      return tmp14.ToString();
    }
  }

}
