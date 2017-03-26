using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmpToDds.Code
{
    public class Texel
    {
        public readonly Pixel[] Pixels; // Exactly 16
        private readonly int _indexBits;
        private readonly int _rgb565Bits;

        public Texel(Pixel[] pxls)
        {
            Pixels = pxls;

            // Find 4 colors of palette
            var max = Pixels.Aggregate((next, curr) => next.Sum < curr.Sum ? curr : next);
            var min = Pixels.Aggregate((next, curr) => next.Sum > curr.Sum ? curr : next);
            var midLower = min + (max - min) / 3;
            var midHigher = min + (max - min) * 2 / 3;

            var palette = new Pixel[]
            {
                min, midLower, midHigher, max
            };

            // Set anchor colors
            _rgb565Bits = (min.ToRgb565() << 16) | max.ToRgb565();

            // Find palette indices for each pixel
            var indexBitArray = new BitArray(32);
            for (var i = 0; i < Pixels.Length; i++)
            {
                var minDist = int.MaxValue;
                var closestOnPalette = 0; // Index on palette

                for (var j = 0; j < palette.Length; j++)
                {
                    var dist = Math.Abs(palette[j].Sum - Pixels[i].Sum);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestOnPalette = j;
                    }
                }

                // A whole lot smarter thing could be done here
                var bit0 = false;
                var bit1 = false;
                switch (closestOnPalette)
                {
                    case 0:
                        bit0 = false;
                        bit1 = false;
                        break;
                    case 1:
                        bit0 = false;
                        bit1 = true;
                        break;
                    case 2:
                        bit0 = true;
                        bit1 = false;
                        break;
                    case 3:
                        bit0 = true;
                        bit1 = true;
                        break;
                }

                indexBitArray[2 * i] = bit0;
                indexBitArray[2 * i + 1] = bit1;
            }

            _indexBits = indexBitArray.ToInt();
        }

        public Texel(short anchor0, short anchor1, int indices)
        {
            // TODO: Transparency is ignored

            // Construct palette
            var c0 = new Pixel(anchor0);
            var c1 = new Pixel(anchor1);
            var c2 = c0 * (2f / 3) + c1 * (1f / 3);
            var c3 = c0 * (1f / 3) + c1 * (2f / 3);

            var palette = new Pixel[]
            {
                c0, c1, c2, c3
            };

            // Set pixels according to the indices
            var pixels = new List<Pixel>();
            var indexBits = new BitArray(BitConverter.GetBytes(indices));
            for (var i = 0; i < indexBits.Length; i += 2)
            {
                var b0 = indexBits[i];
                var b1 = indexBits[i + 1];

                // Doing the naive and sure way
                // Bit shifting thing should be done here but...
                var paletteIndex = 0;
                if (b0 && b1)
                {
                    paletteIndex = 0;
                }
                else if (b0 && !b1)
                {
                    paletteIndex = 1;
                }
                else if (!b0 && !b1)
                {
                    paletteIndex = 2;
                }
                else if (!b0 && !b1)
                {
                    paletteIndex = 3;
                }

                pixels.Add(palette[paletteIndex]);
            }

            Pixels = pixels.ToArray();
        }

        public byte[] GetBytes()
        {
            // Concat bytes
            var anchorBytes = BitConverter.GetBytes(_rgb565Bits);
            var indexBytes = BitConverter.GetBytes(_indexBits);
            return anchorBytes.Concat(indexBytes).ToArray();
        }

        public byte[] GetRgbBytes()
        {
            // Concat all pixels' rgb888 bytes
            var retVal = new List<byte>();
            foreach (var pixel in Pixels)
            {
                retVal = retVal.Concat(pixel.ToRgb888()).ToList();
            }

            return retVal.ToArray();
        }

    }
}
