﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using System.Runtime.Serialization.Json;
namespace CRL.Core
{
    public static class SerializeHelper
    {
        #region 二进制格式序列化和反序列化

        /// <summary>
        /// 把对象用二进制格式序列化到流
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="stream">目标流</param>
        public static void BinarySerialize(object obj, Stream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
        }

        /// <summary>
        /// 把对象用二进制格式序列化到文件
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="file">对象的类型</param>
        public static void BinarySerialize(object obj, string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Create))
            {
                BinarySerialize(obj, stream);
            }
        }

        /// <summary>
        /// 从流反序列化对象
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="stream">流</param>
        /// <returns></returns>
        public static T BinaryDeserialize<T>(Stream stream) where T : class
        {
            IFormatter formatter = new BinaryFormatter();
            object obj = formatter.Deserialize(stream);
            if (obj is T)
            {
                return obj as T;
            }
            else
            {
                Type type = typeof(T);
                throw new Exception("反序列化后不能得到类型{1}");
            }
        }

        /// <summary>
        /// 从文件反序列化对象
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="file">文件路径</param>
        /// <returns></returns>
        public static T BinaryDeserialize<T>(string file) where T : class
        {
            using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                return BinaryDeserialize<T>(stream);
            }
        }

        #endregion



        #region Xml格式序列化和反序列化

        /// <summary>
        /// 把对象用Xml格式格式序列化到流
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="stream">流</param>
        public static void XmlSerialize(object obj, Stream stream)
        {
            Type type = obj.GetType();
            XmlSerializer xmlSer = new XmlSerializer(type);
            xmlSer.Serialize(stream, obj);
        }

        /// <summary>
        /// 把对象用Xml格式格式序列化到文件
        /// </summary>
        /// <param name="obj">对象</param>
        /// <param name="file">文件</param>
        public static void XmlSerialize(object obj, string file)
        {
            using (Stream stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                XmlSerialize(obj, stream);
            }
        }
        public static string XmlSerialize(object obj, Encoding encode)
        {
            var ms = new System.IO.MemoryStream();
            XmlSerialize(obj, ms);
            string xml = encode.GetString(ms.ToArray());
            ms.Close();
            return xml;
        }
        public static T XmlDeserialize<T>(string xml, Encoding encode) where T : class
        {
            var arry = encode.GetBytes(xml);
            var ms = new System.IO.MemoryStream(arry);
            var obj = CRL.Core.SerializeHelper.XmlDeserialize<T>(ms);
            ms.Close();
            return obj;
        }
        /// <summary>
        /// 从流反序列化对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="stream">流</param>
        /// <returns></returns>
        public static T XmlDeserialize<T>(Stream stream) where T : class
        {
            Type type = typeof(T);
            XmlSerializer xmlSer = new XmlSerializer(type);
            object obj = xmlSer.Deserialize(stream);
            if (obj is T)
            {
                return obj as T;
            }
            else
            {
                throw new Exception("反序列化后不能得到类型" + type);
            }
        }

        /// <summary>
        /// 从文件反序列化对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="file">文件</param>
        /// <returns></returns>
        public static T XmlDeserialize<T>(string file) where T : class
        {
            using (Stream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                return XmlDeserialize<T>(stream);
            }
        }
        #endregion

        /// <summary>
        /// 利用序列化克隆对象
        /// </summary>
        /// <typeparam name="T">对象的类型</typeparam>
        /// <param name="source">原对象</param>
        /// <returns></returns>
        public static T Clone<T>(T source) where T : class
        {
            if (typeof(T).IsSerializable)
            {
                using (Stream stream = new MemoryStream())
                {
                    BinarySerialize(source, stream);
                    T clone = BinaryDeserialize<T>(stream);
                    return clone;
                }
            }
            else
            {
                throw new Exception("不能用序列化的方式克隆类型为{0}的对象");
            }
        }
        /// <summary>
        /// 使用DataContractJsonSerializer序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializerToJson<T>(T obj)
        {
            var jsonSerialize = new System.Web.Script.Serialization.JavaScriptSerializer();
            return jsonSerialize.Serialize(obj);
            //Type type = typeof(T);
            //DataContractJsonSerializer serilializer = new DataContractJsonSerializer(type);
            //using (Stream stream = new MemoryStream())
            //{
            //    serilializer.WriteObject(stream, obj);
            //    stream.Flush();
            //    stream.Position = 0;
            //    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
            //    string str = reader.ReadToEnd();
            //    reader.Close();
            //    return str;
            //}
        }
        /// <summary>
        /// 使用DataContractJsonSerializer反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T DeserializeFromJson<T>(string json)
        {
            var jsonSerialize = new System.Web.Script.Serialization.JavaScriptSerializer();
            return jsonSerialize.Deserialize<T>(json);
            //byte[] buffer = Encoding.UTF8.GetBytes(json);
            //var type = typeof(T);
            //using (MemoryStream ms = new MemoryStream(buffer))
            //{
            //    DataContractJsonSerializer serializer = new DataContractJsonSerializer(type);
            //    return (T)serializer.ReadObject(ms);
            //}
        }
    }
}