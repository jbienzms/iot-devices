// Copyright (c) Microsoft. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceCore.Sensors
{
    /// <summary>
    /// Represents an analog sensor reading.
    /// </summary>
    public sealed class AnalogSensorReading
    {
        /// <summary>
        /// Initializes a new <see cref="AnalogSensorReading"/> instance.
        /// </summary>
        /// <param name="val">
        /// The value of the reading.
        /// </param>
        /// <param name="ratio">
        /// The ratio of the reading as a percentage of the range.
        /// </param>
        public AnalogSensorReading(int val, double ratio)
        {
            this.Value = val;
            this.Ratio = ratio;
        }

        /// <summary>
        /// Gets the value of the reading.
        /// </summary>
        /// <value>
        /// The value of the reading.
        /// </value>
        public double Value { get; }

        /// <summary>
        /// Gets the ratio of the reading as a percentage of the range.
        /// </summary>
        /// <value>
        /// The ratio of the reading.
        /// </value>
        public double Ratio { get; }
    }
}
