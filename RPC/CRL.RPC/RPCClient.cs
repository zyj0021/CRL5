﻿
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using System;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Text;
namespace CRL.RPC
{
    class RPCClient : DynamicObject,IDisposable
    {
        public string Host;
        public int Port;
        public string ServiceName;
        public Type ServiceType;
        static Bootstrap bootstrap;
        public string Token;
        IChannel channel = null;

        static ResponseWaits allWaits = new ResponseWaits();
        static RPCClient()
        {
            bootstrap = new Bootstrap()
                .Group(new MultithreadEventLoopGroup())
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    var pipeline = channel.Pipeline;
                    pipeline.AddLast("framing-enc", new LengthFieldPrepender(2));
                    pipeline.AddLast("framing-dec", new LengthFieldBasedFrameDecoder(ushort.MaxValue, 0, 2, 0, 2));

                    pipeline.AddLast(new ClientHandler(allWaits));
                }));
        }
 

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                if (channel == null || !channel.Open)
                {
                    channel = AsyncInvoke.RunSync(() => bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(Host), Port)));
                }
            }
            catch(Exception ero)
            {
                throw new Exception("连接服务端失败:" + ero);
            }
            var id = Guid.NewGuid().ToString();
            allWaits.Add(id);
            var request = new RequestMessage
            {
                MsgId = id,
                Service = ServiceName,
                Method = binder.Name,
                Args = args.ToList(),
                Token = Token
            };

            channel.WriteAndFlushAsync(request.ToBuffer());
            //等待返回
            var response = allWaits.Wait(id).Response;
            if (response == null)
            {
                throw new Exception("请求超时未响应");
            }
            if (!response.Success)
            {
                throw new Exception($"服务端处理错误：{response.Msg}");
            }
            var returnType = ServiceType.GetMethod(binder.Name).ReturnType;
            if (returnType == typeof(void))
            {
                result = null;
                return true;
            }
            result = response.GetData(returnType);
            return true;
        }

        public void Dispose()
        {
            if (channel != null)
            {
                channel.CloseAsync();
            }
        }
    }
}
