// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices.Input
{
    /// <summary>
    /// Represents a thumbstick reading.
    /// </summary>
    public sealed class ThumbstickReading
    {
        /// <summary>
        /// Initializes a new <see cref="ThumbstickReading"/> instance.
        /// </summary>
        /// <param name="xAxis">
        /// The current value of the X axis
        /// </param>
        /// <param name="yAxis">
        /// The current value of the Y axis
        /// </param>
        /// <param name="isPressed">
        /// A value that indicates if the button is pressed
        /// </param>
        public ThumbstickReading(double xAxis, double yAxis, bool isPressed)
        {
            this.IsPressed = isPressed;
            this.XAxis = xAxis;
            this.YAxis = yAxis;
        }

        /// <summary>
        /// Gets a value that indicates if the button is pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the button is pressed; otherwise false.
        /// </value>
        public bool IsPressed { get; }

        /// <summary>
        /// Gets the current value of the X axis where the range is -1.0 to 1.0.
        /// </summary>
        /// <value>
        /// The current value of the X axis.
        /// </value>
        public double XAxis { get; }

        /// <summary>
        /// Gets the current value of the X axis where the range is -1.0 to 1.0.
        /// </summary>
        /// <value>
        /// The current value of the X axis.
        /// </value>
        public double YAxis { get; }
    }
}
