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
        static void Main(string[] args)
        {
            const string bmpFileName = "Assets/example.bmp";

            var bmpBytes = File.ReadAllBytes(bmpFileName);
            Console.WriteLine(bmpBytes.Length);
            Console.ReadLine();
        }
    }
}
