// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;

namespace Microsoft.IoT.DeviceCore.Display
{
    /// <summary>
    /// The interface for a graphical display.
    /// </summary>
    public interface IGraphicsDisplay : IDevice
    {
        #region Public Methods
        /// <summary>
        /// Clears the display.
        /// </summary>
        void Clear();

        /// <summary>
        /// Writes a pixel to display memory.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <remarks>
        /// The pixel is not displayed until <see cref="Update"/> is called.
        /// </remarks>
        void DrawPixel(int x, int y, Color color);

        /// <summary>
        /// Writes a pixel to display memory.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <remarks>
        /// The pixel is not displayed until <see cref="Update"/> is called.
        /// </remarks>
        void DrawPixel(int x, int y, byte red, byte green, byte blue);

        /// <summary>
        /// Gets a value that indicates if the specified orientation is supported by the display.
        /// </summary>
        /// <param name="orientation">
        /// The orientation to test.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified orientation is supported by the display; otherwise <c>false</c>.
        /// </returns>
        bool IsOrientationSupported(DisplayOrientations orientation);

        /// <summary>
        /// Updates the display by writing any uncommitted operations.
        /// </summary>
        void Update();
        #endregion // Public Methods


        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Update"/> should automatically be called 
        /// after drawing operations.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Update"/> should automatically be called 
        /// after drawing operations; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// This property can be set to <c>false</c> to have more fine grained control over 
        /// how many drawing operations are batched before they are sent to the display.
        /// </remarks>
        [DefaultValue(true)]
        bool AutoUpdate { get; set; }

        /// <summary>
        /// Gets or sets the orientation of the display.
        /// </summary>
        /// <value>
        /// A <see cref="DisplayOrientations"/> that specifies the orientation.
        /// </value>
        DisplayOrientations Orientation { get; set; }

        /// <summary>
        /// Gets the height of the display in pixels.
        /// </summary>
        /// <value>
        /// The height of the display in pixels.
        /// </value>
        int Height { get; }

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
        int Width { get; }
        #endregion // Public Properties
    }
}
