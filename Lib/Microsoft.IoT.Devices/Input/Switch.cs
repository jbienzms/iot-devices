// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Microsoft.IoT.DeviceCore;
using Microsoft.IoT.DeviceCore.Input;
using Microsoft.IoT.DeviceHelpers;

namespace Microsoft.IoT.Devices.Input
{
    /// <summary>
    /// An implementation of the <see cref="ISwitch"/> interface that uses a single GPIO pin.
    /// </summary>
    public sealed class Switch : ISwitch, IDisposable
    {
        #region Member Variables
        private double debounceTimeout = 50;
        private bool isInitialized;
        private bool isOn = false;
        private GpioPinValue onValue = GpioPinValue.Low;
        private GpioPin pin;
        private ObservableEvent<ISwitch, bool> switchedEvent;
        private bool usePullResistors = true;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="Switch"/> instance.
        /// </summary>
        public Switch()
        {
            // Create events
            switchedEvent = new ObservableEvent<ISwitch, bool>(firstAdded: EnsureInitialized);
        }
        #endregion // Constructors


        #region Internal Methods
        private void EnsureInitialized()
        {
            // If we're already initialized, ignore
            if (isInitialized) { return; }

            // Validate that the pin has been set
            if (pin == null) { throw new MissingIoException(nameof(Pin)); }

            bool driveSet = false;
            // Use pull resistors?
            if (usePullResistors)
            {
                // Check if resistors are supported 
                if (onValue == GpioPinValue.High)
                {
                    pin.SetDriveModeWithFallback(GpioPinDriveMode.InputPullDown);
                    driveSet = true;
                }
                else
                {
                    pin.SetDriveModeWithFallback(GpioPinDriveMode.InputPullUp);
                    driveSet = true;
                }
            }

            if (!driveSet)
            {
                pin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Set a debounce timeout to filter out switch bounce noise
            if (debounceTimeout > 0)
            {
                pin.DebounceTimeout = TimeSpan.FromMilliseconds(debounceTimeout);
            }

            // Determine state
            IsOn = (pin.Read() == onValue);

            // Subscribe to pin events
            pin.ValueChanged += Pin_ValueChanged;

            // Consider ourselves initialized now
            isInitialized = true;
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            var edge = e.Edge;
            if ((onValue == GpioPinValue.High) && (edge == GpioPinEdge.RisingEdge))
            {
                IsOn = true;
            }
            else if ((onValue == GpioPinValue.Low) && (edge == GpioPinEdge.FallingEdge))
            {
                IsOn = true;
            }
            else
            {
                IsOn = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            isInitialized = false;
            if (pin != null)
            {
                pin.ValueChanged -= Pin_ValueChanged;
                pin.Dispose();
                pin = null;
            }
        }
        #endregion // Overrides / Event Handlers

        #region Public Properties
        /// <summary>
        /// Gets or sets the amount of time in milliseconds that will be used to debounce the switch.
        /// </summary>
        /// <value>
        /// The amount of time in milliseconds that will be used to debounce the switch. The default 
        /// is 50.
        /// </value>
        [DefaultValue(50)]
        public double DebounceTimeout
        {
            get
            {
                return debounceTimeout;
            }
            set
            {
                if (value != debounceTimeout)
                {
                    debounceTimeout = value;
                    if (pin != null)
                    {
                        pin.DebounceTimeout = TimeSpan.FromMilliseconds(debounceTimeout);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates if the switch is on.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if the switch is on; otherwise false.
        /// </remarks>
        public bool IsOn
        {
            get
            {
                return isOn;
            }
            private set
            {
                // Ensure changing
                if (value == isOn) { return; }

                // Update
                isOn = value;

                // Notify
                switchedEvent.Raise(this, isOn);
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="GpioPinValue"/> that indicates the switch is on.
        /// </summary>
        /// <value>
        /// The <see cref="GpioPinValue"/> that indicates the switch is on. 
        /// The default is <see cref="GpioPinValue.Low"/>.
        /// </value>
        [DefaultValue(GpioPinValue.Low)]
        public GpioPinValue OnValue { get { return onValue; } set { onValue = value; } }

        /// <summary>
        /// Gets or sets the pin that the switch is connected to.
        /// </summary>
        public GpioPin Pin
        {
            get
            {
                return pin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                pin = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates if integrated pull up or pull 
        /// down resistors should be used to help maintain the state of the pin.
        /// </summary>
        /// <value>
        /// <c>true</c> if integrated pull up or pull down resistors should; 
        /// otherwise false. The default is <c>true</c>.
        /// </value>
        [DefaultValue(true)]
        public bool UsePullResistors
        {
            get
            {
                return usePullResistors;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                usePullResistors = value;
            }
        }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs when the switch is switched.
        /// </summary>
        public event TypedEventHandler<ISwitch, bool> Switched
        {
            add
            {
                return switchedEvent.Add(value);
            }
            remove
            {
                switchedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}
