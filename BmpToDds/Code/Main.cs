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
            //    const string bmpFileName = "../Assets/example2.bmp";
            //    const string ddsFileName = "../Assets/dump2.dds";
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

                // Construct texels
                var texels = new Texel[bmpWidth / 4, bmpHeight / 4];
                for (var j = 0; j < bmpHeight; j += 4)
                {
                    for (var i = 0; i < bmpWidth; i += 4)
                    {
                        // 4x4 pixels => 1 texel
                        texels[i / 4, j / 4] = new Texel(new Pixel[]
                        {
                            pixels[i, j], pixels[i + 1, j], pixels[i + 2, j], pixels[i + 3, j],
                            pixels[i, j + 1], pixels[i + 1, j + 1], pixels[i + 2, j + 1], pixels[i + 3, j + 1],
                            pixels[i, j + 2], pixels[i + 1, j + 2], pixels[i + 2, j + 2], pixels[i + 3, j + 2],
                            pixels[i, j + 3], pixels[i + 1, j + 3], pixels[i + 2, j + 3], pixels[i + 3, j + 3],
                        });
                    }
                }

                // Write DDS
                using (var outStream = new FileStream(ddsFileName, FileMode.Create))
                {
                    outStream.WriteByte((byte)'D');
                    outStream.WriteByte((byte)'D');
                    outStream.WriteByte((byte)'S');
                    outStream.WriteByte((byte)' ');

                    outStream.WriteInt(124); // Header size
                    outStream.WriteInt(0x00001007); // Flags indicating what info is in the header (well...)
                    outStream.WriteInt(bmpWidth); // Width
                    outStream.WriteInt(bmpHeight); // Height
                    outStream.WriteInt(((bmpWidth + 3) / 4) * 8); // Pitch or Linear size (yup that's the one)
                    outStream.WriteInt(0); // Depth
                    outStream.WriteInt(0); // Mipmap count
                    for (var i = 0; i < 11; i++) // Reserved
                    {
                        outStream.WriteInt(0);
                    }

                    // Pixelformat
                    outStream.WriteInt(0x00000020); // Size
                    outStream.WriteInt(0x00000004); // Flags about who's in here
                    outStream.WriteByte((byte)'D');
                    outStream.WriteByte((byte)'X');
                    outStream.WriteByte((byte)'T');
                    outStream.WriteByte((byte)'1');
                    outStream.WriteInt(0); // RGB bit count
                    outStream.WriteInt(0); // R bitmask
                    outStream.WriteInt(0); // G bitmask
                    outStream.WriteInt(0); // B bitmask
                    outStream.WriteInt(0); // Alpha bitmask
                    outStream.WriteInt(0x00001000); // Caps
                    outStream.WriteInt(0); // Caps2
                    outStream.WriteInt(0); // Caps3
                    outStream.WriteInt(0); // Caps4
                    outStream.WriteInt(0); // Reserved

                    foreach (var texel in texels)
                    {
                        var texelBytes = texel.GetBytes();
                        outStream.Write(texelBytes, 0, texelBytes.Length);
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
                var ddsWidth = stream.ReadInt();
                var ddsHeight = stream.ReadInt();

                stream.Seek(128, SeekOrigin.Begin); // Move to actual image data
                var texels = new Texel[ddsWidth / 4, ddsHeight / 4];
                var w = ddsWidth / 4 - 1;
                var h = 0;
                for (var i = 128 /* Start from end of header */; 
                    i < ddsBytes.Length; 
                    i+=8 /* Each texel is 8 bytes long */)
                {
                    var anchor0 = stream.ReadShort();
                    var anchor1 = stream.ReadShort();
                    var colorIndices = stream.ReadInt();

                    texels[w, h] = new Texel(anchor0, anchor1, colorIndices);

                    // This is a bit confusing, but it seems to work
                    h++;
                    if (h == ddsHeight / 4)
                    {
                        h = 0;
                        w--;
                    }
                }

                // DDS reading done, write BMP
                using (var outStream = new FileStream(bmpFileName, FileMode.Create))
                {
                    // Write header
                    outStream.WriteByte((byte)'B');
                    outStream.WriteByte((byte)'M');
                    outStream.WriteInt(texels.Length * 16); // Image data size
                    outStream.WriteShort(0); // Reserved
                    outStream.WriteShort(0); // Reserved
                    outStream.WriteInt(0); // Should indicate the offset to actual image data

                    outStream.WriteInt(40); // Header size
                    outStream.WriteInt(ddsWidth);
                    outStream.WriteInt(ddsHeight);
                    outStream.WriteShort(1); // Plane count (always 1)
                    outStream.WriteShort(24); // 24 bpp
                    outStream.WriteInt(0); // Compression method. We don't use any
                    outStream.WriteInt(0); // Image size. Docs says 0 can be used
                    outStream.WriteInt(3779); // Pixel per meter (x) (have no idea why is the number)
                    outStream.WriteInt(3779); // Pixel per meter (y)
                    outStream.WriteInt(0); // Color palette count (default is 0)
                    outStream.WriteInt(0); // Important color count (default is 0)

                    // Writing pixel-by-pixel, starting from top-left
                    // Find which texel harbors the pixel we wanna write,
                    // then extract the pixel from "t.Pixels[]"
                    for (var i = 0; i < ddsWidth; i++)
                    {
                        for (var j = 0; j < ddsHeight; j++)
                        {
                            var texelX = i / 4;
                            var texelY = j / 4;
                            var pixelX = i % 4;
                            var pixelY = j % 4;

                            var pixel = texels[texelX, texelY].Pixels[pixelY * 4 + pixelX];
                            outStream.Write(pixel.ToRgb888(), 0, 3);
                        }
                    }
                }

            }
        }
    }
}
