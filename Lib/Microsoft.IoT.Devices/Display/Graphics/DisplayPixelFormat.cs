// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// Specifies the pixel format of pixel data. Each enumeration value defines a channel 
    //  ordering, bit depth, and data type.
    /// </summary>
    public enum DisplayPixelFormat
    {
        /// <summary>
        /// The display pixel format is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Each display pixel is represented by a single bit.
        /// </summary>
        /// <remarks>
        /// Each pixel bit is packed into pages, usually 8 pixels per page.
        /// </remarks>
        OneBit = 1,

        /// <summary>
        /// The pixel format is R5G6B5 unsigned integer.
        /// </summary>
        Rgb16 = 2,
    }
}
