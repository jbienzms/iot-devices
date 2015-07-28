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
    /// The interface for a graphical display.
    /// </summary>
    public interface IGraphicsDisplay
    {
        /// <summary>
        /// Gets the height of the display in pixels.
        /// </summary>
        /// <value>
        /// The height of the display in pixels.
        /// </value>
        UInt32 Height { get; }

        /// <summary>
        /// Gets the format for each pixel on the display.
        /// </summary>
        /// <value>
        /// A <seealso cref="DisplayPixelFormat"/> that describes the pixel format.
        /// </value>
        DisplayPixelFormat PixelFormat { get; }

        /// <summary>
        /// Gets the width of the display in pixels.
        /// </summary>
        /// <value>
        /// The width of the display in pixels.
        /// </value>
        UInt32 Width { get; }
    }
}
