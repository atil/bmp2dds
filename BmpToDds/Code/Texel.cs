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

        public byte[] GetBytes()
        {
            // Concat bytes
            var anchorBytes = BitConverter.GetBytes(_rgb565Bits);
            var indexBytes = BitConverter.GetBytes(_indexBits);
            return anchorBytes.Concat(indexBytes).ToArray();
        }
    }
}
