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

        static public uint GetNativeColor(DisplayPixelFormat format, byte red, byte green, byte blue)
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
                    redBits = blueBits = 5;
                    greenBits = 6;
                    redMask = blueMask = 0x1F;
                    greenMask = 0x3F;
                    break;
                case DisplayPixelFormat.Rgb666:
                    redBits = greenBits = blueBits = 6;
                    redMask = greenMask = blueMask = 0x3F;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(Strings.UnknownPixelFormat, format));
            }


            // Apply mask
            red &= redMask; // 0x1F
            uint color = red;

            color <<= greenBits; // 6
            green &= greenMask; // 0x3F
            color |= green;

            color <<= blueBits;
            blue &= blueMask;
            color |= blue;
            return color;
        }

        static public uint GetNativeColor(DisplayPixelFormat format, Color color)
        {
            return GetNativeColor(format, color.R, color.G, color.B);
        }
    }
}
