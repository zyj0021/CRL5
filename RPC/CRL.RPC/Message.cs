﻿using DotNetty.Buffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRL.RPC
{
    abstract class MessageBase
    {
        public string MsgId
        {
            get;set;
        }
        public IByteBuffer ToBuffer()
        {
            var data = this.ToByte();
            return Unpooled.WrappedBuffer(data);
        }
    }
    class RequestMessage : MessageBase
    {
        public string Service { get; set; }
        public string Method { get; set; }
        public List<object> Args { get; set; }
        public static RequestMessage FromBuffer(IByteBuffer buffer)
        {
            var data = buffer.ToString(Encoding.UTF8);
            return data.ToObject<RequestMessage>();
        }

    }
    class ResponseMessage : MessageBase
    {
        public bool Success { get; set; }
        public string Data { get; set; }
        public object GetData(Type type)
        {
            return Data.ToObject(type);
        }
        public void SetData(object data)
        {
            Data = data.ToJson();
        }
        public string Msg { get; set; }
        public static ResponseMessage FromBuffer(IByteBuffer buffer)
        {
            var data = buffer.ToString(Encoding.UTF8);
            return data.ToObject<ResponseMessage>();
        }
    }
}