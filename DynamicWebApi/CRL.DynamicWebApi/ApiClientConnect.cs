﻿using CRL.Core.Remoting;
using ImpromptuInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CRL.DynamicWebApi
{
    public class ApiClientConnect: AbsClientConnect
    {
        string host;
        public ApiClientConnect(string _host)
        {
            host = _host;
        }
        public override T GetClient<T>()
        {
            var type = typeof(T);
            var serviceName = type.Name;
            var key = string.Format("{0}_{1}", host, serviceName);
            var a = _services.TryGetValue(key, out object instance);
            if (a)
            {
                return instance as T;
            }
            var info = serviceInfo.GetServiceInfo(type);
            var client = new ApiClient(this)
            {
                HostAddress = new HostAddress() { address = host },
                serviceInfo = info,
            };
            //创建代理
            instance = client.ActLike<T>();
            _services[key] = instance;
            return instance as T;
        }
    }
}
