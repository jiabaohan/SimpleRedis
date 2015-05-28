using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RedisCore;
using Trinity;
using Trinity.Extension;
namespace SimpleRedisProgram
{
    public class RedisServer : Trinity.Extension.RedisServerBase
    {
        public override void GetValueHandler(StringRequestReader request, ValueResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(request.key);
            if (Global.LocalStorage.Contains(md5))
            {
                //the bucket exists, very likely the key is in the bucket 
                using (var bucket = Global.LocalStorage.UseBucket(md5))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            response.isFound = true;
                            response.value = bucket.elements[i].value;
                            return;
                        }
                    }
                }
            }

            //no entry for this key exist
            response.isFound = false;
            return;
        }

        public override void SetValueHandler(ElementRequestReader request)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(request.element.key);
            if (Global.LocalStorage.Contains(md5))
            {
                //the bucket already exsit
                using (var bucket = Global.LocalStorage.UseBucket(md5))
                {
                    //different keys may end up in the same bucket(Hash Collision)
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.element.key)
                        {
                            //if already in the bucket, update the value
                            bucket.elements[i].value = request.element.value;
                            return;
                        }
                    }
                    //upon here,no entry for this key exist
                    bucket.elements.Add(request.element);
                }
            }
            else
            {
                //no bucket is ever exist, create for my own!
                Bucket newbucket = new Bucket();
                Global.LocalStorage.SaveBucket(md5, newbucket);
                using (var bucket = Global.LocalStorage.UseBucket(md5))
                {
                    bucket.elements.Add(request.element);
                }
            }
        }

        public override void DelKeyHandler(StringRequestReader request, DelResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(request.key);
            int count = 0;
            if (Global.LocalStorage.Contains(md5))
            {
                //the bucket exists, very likely the key is in the bucket 
                using (var bucket = Global.LocalStorage.UseBucket(md5))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            bucket.elements.RemoveAt(i);
                            response.count = ++count;
                            return;
                        }
                    }
                    response.count = count;
                    return;
                }
            }
            else
            {
                //no entry for this key exist
                response.count = count;
                return;
            }
        }

        public override void ReNameKeyHandler(ReNameRequestReader request, ReNameResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long md5 = useMD5.GetMD5(request.sourceKey);

            using (var bucket = Global.LocalStorage.UseBucket(md5))
            {
                if (bucket.elements.Count == 1)
                {
                    response.element = bucket.elements[0];
                }
                else
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.sourceKey)
                        {
                            response.element = bucket.elements[i];
                            break;
                        }
                    }
                }
            }
        }

        public override void AppendHandler(AppendRequestReader request)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        if (bucket.elements[i].value.string_value != "")
                        {
                            StringBuilder partString = new StringBuilder(bucket.elements[i].value.string_value);
                            partString.Append(request.value);
                            bucket.elements[i].value.string_value = partString.ToString();
                        }
                        else
                        {
                            Console.WriteLine("value type of the key is not string");
                        }
                    }
                }
            }
        }

        public override void SetRangeHandler(SetRangeRequestReader request)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);

            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        if (bucket.elements[i].value.string_value != "")
                        {
                            int valueLength = bucket.elements[i].value.string_value.Length;
                            int newValueLenth = request.value.Length;
                            if (request.offset >= valueLength)
                            {
                                StringBuilder str = new StringBuilder(bucket.elements[i].value.string_value);
                                for (int j = 0; j < request.offset - valueLength; j++)
                                {
                                    str.Append("\\x00");
                                }
                                str.Append(request.value);
                                bucket.elements[i].value.string_value = str.ToString();
                            }
                            else
                            {
                                string oldValue = bucket.elements[i].value.string_value;
                                StringBuilder str = new StringBuilder(oldValue.Substring(0, request.offset));
                                if (request.offset + newValueLenth < valueLength)
                                {
                                    str.Append(request.value);
                                    str.Append(oldValue.Substring(request.offset + newValueLenth, valueLength - newValueLenth - request.offset));
                                    bucket.elements[i].value.string_value = str.ToString();
                                }
                                else
                                {
                                    str.Append(request.value);
                                    bucket.elements[i].value.string_value = str.ToString();
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void GetRangeHandler(GetRangeRequestReader request, GetRangeResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            string value = "";
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        value = bucket.elements[i].value.string_value;
                    }
                }
            }
            if (request.end < value.Length - 1)
            {
                response.value = value.Substring(request.start, request.end - request.start + 1);
            }
            else
            {
                response.value = value.Substring(request.start);
            }
        }

        public override void IncrHandler(IncrRequestReader request, IncrResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        bucket.elements[i].value.double_value += 1;
                        response.value = bucket.elements[i].value.double_value;
                    }
                }
            }
        }

        public override void DecrHandler(DecrRequestReader request, DecrResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        bucket.elements[i].value.double_value -= 1;
                        response.value = bucket.elements[i].value.double_value;
                    }
                }
            }
        }

        public override void IncrByHandler(IncrByRequestReader request, IncrByResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        bucket.elements[i].value.double_value += request.increment;
                        response.value = bucket.elements[i].value.double_value;
                    }
                }
            }
        }

        public override void DecrByHandler(DecrByRequestReader request, DecrByResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            using (var bucket = Global.LocalStorage.UseBucket(cellId))
            {
                for (int i = 0; i < bucket.elements.Count; i++)
                {
                    if (bucket.elements[i].key == request.key)
                    {
                        bucket.elements[i].value.double_value -= request.decrement;
                        response.value = bucket.elements[i].value.double_value;
                    }
                }
            }
        }

        public override void LRemHandler(LRemRequestReader request, LRemResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.delNum = 0;
                return;
            }
            if (request.count > 0)
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    int count = 0;
                    List<int> toDel = new List<int>();
                    int index = -1;
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            index = i;
                            for (int j = 0; j < bucket.elements[i].value.int_list_value.Count; j++)
                            {
                                if (bucket.elements[i].value.int_list_value[j] == request.value)
                                {
                                    if (count != request.count)
                                    {
                                        toDel.Add(j);
                                        response.delNum = toDel.Count;
                                        count++;
                                    }
                                    else
                                    {
                                        response.delNum = toDel.Count;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (index == -1)
                    {
                        response.delNum = 0;
                        return;
                    }
                    for (int k = toDel.Count - 1; k > -1; k--)
                    {
                        bucket.elements[index].value.int_list_value.RemoveAt(toDel[k]);
                    }
                }
            }
            else if (request.count < 0)
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    int count = 0;
                    List<int> toDel = new List<int>();
                    int index = -1;
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            index = i;
                            for (int j = bucket.elements[i].value.int_list_value.Count - 1; j > -1; j--)
                            {
                                if (bucket.elements[i].value.int_list_value[j] == request.value)
                                {
                                    if (count != Math.Abs(request.count))
                                    {
                                        toDel.Add(j);
                                        response.delNum = toDel.Count;
                                        count++;
                                    }
                                    else
                                    {
                                        response.delNum = toDel.Count;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (index == -1)
                    {
                        response.delNum = 0;
                        return;
                    }
                    for (int k = 0; k < toDel.Count; k++)
                    {
                        bucket.elements[index].value.int_list_value.RemoveAt(toDel[k]);
                    }
                }
            }
            else if (request.count == 0)
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    List<int> toDel = new List<int>();
                    int index = -1;
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            index = i;
                            for (int j = 0; j < bucket.elements[i].value.int_list_value.Count; j++)
                            {
                                if (bucket.elements[i].value.int_list_value[j] == request.value)
                                {
                                    toDel.Add(j);
                                    response.delNum = toDel.Count;
                                }
                            }
                        }
                    }
                    if (index == -1)
                    {
                        response.delNum = 0;
                        return;
                    }
                    for (int k = toDel.Count - 1; k > -1; k--)
                    {
                        bucket.elements[index].value.int_list_value.RemoveAt(toDel[k]);
                    }
                }
            }

        }

        public override void RPushHandler(RPushRequestReader request, RPushResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                Element e = new Element(new Value(null, null, request.value), request.key);
                Bucket newbucket = new Bucket();
                Global.LocalStorage.SaveBucket(cellId, newbucket);
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    bucket.elements.Add(e);
                }
                response.listLength = request.value.Count;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (bucket.elements[i].value.Contains_double_value || bucket.elements[i].value.Contains_string_value)
                            {
                                response.strValue = "type error";
                                return;
                            }
                            bucket.elements[i].value.int_list_value.AddRange(request.value);
                            response.listLength = bucket.elements[i].value.int_list_value.Count;
                        }
                    }
                }
            }
        }

        public override void RPopHandler(RPopRequestReader request, RPopResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.strValue = "nil";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            response.value = bucket.elements[i].value.int_list_value[bucket.elements[i].value.int_list_value.Count - 1];
                            bucket.elements[i].value.int_list_value.RemoveAt(bucket.elements[i].value.int_list_value.Count - 1);
                            break;
                        }
                    }
                }
            }
        }

        public override void LPopHandler(LPopRequestReader request, LPopResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.strValue = "nil";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            response.value = bucket.elements[i].value.int_list_value[0];
                            bucket.elements[i].value.int_list_value.RemoveAt(0);
                            break;
                        }
                    }
                }
            }
        }

        public override void LSetHandler(LSetRequestReader request, LSetResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.message = "ERR no such key";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (request.index > bucket.elements[i].value.int_list_value.Count - 1 || request.index < -bucket.elements[i].value.int_list_value.Count)
                            {
                                response.message = "ERR index out of range";
                                return;
                            }
                            int index = 0;
                            if (request.index < 0)
                                index = request.index + bucket.elements[i].value.int_list_value.Count;
                            else
                                index = request.index;
                            bucket.elements[i].value.int_list_value[index] = request.value;
                            response.message = "OK";
                            return;
                        }
                    }
                }
            }
        }


        public override void RPushXHandler(RPushXRequestReader request, RPushXResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.listLength = 0;
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (bucket.elements[i].value.Contains_int_list_value)
                            {
                                bucket.elements[i].value.int_list_value.Add(request.value);
                            }
                            else
                            {
                                response.listLength = bucket.elements[i].value.int_list_value.Count;
                                return;
                            }
                        }
                    }
                }
            }
        }

        public override void LPushHandler(LPushRequestReader request, LPushResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            List<int> toInsert = new List<int>();
            for (int i = request.value.Count - 1; i > -1; i--)
            {
                toInsert.Add(request.value[i]);
            }
            if (!Global.LocalStorage.Contains(cellId))
            {
                Element e = new Element(new Value(null, null, toInsert), request.key);
                Bucket newbucket = new Bucket();
                Global.LocalStorage.SaveBucket(cellId, newbucket);
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    bucket.elements.Add(e);
                }
                response.listLength = request.value.Count;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (bucket.elements[i].value.Contains_double_value || bucket.elements[i].value.Contains_string_value)
                            {
                                response.strValue = "type error";
                                return;
                            }
                            toInsert.AddRange(bucket.elements[i].value.int_list_value);
                            Value e = new Value(null, null, toInsert);
                            bucket.elements[i].value = e;
                            response.listLength = bucket.elements[i].value.int_list_value.Count;
                        }
                    }
                }
            }
        }

        public override void LTrimHandler(LTrimRequestReader request, LTrimResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            int start = 0;
            int stop = 0;
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.message = "no such key";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (!bucket.elements[i].value.Contains_int_list_value)
                            {
                                response.message = "error type";
                                return;
                            }
                            else
                            {
                                if (request.start < 0)
                                {
                                    start = request.start + bucket.elements[i].value.int_list_value.Count;
                                }
                                else
                                {
                                    start = request.start;
                                }
                                if (request.stop < 0)
                                {
                                    stop = request.stop + bucket.elements[i].value.int_list_value.Count;
                                }
                                else
                                {
                                    stop = request.stop;
                                }
                                if (start > bucket.elements[i].value.int_list_value.Count - 1)
                                {
                                    bucket.elements[i].value.int_list_value.Clear();
                                    response.message = "OK";
                                }
                                else
                                {
                                    if (stop < start)
                                    {
                                        response.message = "error index";
                                    }
                                    else
                                    {
                                        if (stop > bucket.elements[i].value.int_list_value.Count - 1)
                                        {
                                            bucket.elements[i].value.int_list_value.RemoveRange(0, start);
                                        }
                                        else
                                        {
                                            bucket.elements[i].value.int_list_value.RemoveRange(stop + 1, bucket.elements[i].value.int_list_value.Count - stop - 1);
                                            bucket.elements[i].value.int_list_value.RemoveRange(0, start);
                                        }
                                        response.message = "ok";
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void LIndexHandler(LIndexRequestReader request, LIndexResponseWriter response)
        {
            int index = -1;
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.strValue = "nil";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (!bucket.elements[i].value.Contains_int_list_value)
                            {
                                response.strValue = "nil";
                                return;
                            }
                            else
                            {
                                if (request.index < 0)
                                {
                                    if (request.index < -bucket.elements[i].value.int_list_value.Count)
                                    {

                                        response.strValue = "nil";
                                        return;
                                    }
                                    else
                                        index = request.index + bucket.elements[i].value.int_list_value.Count;
                                }
                                else
                                {
                                    if (index > bucket.elements[i].value.int_list_value.Count - 1)
                                    {
                                        response.strValue = "nil";
                                        return;
                                    }
                                    else
                                    {
                                        index = request.index;
                                    }
                                }
                                response.value = bucket.elements[i].value.int_list_value[index];
                            }
                        }
                    }
                }
            }
        }

        public override void LPushXHandler(LPushXRequestReader request, LPushXResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.listLength = 0;
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (bucket.elements[i].value.Contains_int_list_value)
                            {
                                List<int> temp = new List<int> { request.value };
                                temp.AddRange(bucket.elements[i].value.int_list_value);
                                Value value = new Value(null, null, temp);
                                bucket.elements[i].value = value;
                                response.listLength = bucket.elements[i].value.int_list_value.Count;
                            }
                            else
                            {
                                response.listLength = 0;
                                return;
                            }
                        }
                    }
                }
            }
        }

        public override void LInsertHandler(LInsertRequestReader request, LInsertResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                response.listLength = "0";
                return;
            }
            else
            {
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; )
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (!bucket.elements[i].value.Contains_int_list_value)
                            {
                                response.listLength = "error type";
                                return;
                            }
                            else
                            {
                                if (bucket.elements[i].value.int_list_value.Count == 0)
                                {
                                    response.listLength = "0";
                                    return;
                                }
                                if (!bucket.elements[i].value.int_list_value.Contains(request.pivot))
                                {
                                    response.listLength = "-1";
                                    return;
                                }
                                if (request.bORa == "before")
                                {
                                    for (int j = 0; j < bucket.elements[i].value.int_list_value.Count; j++)
                                    {
                                        if (bucket.elements[i].value.int_list_value[j] == request.pivot)
                                        {

                                            bucket.elements[i].value.int_list_value.Insert(j, request.value);
                                            response.listLength = bucket.elements[i].value.int_list_value.Count.ToString();
                                            return;
                                        }
                                    }
                                }
                                if (request.bORa == "after")
                                {
                                    for (int j = 0; j < bucket.elements[i].value.int_list_value.Count; j++)
                                    {
                                        if (bucket.elements[i].value.int_list_value[j] == request.pivot)
                                        {

                                            bucket.elements[i].value.int_list_value.Insert(j + 1, request.value);
                                            response.listLength = bucket.elements[i].value.int_list_value.Count.ToString();
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void LRangeHandler(LRangeRequestReader request, LRangeResponseWriter response)
        {
            MD5Utility useMD5 = new MD5Utility();
            long cellId = useMD5.GetMD5(request.key);
            if (!Global.LocalStorage.Contains(cellId))
            {
                Console.WriteLine("error no such key");
            }
            else
            {
                int start = 0;
                int stop = 0;
                List<int> copy = new List<int>();
                using (var bucket = Global.LocalStorage.UseBucket(cellId))
                {
                    for (int i = 0; i < bucket.elements.Count; i++)
                    {
                        if (bucket.elements[i].key == request.key)
                        {
                            if (!bucket.elements[i].value.Contains_int_list_value)
                            {
                                Console.WriteLine("type error");
                            }
                            else
                            {
                                if (request.start < 0)
                                {
                                    start = request.start + bucket.elements[i].value.int_list_value.Count;
                                }
                                else
                                {
                                    start = request.start;
                                }
                                if (request.stop < 0)
                                {
                                    stop = request.stop + bucket.elements[i].value.int_list_value.Count;
                                }
                                else
                                {
                                    stop = request.stop;
                                }
                                if (stop < start)
                                {
                                    Console.WriteLine("error index");
                                    return;
                                }
                                if (start > bucket.elements[i].value.int_list_value.Count - 1)
                                {
                                    response.valueList = new List<int>();
                                    return;
                                }
                                else
                                {
                                    if (stop > bucket.elements[i].value.int_list_value.Count - 1)
                                    {
                                        stop = bucket.elements[i].value.int_list_value.Count - 1;
                                    }
                                    copy = bucket.elements[i].value.int_list_value;
                                    response.valueList = copy.GetRange(start, stop - start + 1);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
