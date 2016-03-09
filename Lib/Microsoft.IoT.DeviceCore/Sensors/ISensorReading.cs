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
    /// The interface for a sensor reading.
    /// </summary>
    public interface ISensorReading
    {
        /// <summary>
        /// Gets the time when the sensor reported the reading.
        /// </summary>
        /// <value>
        /// The time when the sensor reported the reading.
        /// </value>
        DateTimeOffset Timestamp { get; }
    }
}
