using System;
using System.IO;

namespace BmpToDds.Code
{
    // So this program either converts bmp to dds or vice versa
    // These two processes are defined in functions below
    // ... which are really basically parsing the input into a pixel matrix
    // then deserializing the matrix into the output
    public static class Program
    {
        private const string Bmp2DdsOp = "-bmp2dds";
        private const string Dds2BmpOp = "-dds2bmp";

        private static void Main(string[] args)
        {
            // To speed up development
            //if (args.Length == 0)
            //{
            //    const string bmpFileName = "../Assets/example.bmp";
            //    const string ddsFileName = "../Assets/dump.dds";
            //    //Bmp2Dds(bmpFileName, ddsFileName);
            //    //Dds2Bmp(ddsFileName, bmpFileName);
            //    return;
            //}

            // Read cmdline args and make sure they are the things we want
            if (args.Length != 3)
            {
                Console.WriteLine("Usage:BmpToDds.exe <option> <input file> <output file>");
                return;
            }
            var option = args[0];
            if (option != Bmp2DdsOp && option != Dds2BmpOp)
            {
                Console.WriteLine("Invalid option. Use either -bmp2dds or -dds2bmp");
                return;
            }

            var inputFileName = args[1];
            var inputFileInfo = new FileInfo(inputFileName);
            
            var outputFileName = args[2];
            var outputFileInfo = new FileInfo(outputFileName);

            if (option == Bmp2DdsOp)
            {
                Utility.Assert(inputFileInfo.Extension == ".bmp", "Input must be a bmp file");
                Utility.Assert(outputFileInfo.Extension == ".dds", "Output must be a dds file");
                Bmp2Dds(inputFileName, outputFileName);
            }
            else if (option == Dds2BmpOp)
            {
                Utility.Assert(inputFileInfo.Extension == ".dds", "Input must be a dds file");
                Utility.Assert(outputFileInfo.Extension == ".bmp", "Output must be a bmp file");
                Dds2Bmp(inputFileName, outputFileName);
            }
        }

        private static void Bmp2Dds(string bmpFileName, string ddsFileName)
        {
            // Read bmp
            var bmpBytes = File.ReadAllBytes(bmpFileName);
            using (var stream = new MemoryStream(bmpBytes))
            {
                // Read bmp properties
                stream.Seek(14, SeekOrigin.Begin); // Begins at 14
                var headerSize = stream.ReadInt();
                var bmpWidth = stream.ReadInt(); // TODO: Is this short?
                var bmpHeight = stream.ReadInt();
                var numOfColorPlanes = stream.ReadShort();
                var colorDepth = stream.ReadShort();

                // Discard errorneous input
                if ((bmpWidth % 4 != 0) || (bmpHeight % 4 != 0))
                {
                    stream.Dispose();
                    Console.WriteLine("Image dimensions are not a multiply of 4");
                    return;
                }

                // Check 24bpp
                if (colorDepth != 24)
                {
                    stream.Dispose();
                    Console.WriteLine("BMP color depth must be 24");
                    return;
                }

                // Skip bmp header
                stream.Seek(54, SeekOrigin.Begin);

                // Read color data from bmp
                var pixels = new Pixel[bmpWidth, bmpHeight];

                // BMP has its origin at lower-left
                // To make it top-left, we invert .... *drumroll*
                // WIDTH!
                // Don't know why, but after trial and error this produced a meaningful thing
                // It seems like BMP keeps things column-wise
                var w = bmpWidth - 1;
                var h = 0;
                for (var i = 54; i < bmpBytes.Length; i += 3)
                {
                    var b = stream.ReadByte();
                    var g = stream.ReadByte();
                    var r = stream.ReadByte();
                    var p = new Pixel(r, g, b);

                    pixels[w, h] = p;

                    h++; // Walk on height-first
                    if (h == bmpHeight)
                    {
                        h = 0;
                        w--;
                    }
                }

                // Write DDS
                using (var fs = new FileStream(ddsFileName, FileMode.OpenOrCreate))
                {
                    fs.WriteByte((byte)'D');
                    fs.WriteByte((byte)'D');
                    fs.WriteByte((byte)'S');
                    fs.WriteByte((byte)' ');

                    fs.WriteInt(124); // Header size
                    fs.WriteInt(0x0000100f); // Flags indicating what info is in the header (well...)
                    fs.WriteInt(bmpWidth); // Width
                    fs.WriteInt(bmpHeight); // Height
                    fs.WriteInt(((bmpWidth + 3) / 4) * 8); // Pitch or Linear size (yup that's the one)
                    fs.WriteInt(0); // Depth
                    fs.WriteInt(0); // Mipmap count
                    for (var i = 0; i < 11; i++) // Reserved
                    {
                        fs.WriteInt(0);
                    }

                    // Pixelformat
                    fs.WriteInt(0x00000020); // Size
                    fs.WriteInt(0x00000040); // Flags about who's in here
                    fs.WriteInt(0); // FourCC (don't know what this is)
                    fs.WriteInt(0x00000010); // RGB bit count
                    fs.WriteInt(0x0000f800); // R bitmask
                    fs.WriteInt(0x000007e0); // G bitmask
                    fs.WriteInt(0x0000001f); // B bitmask
                    fs.WriteInt(0); // Alpha bitmask
                    fs.WriteInt(0x00001000); // Caps
                    fs.WriteInt(0); // Caps2
                    fs.WriteInt(0); // Caps3
                    fs.WriteInt(0); // Caps4
                    fs.WriteInt(0); // Reserved

                    foreach (var pixel in pixels)
                    {
                        // Can't believe that all 4x4 texel business is a lie...
                        var pixelBytes = BitConverter.GetBytes(pixel.ToRgb565());
                        fs.Write(pixelBytes, 0, pixelBytes.Length);
                    }

                    // TODO: Mipmaps here? 
                    // Lol nope
                }

            }
        }

