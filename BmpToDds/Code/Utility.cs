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

        public static short ReadShort(this Stream s)
        {
            var bytes = new byte[2];
            s.Read(bytes, 0, 2);
            return BitConverter.ToInt16(bytes, 0);
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

        public static void Assert(bool condition, string printIfCondFails)
        {
            if (!condition)
            {
                Console.WriteLine(printIfCondFails);
                Console.ReadLine();
            }
        }
    }
}
