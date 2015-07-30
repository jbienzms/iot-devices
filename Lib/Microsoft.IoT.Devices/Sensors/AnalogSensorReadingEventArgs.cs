// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices.Sensors
{
    public interface IAnalogSensorReadingChangedEventArgs
    {
        /// <summary>
        /// Gets the current reading of the analog sensors.
        /// </summary>
        AnalogSensorReading Reading { get; }
    }

    public sealed class AnalogSensorReadingChangedEventArgs : IAnalogSensorReadingChangedEventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="AnalogSensorReadingChangedEventArgs"/>
        /// </summary>
        /// <param name="reading">
        /// The current reading.
        /// </param>
        public AnalogSensorReadingChangedEventArgs(AnalogSensorReading reading)
        {
            if (reading == null) throw new ArgumentNullException("reading");
            this.Reading = reading;
        }

        /// <summary>
        /// Gets the current reading of the analog sensor.
        /// </summary>
        public AnalogSensorReading Reading { get; }
    }
}