        private static void Dds2Bmp(string ddsFileName, string bmpFileName)
        {
            var ddsBytes = File.ReadAllBytes(ddsFileName);
            using (var stream = new MemoryStream(ddsBytes))
            {
                // We're only interested in width and height
                stream.Seek(12, SeekOrigin.Begin);
                var imageWidth = stream.ReadInt();
                var imageHeight = stream.ReadInt();

                stream.Seek(128, SeekOrigin.Begin); // Move to actual image data
                var pixels = new Pixel[imageWidth, imageHeight];
                var w = imageWidth - 1;
                var h = 0;
                for (var i = 128; i < ddsBytes.Length; i+=2) // Start from end of header
                {
                    // Two bytes == 1 color (R5G5B5)
                    var byte0 = stream.ReadByte();
                    var byte1 = stream.ReadByte();

                    var s = (short)(byte1 << 8 | byte0);
                    pixels[w, h] = new Pixel(s);

                    h++;
                    if (h == imageHeight)
                    {
                        h = 0;
                        w--;
                    }
                }

                // DDS reading done, write BMP
                using (var outStream = new FileStream(bmpFileName, FileMode.OpenOrCreate))
                {
                    // Write header
                    outStream.WriteByte((byte)'B');
                    outStream.WriteByte((byte)'M');
                    outStream.WriteInt(pixels.Length * 3); // Image data size
                    outStream.WriteShort(0); // Reserved
                    outStream.WriteShort(0); // Reserved
                    outStream.WriteInt(0); // Should indicate the offset to actual image data

                    outStream.WriteInt(40); // Header size
                    outStream.WriteInt(imageWidth);
                    outStream.WriteInt(imageHeight);
                    outStream.WriteShort(1); // Plane count (always 1)
                    outStream.WriteShort(24); // 24 bpp
                    outStream.WriteInt(0); // Compression method. We don't use any
                    outStream.WriteInt(0); // Image size. Docs says 0 can be used
                    outStream.WriteInt(3779); // Pixel per meter (x) (have no idea why is the number)
                    outStream.WriteInt(3779); // Pixel per meter (y)
                    outStream.WriteInt(0); // Color palette count (default is 0)
                    outStream.WriteInt(0); // Important color count (default is 0)

                    foreach (var pixel in pixels)
                    {
                        var pixelBytes = pixel.ToRgb888();
                        outStream.Write(pixelBytes, 0, pixelBytes.Length);
                    }
                }

            }
        }
    }
}
