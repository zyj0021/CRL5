
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using CRL.Core;
using CRL.Core.Session;

namespace CRL.Core.RedisProvider
{
    /// <summary>
    /// redis缓存客户端调用类
    /// </summary>
    public class RedisClient
    {
        public static Func<string> GetRedisConn;
        public bool Remove(string key)
        {
            return StackExchangeRedisHelper.Remove(key);
        }

        public T KGet<T>(string key)
        {
            bool find;
            return KGet<T>(key, out find);
        }
        public T KGet<T>(string key, out bool find)
        {
            var str = KGet(key, out find);
            if (find)
            {
                return SerializeHelper.DeserializeFromJson<T>(str);
            }
            return default(T);
        }
        public string KGet(string key)
        {
            bool find;
            return KGet(key, out find);
        }
        /// <summary>
        /// 读取Key/Value值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public string KGet(string key, out bool find)
        {
            find = false;
            var str = StackExchangeRedisHelper.Get(key);
            find = str != null;
            return str;
        }


        #region hash
        public void HSet(string hashId, string key, object obj)
        {
            StackExchangeRedisHelper.HSet(hashId, key, obj);
        }

        public bool HRemove(string hashId, string key)
        {
            return StackExchangeRedisHelper.HRemove(hashId,key);
        }

        public T HGet<T>(string hashId, string key)
        {
            return StackExchangeRedisHelper.HGet<T>(hashId, key);
        }

        public List<T> HGetAll<T>(string hashId)
        {
            return StackExchangeRedisHelper.HGetAll<T>(hashId);
        }

        public bool HContainsKey(string hashId, string key)
        {
            return StackExchangeRedisHelper.HContainsKey(hashId,key);
        }

        public long GetHashCount(string hashId)
        {
            return StackExchangeRedisHelper.GetHashCount(hashId);
        }
        public List<string> HGetAllKeys(string hashId)
        {
            var allKeys = StackExchangeRedisHelper.GetDatabase().HashKeys(hashId);
            if (allKeys.Length == 0)
            {
                return new List<string>();
            }
            return allKeys.Select(b => b.ToString()).ToList();
        }
        #endregion
        /// <summary>
        /// 延期
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expiresTime"></param>
        public void KSetEntryIn(string key, TimeSpan expiresTime)
        {
            StackExchangeRedisHelper.GetDatabase().KeyExpire(key, expiresTime);
        }

        public void KSet(string key, object obj, TimeSpan timeSpan)
        {
            StackExchangeRedisHelper.Set(key, obj, timeSpan);
        }

        public bool ContainsKey(string key)
        {
            return StackExchangeRedisHelper.GetDatabase().KeyExists(key);
        }
        /// <summary>
        /// 递增
        /// </summary>
        /// <param name="key"></param>
        /// <param name="type"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public long Increment(string key, string type, TimeSpan timeOut, int num = 1)
        {
            key = string.Format("Increment_{0}", key);
            var exis = ContainsKey(key);

            var result = StackExchangeRedisHelper.GetDatabase().HashIncrement(key, type, num);
            if (!exis)
            {
                //自动失效,清理垃圾数据
                KSetEntryIn(key, timeOut);
            }
            return result;
        }

        #region 消息订阅发布
        public long Pubblish<T>(T obj)
        {       //只限当前进程
            var process = System.Diagnostics.Process.GetCurrentProcess().Id;
            var channelName = string.Format("{0}_{1}", process, typeof(T).Name);
            string ser = SerializeHelper.SerializerToJson(obj);
            return StackExchangeRedisHelper.Publish(channelName, ser);
        }
        public void Subscribe<T>(Action<T> callBack)
        {
            //只限当前进程
            var process = System.Diagnostics.Process.GetCurrentProcess().Id;
            var channelName = string.Format("{0}_{1}", process, typeof(T).Name);
            StackExchangeRedisHelper.Subscribe(channelName, callBack);
            EventLog.Info($"RedisMessage 启动 {channelName}");
        }
        #endregion

        #region CustomerCache
        class HashTime<T>
        {
            public T data
            {
                get; set;
            }
            public DateTime time
            {
                get; set;
            }
        }
        /// <summary>
        /// 按类型自动过期的自定义HASH
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="timeOutSecond"></param>
        /// <param name="dataCall"></param>
        /// <returns></returns>
        public T GetCustomCache<T>(string key, int timeOutSecond, Func<T> dataCall) where T : class, new()
        {
            var type = StringHelper.EncryptMD5(typeof(T).FullName);
            return GetCustomCacheBase(type, key, timeOutSecond, dataCall);
        }
        public T GetCustomCache<T>(string type, string key, int timeOutSecond, Func<T> dataCall)
        {
            var type2 = string.Format("{0}_{1}", typeof(T).Name, type);
            return GetCustomCacheBase(type2, key, timeOutSecond, dataCall);
        }
        //System.Collections.Concurrent.ConcurrentDictionary<string, expObj> removeKeys = new System.Collections.Concurrent.ConcurrentDictionary<string, expObj>();
        T GetCustomCacheBase<T>(string type, string key, int timeOutSecond, Func<T> dataCall)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default(T);
            }
            var hashId = string.Format("customCache_{0}", type);
        label1:
            var value = HGet<HashTime<T>>(hashId, key);
            if (value == null)
            {
                var obj = dataCall();
                if (obj == null)
                {
                    return default(T);
                }
                value = new HashTime<T>() { data = obj, time = DateTime.Now.AddSeconds(timeOutSecond) };
                HSet(hashId, key, value);
                //removeKeys.TryAdd(hashId + "_" + key, new expObj() { hashId = hashId, key = key, time = value.time });
            }
            else if (value.time < DateTime.Now)
            {
                HRemove(hashId, key);
                //expObj expObj;
                //removeKeys.TryRemove(hashId + "_" + key, out expObj);
                goto label1;
            }
            return value.data;
        }
        #endregion
        #region list
        public long ListRightPush(string key, object value)
        {
            return StackExchangeRedisHelper.ListRightPush(key, value);
        }
        public long ListRemove(string key, object value)
        {
            return StackExchangeRedisHelper.ListRemove(key, value);
        }
        public List<T> ListRange<T>(string key, long start, long end)
        {
            return StackExchangeRedisHelper.ListRange<T>(key, start, end);
        }
        public  void ListTrim(string key, long start, long end)
        {
            StackExchangeRedisHelper.ListTrim(key, start, end);
        }
        public  long ListLength(string key)
        {
            return StackExchangeRedisHelper.ListLength(key);
        }
        #endregion

    }

}