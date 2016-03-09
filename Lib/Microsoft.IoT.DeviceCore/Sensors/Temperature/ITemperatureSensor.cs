// Copyright (c) Microsoft. All rights reserved.
//
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore.Sensors
{
    /// <summary>
    /// The interface for a generic analog sensor.
    /// </summary>
    public interface ITemperatureSensor : IDevice
    {
        #region Public Methods
        /// <summary>
        /// Gets the current sensor reading. 
        /// </summary>
        /// <returns>
        /// An <see cref="ITemperatureReading"/>.
        /// </returns>
        ITemperatureReading GetCurrentReading();
        #endregion // Public Methods

        #region Public Events
        /// <summary>
        /// Occurs each time the analog sensor reports a new reading. 
        /// </summary>
        event TypedEventHandler<ITemperatureSensor, ITemperatureReading> ReadingChanged;
        #endregion // Public Events
    }
}
