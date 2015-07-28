// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.Devices.Adc;
using Windows.Devices.Gpio;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Input
{
    /// <summary>
    /// Driver for the <see href="http://www.sainsmart.com/sainsmart-joystick-module-free-10-cables-for-arduino.html">SainSmart SS944 joystick</see>
    /// </summary>
    public sealed class SS944 : IThumbstick
    {
        #region Member Variables
        private GpioPin buttonPin;
        private ThumbstickReading currentReading;
        private bool isInitialized;
        private int minValue;
        private int maxValue = 255;
        private ObservableEvent<IThumbstick, ThumbstickReadingChangedEventArgs> readingChangedEvent;
        private ScheduledUpdater updater;
        private IAdcChannel xChannel;
        private IAdcChannel yChannel;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="SS944"/> instance.
        /// </summary>
        public SS944()
        {
            // Create updater
            updater = new ScheduledUpdater(new ScheduleOptions(reportInterval: 100));
            updater.SetUpdateAction(Update);
            updater.Starting += (s, e) => EnsureInitialized();

            // Create events
            readingChangedEvent = new ObservableEvent<IThumbstick, ThumbstickReadingChangedEventArgs>(updater);
        }
        #endregion // Constructors

        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Validate
            if (xChannel == null) { throw new MissingIoException(nameof(XChannel)); }
            if (yChannel == null) { throw new MissingIoException(nameof(YChannel)); }

            // Start with fake reading
            currentReading = new ThumbstickReading(0, 0, false);

            // Initialize button?
            if (buttonPin != null)
            {
                if (buttonPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                {
                    buttonPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                }
                else
                {
                    buttonPin.SetDriveMode(GpioPinDriveMode.Input);
                }
                buttonPin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
            }

            isInitialized = true;
        }

        private void Update()
        {
            // Read X and Y values
            float x = xChannel.ReadValue();
            float y = yChannel.ReadValue();

            // Scale
            var halfValue = maxValue - (maxValue - minValue) / 2;
            x -= halfValue;
            y -= halfValue;
            x /= halfValue;
            y /= halfValue;

            // Button
            bool pressed = false;
            if (buttonPin != null)
            {
                pressed = (buttonPin.Read() == GpioPinValue.Low);
            }

            // Update current value
            lock(currentReading)
            {
                currentReading = new ThumbstickReading(x, y, pressed);
            }

            // Notify
            readingChangedEvent.Raise(this, new ThumbstickReadingChangedEventArgs(currentReading));
        }
        #endregion // Internal Methods

        #region Public Methods
        public void Dispose()
        {
            if (buttonPin != null)
            {
                buttonPin.Dispose();
                buttonPin = null;
            }
            if (xChannel != null)
            {
                xChannel.Close();
                xChannel = null;
            }
            if (yChannel != null)
            {
                yChannel.Close();
                yChannel = null;
            }
            isInitialized = false;
        }

        /// <summary>
        /// Gets the current thumbstick reading. 
        /// </summary>
        /// <returns>
        /// A <see cref="ThumbstickReading"/>.
        /// </returns>
        public ThumbstickReading GetCurrentReading()
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
        /// Gets or sets the pin to be used for the button.
        /// </summary>
        /// <value>
        /// The pin to be used for the button.
        /// </value>
        public GpioPin ButtonPin
        {
            get
            {
                return buttonPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                buttonPin = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum value that can be read by the ADC channels.
        /// </summary>
        /// <value>
        /// The maximum value that can be read by the ADC channels. The default is 0.
        /// </value>
        [DefaultValue(255)]
        public int MaxValue
        {
            get
            {
                return maxValue;
            }
            set
            {
                maxValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum value that can be read by the ADC channels.
        /// </summary>
        /// <value>
        /// The minimum value that can be read by the ADC channels. The default is 0.
        /// </value>
        [DefaultValue(0)]
        public int MinValue
        {
            get
            {
                return minValue;
            }
            set
            {
                minValue = value;
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

        /// <summary>
        /// Gets or sets the ADC channel for the X axis.
        /// </summary>
        /// <value>
        /// The ADC channel for the X axis.
        /// </value>
        public IAdcChannel XChannel
        {
            get
            {
                return xChannel;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                xChannel = value;
            }
        }

        /// <summary>
        /// Gets or sets the ADC channel for the Y axis.
        /// </summary>
        /// <value>
        /// The ADC channel for the Y axis.
        /// </value>
        public IAdcChannel YChannel
        {
            get
            {
                return yChannel;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                yChannel = value;
            }
        }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs each time the thumbstick reports a new reading. 
        /// </summary>
        public event TypedEventHandler<IThumbstick, ThumbstickReadingChangedEventArgs> ReadingChanged
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
