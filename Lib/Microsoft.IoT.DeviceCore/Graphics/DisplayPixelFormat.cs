// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceCore.Display
{
    /// <summary>
    /// Specifies the pixel format of pixel data. Each enumeration value defines a channel 
    /// ordering, bit depth, and data type.
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
        /// A 12-bit pixel format with 4 Red, 4 Green and 4 Blue bits.
        /// </summary>
        Rgb444 = 2,

        /// <summary>
        /// A 16-bit pixel format with 5 Red, 6 Green and 5 Blue bits.
        /// </summary>
        Rgb565 = 5,

        /// <summary>
        /// An 18-bit pixel format with 6 Red, 6 Green and 6 Blue bits.
        /// </summary>
        Rgb666 = 6,

    }
}
