using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RedisCore
{
    public class MD5Utility
    {
        MD5 md5 = System.Security.Cryptography.MD5.Create();
        public unsafe long GetMD5(string s)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(s);
            byte[] hash = md5.ComputeHash(inputBytes);
            fixed (byte* p = hash)
            {
                return *(long*)p;
            }
        }
        public int GetSlaveID(long md5, int slave_count)
        {
            return (int)(((ulong)md5) % (uint)slave_count);
        }
    }
}
