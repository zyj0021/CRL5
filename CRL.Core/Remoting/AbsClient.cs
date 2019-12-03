﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CRL.Core.Remoting
{
    public class HostAddress
    {
        public string address;
        public int port;
        public override string ToString()
        {
            var str = address;
            if (port > 0)
            {
                str += $":{port}";
            }
            return str;
        }
    }
    public abstract class AbsClient: DynamicObject, IDisposable
    {
        HostAddress hostAddress;
        public HostAddress HostAddress
        {
            get
            {
                //获取consol服务发现
                if (clientConnect.__GetConsulAgent != null)
                {
                    return clientConnect.__GetConsulAgent();
                }
                return hostAddress;
            }
            set
            {
                hostAddress = value;
            }
        }
    
        public string ServiceName
        {
            get
            {
                return serviceInfo.ServiceType.Name;
            }
        }
        public Type ServiceType
        {
            get
            {
                return serviceInfo.ServiceType;
            }
        }
        public serviceInfo serviceInfo;
        public AbsClient(AbsClientConnect _clientConnect)
        {
            clientConnect = _clientConnect;
        }
        protected AbsClientConnect clientConnect;
        protected void ThrowError(string msg, string code)
        {
            clientConnect.OnError?.Invoke(msg, code);
            throw new RemotingEx(msg) { Code = code };

            //else
            //{
            //    throw new RemotingEx(msg) { Code = code };
            //}
        }
        public virtual void Dispose()
        {

        }
        protected string GetToken(ParameterInfo[] argsName, List<object> args,string token)
        {
            if (clientConnect.__UseSign && !string.IsNullOrEmpty(token))
            {
                var arry = token.Split('@');
                var sign = SignCheck.CreateSign(arry[1], argsName, args);
                token = string.Format("{0}@{1}", arry[0], sign);
            }
            return token;
        }
    }
}
