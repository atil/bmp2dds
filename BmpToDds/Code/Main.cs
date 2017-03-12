using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmpToDds
{
    public class Pixel
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }

        public Pixel(int r, int g, int b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

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

                if ((width % 4 != 0) || (height % 4 != 0))
                {
                    stream.Dispose();
                    Console.WriteLine("Image dimensions are not a multiply of 4");
                    return;
                }
                var pixels = new Pixel[width, height];

                stream.Seek(54, SeekOrigin.Begin);

                var w = 0;
                var h = 0;
                for (var i = 54; i < bmpBytes.Length; i += 3)
                {
                    var b = stream.ReadByte();
                    var g = stream.ReadByte();
                    var r = stream.ReadByte();
                    var p = new Pixel(r, g, b);

                    pixels[w, h] = p;

                    w++;
                    if (w == width)
                    {
                        w = 0;
                        h++;
                    }
                }



                Console.WriteLine($"pixels {w} {h}");
                Console.ReadLine();
            }
            
        }
    }
}
