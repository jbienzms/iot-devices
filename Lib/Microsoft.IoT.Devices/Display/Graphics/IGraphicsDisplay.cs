// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// The interface for a graphical display.
    /// </summary>
    public interface IGraphicsDisplay
    {
        #region Public Methods
        /// <summary>
        /// Clears the display.
        /// </summary>
        IAsyncAction ClearAsync();

        /// <summary>
        /// Writes a pixel to display memory.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <returns>
        /// An <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        /// <remarks>
        /// The pixel is not displayed until <see cref="UpdateAsync"/> is called.
        /// </remarks>
        IAsyncAction WritePixelAsync(int x, int y, Color color);

        /// <summary>
        /// Updates the display by writing any uncomitted operations.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        IAsyncAction UpdateAsync();
        #endregion // Public Methods


        #region Public Properties
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
