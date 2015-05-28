using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RedisCore;
using Trinity;
using Trinity.Extension;
namespace RedisCore
{
    public class ClientCommands
    {
        static int slaveCount = 1;
        public void Set(string key, string value)
        {
            MD5Utility useMD5 = new MD5Utility();
            if (value.StartsWith("[") && value.EndsWith("]"))
            {
                Console.WriteLine("setting the value under \"{0}\" to a List", key);
                value = value.TrimStart("[".ToCharArray());
                value = value.TrimEnd("]".ToCharArray());
                string[] numbers = value.Split(",".ToCharArray());
                List<int> list_value = new List<int>();
                foreach (string s in numbers)
                    list_value.Add(int.Parse(s.Trim()));

                //notice! since it's a list value, set the other two arguments to be null, so they won't take extraspace
                Element e = new Element(new Value(null, null, list_value), key);
                long md5 = useMD5.GetMD5(key);
                Global.CloudStorage.SetValueToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new ElementRequestWriter(e));

            }
            else if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                Console.WriteLine("setting the value under \"{0}\" to a string", key);
                value = value.TrimStart("\"".ToCharArray());
                value = value.TrimEnd("\"".ToCharArray());
                string string_value = value;

                //notice! since it's a string value, set the other two arguments to be null, so they won't take extraspace
                Element e = new Element(new Value(null, string_value, null), key);
                long md5 = useMD5.GetMD5(key);
                Global.CloudStorage.SetValueToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new ElementRequestWriter(e));
            }
            else
            {
                double double_value = 0;
                if (!double.TryParse(value.Trim(), out double_value))
                {
                    Console.WriteLine("don't understand what you're talking about.");
                }
                Console.WriteLine("setting the value under \"{0}\" to a double", key);

                //notice! since it's a double value, set the other two arguments to be null, so they won't take extraspace
                Element e = new Element(new Value(double_value, null, null), key);
                long md5 = useMD5.GetMD5(key);
                Global.CloudStorage.SetValueToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new ElementRequestWriter(e));

            }
        }
        public void Get(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (reader.isFound)
            {
                if (reader.value.Contains_int_list_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a List", key);
                    Console.Write("List of Int: [");
                    for (int i = 0; i < reader.value.int_list_value.Count - 1; i++)
                        Console.Write(reader.value.int_list_value[i] + ",");
                    Console.Write(reader.value.int_list_value[reader.value.int_list_value.Count - 1]);
                    Console.WriteLine("]");
                }
                else if (reader.value.Contains_string_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a string", key);
                    Console.WriteLine(reader.value.string_value);
                }
                else if (reader.value.Contains_double_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a double", key);
                    Console.WriteLine(reader.value.double_value);
                }
            }
            else
            {
                Console.WriteLine("no such object exist under the key: \"{0}\"", key);
            }
        }
        public void Del(string[] key)
        {
            MD5Utility useMD5 = new MD5Utility();
            int count = 0;
            foreach (var eachKey in key)
            {
                long md5 = useMD5.GetMD5(eachKey);
                DelResponseReader reader = Global.CloudStorage.DelKeyToRedisServer(
                    useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(eachKey));
                count += reader.count;
            }
            Console.WriteLine("The {0} keys that were removed", count);
        }
        public void Del(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            DelResponseReader reader = Global.CloudStorage.DelKeyToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
        }
        public void Type(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (reader.isFound)
            {
                if (reader.value.Contains_int_list_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a List", key);
                }
                else if (reader.value.Contains_string_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a string", key);
                }
                else if (reader.value.Contains_double_value)
                {
                    Console.WriteLine("the value under \"{0}\" is a double", key);
                }
            }
            else
            {
                Console.WriteLine("no such object exist under the key: \"{0}\"", key);
            }
        }
        public void Exists(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (reader.isFound)
            {
                Console.WriteLine("1");
            }
            else
            {
                Console.WriteLine("0");
            }
        }
        public void ReName(string key, string newKey)
        {
            if (key == newKey)
            {
                Console.WriteLine("the new key ==  the old key");
                return;
            }
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Console.WriteLine("the key dose not exist");
                return;
            }
            else
            {
                Console.WriteLine("oldkey is {0}", key);
                Console.WriteLine("newkey is {0}", newKey);
                long newMD5 = useMD5.GetMD5(newKey);
                //ValueResponseReader vReader = Global.CloudStorage.GetValueToRedisServer(
                //useMD5.GetSlaveID(newMD5, slaveCount), new StringRequestWriter(newKey));
                //if (vReader.isFound)
                //{
                //    Del(newKey);
                //}
                ReNameResponseReader rReader = Global.CloudStorage.ReNameKeyToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new ReNameRequestWriter(key, newKey));
                Element ele = new Element(rReader.element.value, newKey);
                Global.CloudStorage.SetValueToRedisServer(useMD5.GetSlaveID(newMD5, slaveCount),
                    new ElementRequestWriter(ele));
                Del(key);
            }
        }
        public void ReNameNx(string key, string newKey)
        {
            if (key == newKey)
            {
                Console.WriteLine("the source and destination names are the same");
                return;
            }
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Console.WriteLine("the key dose not exist");
                return;
            }
            else
            {
                long newMD5 = useMD5.GetMD5(newKey);
                ValueResponseReader vReader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(newMD5, slaveCount), new StringRequestWriter(newKey));
                if (!vReader.isFound)
                {
                    ReNameResponseReader rReader = Global.CloudStorage.ReNameKeyToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new ReNameRequestWriter(key, newKey));
                    Global.CloudStorage.SetValueToRedisServer(useMD5.GetSlaveID(newMD5, slaveCount),
                        new ElementRequestWriter(rReader.element));
                }
                else
                    Console.WriteLine("0");
            }
        }
        public void Append(string key, string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.TrimStart("\"".ToCharArray());
                value = value.TrimEnd("\"".ToCharArray());
                MD5Utility useMD5 = new MD5Utility();
                long md5 = useMD5.GetMD5(key);
                ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                    useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
                if (!reader.isFound)
                {
                    Set(key, value);
                }
                else
                {
                    Global.CloudStorage.AppendToRedisServer(useMD5.GetSlaveID(md5, slaveCount), new AppendRequestWriter(key, value));
                }
            }
        }
        /// <summary>
        /// TODO:
        /// </summary>
        /// <param name="keyValuePair"></param>
        public void MSet(string[] keyValuePair)
        {
            if (keyValuePair.Length % 2 != 0)
            {
                Console.WriteLine("parameter error");
                return;
            }
            else
            {
                for (int i = 0; i < keyValuePair.Length; i += 2)
                {
                    Set(keyValuePair[i], keyValuePair[i + 1]);
                }
            }
        }
        public void SetRange(string key, int offset, string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.TrimStart("\"".ToCharArray());
                value = value.TrimEnd("\"".ToCharArray());
                MD5Utility useMD5 = new MD5Utility();
                long md5 = useMD5.GetMD5(key);
                ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                    useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
                if (!reader.isFound)
                {
                    StringBuilder partString = new StringBuilder();
                    for (int i = 0; i < offset; i++)
                    {
                        partString.Append("\\x00");
                    }
                    partString.Append(value);
                    Set(key, "\"" + partString.ToString() + "\"");
                }
                else
                {
                    Global.CloudStorage.SetRangeToRedisServer(useMD5.GetSlaveID(md5, slaveCount), new SetRangeRequestWriter(offset, key, value));
                }
            }
            else
            {
                Console.WriteLine("value type is not correct");
            }
        }
        public void GetRange(string key, int start, int end)
        {

            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (reader.isFound)
            {
                if (start < 0)
                {
                    start += reader.value.string_value.Length;
                }
                if (end < 0)
                {
                    end += reader.value.string_value.Length;
                }
                if (start > end || start > reader.value.string_value.Length - 1)
                {
                    Console.WriteLine("");
                    return;
                }
                else
                {
                    GetRangeResponseReader gReader = Global.CloudStorage.GetRangeToRedisServer(useMD5.GetSlaveID(md5, slaveCount), new GetRangeRequestWriter(start, end, key));
                    Console.WriteLine(gReader.value);
                }
            }
            else
            {
                Console.WriteLine("");
                return;
            }
        }
        public void StrLen(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Console.WriteLine(0);
            }
            else
            {
                Console.WriteLine(reader.value.string_value.Length);
            }
        }
        public void GetSet(string key, string value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (reader.isFound)
            {
                Console.WriteLine(reader.value.double_value);
                Set(key, value);
            }
        }
        public void Incr(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Set(key, "0");
            }
            if (!reader.value.Contains_double_value)
            {
                Console.WriteLine("type error");
                return;
            }
            IncrResponseReader iReader = Global.CloudStorage.IncrToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new IncrRequestWriter(key));
            Console.WriteLine(iReader.value);
        }
        public void Decr(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Set(key, "0");
            }
            if (!reader.value.Contains_double_value)
            {
                Console.WriteLine("type error");
                return;
            }
            DecrResponseReader iReader = Global.CloudStorage.DecrToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new DecrRequestWriter(key));
            Console.WriteLine(iReader.value);
        }
        public void IncrBy(string key, double increment)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Set(key, "0");
            }
            if (!reader.value.Contains_double_value)
            {
                Console.WriteLine("type error");
                return;
            }
            IncrByResponseReader iReader = Global.CloudStorage.IncrByToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new IncrByRequestWriter(increment, key));
            Console.WriteLine(iReader.value);
        }
        public void DecrBy(string key, double decrement)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Set(key, "0");
            }
            if (!reader.value.Contains_double_value)
            {
                Console.WriteLine("type error");
                return;
            }
            DecrByResponseReader iReader = Global.CloudStorage.DecrByToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                    new DecrByRequestWriter(decrement, key));
            Console.WriteLine(iReader.value);
        }
        public void SetNX(string key, string value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Set(key, value);
            }
            else
                return;
        }
        public void LLen(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            ValueResponseReader reader = Global.CloudStorage.GetValueToRedisServer(
                useMD5.GetSlaveID(md5, slaveCount), new StringRequestWriter(key));
            if (!reader.isFound)
            {
                Console.WriteLine(0);
            }
            else
            {
                Console.WriteLine(reader.value.int_list_value.Count);
            }

        }
        public void LRem(string key, int count, int value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LRemResponseReader reader = Global.CloudStorage.LRemToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LRemRequestWriter(count, value, key));
            Console.WriteLine(reader.delNum);
        }
        public void RPush(string key, int[] value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            RPushResponseReader reader = Global.CloudStorage.RPushToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new RPushRequestWriter(key, value.ToList()));
            if (reader.strValue != "")
                Console.WriteLine(reader.strValue);
            else
                Console.WriteLine(reader.listLength);
        }
        public void RPop(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            RPopResponseReader reader = Global.CloudStorage.RPopToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new RPopRequestWriter(key));
            if (reader.strValue == "nil")
                Console.WriteLine(reader.strValue);
            else
                Console.WriteLine(reader.value);
        }
        public void LPop(string key)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LPopResponseReader reader = Global.CloudStorage.LPopToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LPopRequestWriter(key));
            if (reader.strValue == "nil")
                Console.WriteLine(reader.strValue);
            else
                Console.WriteLine(reader.value);
        }
        public void LSet(string key, int index, int value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LSetResponseReader reader = Global.CloudStorage.LSetToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LSetRequestWriter(index, value, key));
            Console.WriteLine(reader.message);
        }
        public void RPushX(string key, int value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            RPushXResponseReader reader = Global.CloudStorage.RPushXToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new RPushXRequestWriter(value, key));
            Console.WriteLine(reader.listLength);
        }
        public void LPush(string key, List<int> value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LPushResponseReader reader = Global.CloudStorage.LPushToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LPushRequestWriter(key, value));
            if (reader.strValue != "")
                Console.WriteLine(reader.strValue);
            else
                Console.WriteLine(reader.listLength);
        }
        public void LTrim(string key, int start, int stop)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LTrimResponseReader reader = Global.CloudStorage.LTrimToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LTrimRequestWriter(start, stop, key));
            Console.WriteLine(reader.message);
        }
        public void LIndex(string key, int index)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LIndexResponseReader reader = Global.CloudStorage.LIndexToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LIndexRequestWriter(index, key));
            if (reader.strValue != "")
                Console.WriteLine(reader.strValue);
            else
                Console.WriteLine(reader.value);
        }
        public void LPushX(string key, int value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LPushXResponseReader reader = Global.CloudStorage.LPushXToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LPushXRequestWriter(value, key));
            Console.WriteLine(reader.listLength);
        }
        public void LInsert(string key, string bORa, int pivot, int value)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LInsertResponseReader reader = Global.CloudStorage.LInsertToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LInsertRequestWriter(pivot, value, key, bORa));
            Console.WriteLine(reader.listLength);
        }
        public void LRange(string key, int start, int stop)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(key);
            LRangeResponseReader reader = Global.CloudStorage.LRangeToRedisServer(useMD5.GetSlaveID(md5, slaveCount),
                new LRangeRequestWriter(start, stop, key));
            foreach (var e in reader.valueList)
            {
                Console.WriteLine(e);
            }

        }
    }
}
