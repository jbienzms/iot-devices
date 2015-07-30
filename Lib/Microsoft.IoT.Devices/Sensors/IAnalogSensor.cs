// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Sensors
{
    /// <summary>
    /// The interface for a generic analog sensor.
    /// </summary>
    public interface IAnalogSensor : IDevice
    {
        #region Public Methods
        /// <summary>
        /// Gets the current sensor reading. 
        /// </summary>
        /// <returns>
        /// An <see cref="AnalogSensorReading"/>.
        /// </returns>
        AnalogSensorReading GetCurrentReading();
        #endregion // Public Methods

        #region Public Events
        /// <summary>
        /// Occurs each time the analog sensor reports a new reading. 
        /// </summary>
        event TypedEventHandler<IAnalogSensor, AnalogSensorReadingChangedEventArgs> ReadingChanged;
        #endregion // Public Events
    }
}
