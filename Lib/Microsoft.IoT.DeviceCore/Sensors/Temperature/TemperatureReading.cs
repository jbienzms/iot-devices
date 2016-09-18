// Copyright (c) Microsoft. All rights reserved.
//
using System;
using UnitsNet;

namespace Microsoft.IoT.DeviceCore.Sensors
{
    /// <summary>
    /// The interface for a temperature sensor reading.
    /// </summary>
    public interface ITemperatureReading : ISensorReading
    {
        /// <summary>
        /// Gets the temperature of the reading.
        /// </summary>
        /// <value>
        /// The temperature of the reading.
        /// </value>
        Temperature Temperature { get; }
    }

    /// <summary>
    /// Represents a temperature sensor reading.
    /// </summary>
    public sealed class TemperatureReading : ITemperatureReading
    {
        /// <summary>
        /// Initializes a new <see cref="TemperatureReading"/> instance.
        /// </summary>
        /// <param name="temperature">
        /// The temperature of the reading.
        /// </param>
        /// <param name="timestamp">
        /// The time when the sensor reported the reading.
        /// </param>
        public TemperatureReading(Temperature temperature, DateTimeOffset timestamp)
        {
            this.Temperature = temperature;
            this.Timestamp = timestamp;
        }

        /// <summary>
        /// Initializes a new <see cref="TemperatureReading"/> instance.
        /// </summary>
        /// <param name="temperature">
        /// The temperature of the reading.
        /// </param>
        public TemperatureReading(Temperature temperature) : this(temperature, DateTimeOffset.Now) { }

        /// <summary>
        /// Gets the temperature of the reading.
        /// </summary>
        /// <value>
        /// The temperature of the reading.
        /// </value>
        public Temperature Temperature { get; private set; }

        /// <inheritdoc/>
        public DateTimeOffset Timestamp { get; private set; }
    }
}
