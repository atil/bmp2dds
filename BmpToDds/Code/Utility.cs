using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmpToDds.Code
{
    public static class Utility
    {
        public static int ReadInt(this Stream s)
        {
            var bytes = new byte[4];
            s.Read(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static void WriteInt(this Stream s, int arg)
        {
            s.Write(BitConverter.GetBytes(arg), 0 , 4);
        }

        public static int ToInt(this BitArray bitArray)
        {
            var result = new int[1];
            bitArray.CopyTo(result, 0);
            return result[0];
        }
    }
}
