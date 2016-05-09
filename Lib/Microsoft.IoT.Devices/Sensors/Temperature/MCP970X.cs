// Copyright (c) Microsoft. All rights reserved.
//
using Microsoft.IoT.DeviceCore;
using Microsoft.IoT.DeviceCore.Sensors;
using Microsoft.IoT.DeviceHelpers;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UnitsNet;
using Windows.Devices.Adc;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Sensors
{
    /// <summary>
    /// Devices supported based on the MCP970X chipset.
    /// </summary>
    public enum MCP970XDevice
    {
        /// <summary>
        /// The <see href="http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP9700A">MCP9700A</see>
        /// </summary>
        MCP9700A,

        /// <summary>
        /// The <see href="http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP9701A">MCP9701A</see>
        /// </summary>
        MCP9701A,

        /// <summary>
        /// The <see href="http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP9701A">MCP9701AE</see>
        /// </summary>
        MCP9701AE,
    }

    /// <summary>
    /// A driver for the MCP970X family of temperature sensors.
    /// </summary>
    /// <remarks>
    /// The original core implementation for this sensor family was contributed by Dave Glover. Dave gave permission 
    /// to incorporate code from his 
    /// <see href="https://github.com/gloveboxes/Windows-IoT-Core-Driver-Library">IoT Core Driver Library</see>. 
    /// Thank you Dave!
    /// </remarks>
    public sealed class MCP970X : ITemperatureSensor, IScheduledDevice
    {
        #region Member Variables
        private TemperatureReading currentReading;
        private bool isInitialized;
        private AnalogSensor sensor = new AnalogSensor();
        private ObservableEvent<ITemperatureSensor, ITemperatureReading> readingChangedEvent;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="MCP970X"/> instance.
        /// </summary>
        public MCP970X()
        {
            // Create events
            readingChangedEvent = new ObservableEvent<ITemperatureSensor, ITemperatureReading>(firstAdded: OnFirstAdded, lastRemoved: OnLastRemoved);
        }
        #endregion // Constructors

        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Validate
            if (sensor.AdcChannel == null) { throw new MissingIoException(nameof(AdcChannel)); }

            // Create default reading
            currentReading = new TemperatureReading(Temperature.Zero);

            // Done
            isInitialized = true;
        }

        private void Update(AnalogSensorReading reading)
        {
            // Determine starting point and average count based on a value being passed in
            double AverageRatio = (reading != null ? reading.Ratio : 0);
            int totalReads = (reading != null ? 6 : 5);

            // Calculate average
            for (int i = 0; i < totalReads; i++)
            {
                AverageRatio += sensor.GetCurrentReading().Ratio;
                Task.Delay(1).Wait();
            }
            var ratio = AverageRatio / totalReads;

            // Multiply by reference
            double milliVolts = ratio * ReferenceMilliVolts;

            // Convert to Celsius
            double celsius = ((milliVolts - ZeroDegreeOffset) / MillivoltsPerDegree) + CalibrationOffset;

            // Update current value
            lock (currentReading)
            {
                currentReading = new TemperatureReading(Temperature.FromDegreesCelsius(celsius));
            }

            // Notify
            readingChangedEvent.Raise(this, currentReading);
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        private void OnFirstAdded()
        {
            sensor.ReadingChanged += Sensor_ReadingChanged;
        }

        private void OnLastRemoved()
        {
            sensor.ReadingChanged -= Sensor_ReadingChanged;
        }

        private void Sensor_ReadingChanged(IAnalogSensor sender, AnalogSensorReadingChangedEventArgs args)
        {
            // Perform update, existing reading
            Update(args.Reading);
        }
        #endregion // Overrides / Event Handlers

        #region Public Methods
        /// <summary>
        /// Configures this instance using the parameters for the specified device.
        /// </summary>
        /// <param name="device">
        /// The device type that defines the parameters.
        /// </param>
        public void ConfigureAs(MCP970XDevice device)
        {
            switch (device)
            {
                case MCP970XDevice.MCP9700A:
                    CalibrationOffset = -2;
                    MillivoltsPerDegree = 11;
                    ZeroDegreeOffset = 530;
                    break;

                case MCP970XDevice.MCP9701A:
                    CalibrationOffset = -6;
                    MillivoltsPerDegree = 19.53;
                    ZeroDegreeOffset = 400;
                    break;

                case MCP970XDevice.MCP9701AE:
                    CalibrationOffset = -4;
                    MillivoltsPerDegree = 19.5;
                    ZeroDegreeOffset = 400;
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (sensor != null)
            {
                sensor.Dispose();
                sensor = null;
            }
        }

        /// <inheritdoc/>
        public ITemperatureReading GetCurrentReading()
        {
            // Make sure we're initialized
            EnsureInitialized();

            // Perform update, no existing reading
            Update(null);

            // Return the current reading
            return currentReading;
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the ADC channel for the sensor.
        /// </summary>
        /// <value>
        /// The ADC channel for the sensor.
        /// </value>
        public AdcChannel AdcChannel
        {
            get
            {
                return sensor.AdcChannel;
            }
            set
            {
                sensor.AdcChannel = value;
            }
        }

        /// <summary>
        /// Gets or sets the calibration offset.
        /// </summary>
        /// <value>
        /// The calibration offset. The default is 0.
        /// </value>
        [DefaultValue(0d)]
        public double CalibrationOffset { get; set; } = 0;

        /// <summary>
        /// Gets or sets the millivolts per degree.
        /// </summary>
        /// <value>
        /// The millivolts per degree. The default is 20.
        /// </value>
        [DefaultValue(20d)]
        public double MillivoltsPerDegree { get; set; } = 20;

        /// <summary>
        /// Gets or sets the reference voltage in millivolts.
        /// </summary>
        /// <value>
        /// The reference voltage in millivolts. The default is 3300.
        /// </value>
        [DefaultValue(3300)]
        public int ReferenceMilliVolts { get; set; } = 3300;

        /// <summary>
        /// Gets or sets the current report interval for the analog sensor.
        /// </summary>
        public uint ReportInterval
        {
            get
            {
                return sensor.ReportInterval;
            }

            set
            {
                sensor.ReportInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the zero degree offset.
        /// </summary>
        /// <value>
        /// The zero degree offset. The default is 400.
        /// </value>
        [DefaultValue(400d)]
        public double ZeroDegreeOffset { get; set; } = 400;
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs each time the sensor reports a new reading. 
        /// </summary>
        public event TypedEventHandler<ITemperatureSensor, ITemperatureReading> ReadingChanged
        {
            add
            {
                return readingChangedEvent.Add(value);
            }
            remove
            {
                readingChangedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}