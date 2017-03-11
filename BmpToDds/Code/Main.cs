using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmpToDds
{
    public static class Program
    {
        static int ReadIntFrom(Stream s)
        {
            var bytes = new byte[4];
            s.Read(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        static void Main(string[] args)
        {
            const string bmpFileName = "../Assets/example.bmp";

            var bmpBytes = File.ReadAllBytes(bmpFileName);

            using (var stream = new MemoryStream(bmpBytes))
            {
                stream.Seek(14, SeekOrigin.Begin);
                var headerSize = ReadIntFrom(stream);
                var width = ReadIntFrom(stream);
                var height = ReadIntFrom(stream);

                stream.Seek(54, SeekOrigin.Begin);

                var b = stream.ReadByte();

                Console.WriteLine($"b {b}");
                Console.ReadLine();
            }
            
        }
    }
}
