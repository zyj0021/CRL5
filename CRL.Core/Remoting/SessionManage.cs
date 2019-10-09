﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CRL.Core.Remoting
{
    public interface ISessionManage
    {
        void SaveSession(string user, string token, object tag = null);
        bool CheckSession(string user, string token, ParameterInfo[] argsName, List<object> args, out string error);
        Tuple<string, object> GetSession(string user);
    }
    public class SignCheck
    {
        public static string CreateSign(string key, ParameterInfo[] argsName, List<object> args)
        {
            var dic = new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var list = new List<string>();
            for (int i = 0; i < argsName.Length; i++)
            {
                if (argsName[i].Name.ToLower() == "token")
                {
                    continue;
                }
                dic.Add(argsName[i].Name, args[i]);
            }
            foreach (var kv in dic)
            {
                list.Add(string.Format("{0}={1}", kv.Key, kv.Value));
            }
            var str = string.Join("&", list);
            var sign = Core.StringHelper.EncryptMD5(str + "&" + key);
            return sign;
        }
    }
    public class SessionManage : ISessionManage
    {
        static ConcurrentDictionary<string, Tuple<string, object>> sessions = new ConcurrentDictionary<string, Tuple<string, object>>();
        /// <summary>
        /// 登录后返回新的TOKEN
        /// </summary>
        /// <param name="user"></param>
        /// <param name="token"></param>
        /// <param name="tag"></param>
        public void SaveSession(string user, string token, object tag = null)
        {
            if (!sessions.TryGetValue(user, out Tuple<string, object> token2))
            {
                sessions.TryAdd(user, new Tuple<string, object>(token, tag));
            }
            else
            {
                sessions[user] = new Tuple<string, object>(token, tag);
            }
        }

        public bool CheckSession(string user, string token, ParameterInfo[] argsName, List<object> args, out string error)
        {
            error = "";
            var exists = sessions.TryGetValue(user, out Tuple<string, object> v);
            if (!exists)
            {
                error = "API未登录";
                return false;
            }
            var serverToken = v.Item1;
            if (ServerCreater.__CheckSign)//使用简单签名
            {
                serverToken = SignCheck.CreateSign(serverToken, argsName, args);
            }
            if (token != serverToken)
            {
                error = "token验证失败";
                return false;
            }
            return true;
        }

        public Tuple<string, object> GetSession(string user)
        {
            sessions.TryGetValue(user, out Tuple<string, object> v);
            return v;
        }
    }
}
