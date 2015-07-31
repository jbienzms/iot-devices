// Copyright (c) Microsoft. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.Devices.Adc;
using Windows.Devices.Adc;
using Windows.Devices.Gpio;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Sensors
{
    /// <summary>
    /// Driver for a generic analog sensor.
    /// </summary>
    public sealed class AnalogSensor : IAnalogSensor, IScheduledDevice
    {
        #region Member Variables
        private AdcChannel adcChannel;
        private AnalogSensorReading currentReading;
        private bool isInitialized;
        private ObservableEvent<IAnalogSensor, AnalogSensorReadingChangedEventArgs> readingChangedEvent;
        private ScheduledUpdater updater;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="AnalogSensor"/> instance.
        /// </summary>
        public AnalogSensor()
        {
            // Create updater
            updater = new ScheduledUpdater(new ScheduleOptions(reportInterval: 100));
            updater.SetUpdateAction(Update);
            updater.Starting += (s, e) => EnsureInitialized();

            // Create events
            readingChangedEvent = new ObservableEvent<IAnalogSensor, AnalogSensorReadingChangedEventArgs>(updater);
        }
        #endregion // Constructors

        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Validate
            if (adcChannel == null) { throw new MissingIoException(nameof(AdcChannel)); }

            // Create default reading
            currentReading = new AnalogSensorReading(0, 0);

            // Done
            isInitialized = true;
        }

        private void Update()
        {
            // Read X and Y values
            var val = adcChannel.ReadValue();
            var ratio = adcChannel.ReadRatio(); // TODO: Support customized value scaling

            // Update current value
            lock (currentReading)
            {
                currentReading = new AnalogSensorReading(val, ratio);
            }

            // Notify
            readingChangedEvent.Raise(this, new AnalogSensorReadingChangedEventArgs(currentReading));
        }
        #endregion // Internal Methods

        #region Public Methods
        public void Dispose()
        {
            if (updater != null)
            {
                updater.Dispose();
                updater = null;
            }

            if (adcChannel != null)
            {
                adcChannel.Dispose();
                adcChannel = null;
            }

            isInitialized = false;
        }

        /// <summary>
        /// Gets the current sensor reading. 
        /// </summary>
        /// <returns>
        /// A <see cref="AnalogSensorReading"/>.
        /// </returns>
        public AnalogSensorReading GetCurrentReading()
        {
            // Make sure we're initialized
            EnsureInitialized();

            // Manual update
            Update();

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
                return adcChannel;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                adcChannel = value;
            }
        }

        /// <summary>
        /// Gets or sets the current report interval for the thumbstick.
        /// </summary>
        public uint ReportInterval
        {
            get
            {
                return updater.UpdateInterval;
            }

            set
            {
                updater.UpdateInterval = value;
            }
        }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs each time the sensor reports a new reading. 
        /// </summary>
        public event TypedEventHandler<IAnalogSensor, AnalogSensorReadingChangedEventArgs> ReadingChanged
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
