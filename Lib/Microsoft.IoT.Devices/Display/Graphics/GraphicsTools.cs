// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// A helper class for working with graphical displays.
    /// </summary>
    static public class GraphicsTools
    {
        /// <summary>
        /// Gets the total bits per pixels for the specified format.
        /// </summary>
        /// <param name="format">
        /// The format used to obtain the bit count.
        /// </param>
        /// <returns>
        /// The number of bits per pixel.
        /// </returns>
        static public int GetBitsPerPixel(DisplayPixelFormat format)
        {
            switch (format)
            {
                case DisplayPixelFormat.OneBit:
                    return 1;
                case DisplayPixelFormat.Rgb444:
                    return 12;
                case DisplayPixelFormat.Rgb565:
                    return 16;
                case DisplayPixelFormat.Rgb666:
                    return 18;
                default:
                    throw new InvalidOperationException(string.Format(Strings.UnknownPixelFormat, format));
            }
        }

        static public ushort GetNativeColor(DisplayPixelFormat format, byte red, byte green, byte blue)
        {
            int redBits, greenBits, blueBits; // bits per color
            byte redMask, greenMask, blueMask; // mask for shifting
            switch (format)
            {
                case DisplayPixelFormat.OneBit:
                    // Just return 1 if any color is greater than black
                    if ((red > 0) || (green > 0) || (blue > 0))
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                case DisplayPixelFormat.Rgb444:
                    redBits = greenBits = blueBits = 4;
                    redMask = greenMask = blueMask = 0x0F;
                    break;
                case DisplayPixelFormat.Rgb565:
                    redBits = blueBits = 5;                 // Red and Blue have 5 bits
                    greenBits = 6;                          // Green has 6 bits
                    redMask = blueMask = 0x1F;              // 0001:1111
                    greenMask = 0x3F;                       // 0011:1111
                    break;
                case DisplayPixelFormat.Rgb666:
                    redBits = greenBits = blueBits = 6;
                    redMask = greenMask = blueMask = 0x3F;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(Strings.UnknownPixelFormat, format));
            }

            int x = 0;
            if ((red > 0) && (green > 0) && (blue > 0))
            {
                x++;
            }

            // Apply mask // TODO: Faster algorithm?
            red >>= (8 - redBits);
            green >>= (8 - greenBits);
            blue >>= (8 - blueBits);

            // Shift and build
            ushort color = blue;
            color <<= greenBits;
            color |= green;
            color <<= redBits;
            color |= red;
            return color;
        }

        static public ushort GetNativeColor(DisplayPixelFormat format, Color color)
        {
            return GetNativeColor(format, color.R, color.G, color.B);
        }
    }
}
