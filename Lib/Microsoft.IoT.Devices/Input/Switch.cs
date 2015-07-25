// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Input
{
    public sealed class Switch : ISwitch, IDisposable
    {
        #region Member Variables
        private bool isOn = false;
        private GpioPinValue onValue = GpioPinValue.High;
        private GpioPin pin;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="Switch"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        /// <param name="onValue">
        /// </param>
        /// <param name="usePull">
        /// </param>
        /// <param name="debounceTime">
        /// </param>
        public Switch(GpioPin pin, GpioPinValue onValue, bool usePull, double debounceTime)
        {
            // Validate
            if (pin == null) throw new ArgumentNullException("pin");

            // Store
            this.pin = pin;
            this.onValue = onValue;

            // Initialize IO
            InitIO(usePull, debounceTime);
        }

        /// <summary>
        /// Initializes a new <see cref="Switch"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        /// <param name="onValue">
        /// </param>
        public Switch(GpioPin pin, GpioPinValue onValue) : this(pin, onValue, true, 50) { }

        /// <summary>
        /// Initializes a new <see cref="Switch"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        public Switch(GpioPin pin) : this(pin, GpioPinValue.High, true, 50){}

        #endregion // Constructors


        #region Internal Methods
        private void InitIO(bool usePull, double debounceTime)
        {
            bool driveSet = false;
            // Use pull resistors?
            if (usePull)
            {
                // Check if resistors are supported 
                if (onValue == GpioPinValue.High)
                {
                    if (pin.IsDriveModeSupported(GpioPinDriveMode.InputPullDown))
                    {
                        pin.SetDriveMode(GpioPinDriveMode.InputPullDown);
                        driveSet = true;
                    }
                }
                else
                {
                    if (pin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                    {
                        pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
                        driveSet = true;
                    }
                }
            }

            if (!driveSet)
            {
                pin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Set a debounce timeout to filter out switch bounce noise
            if (debounceTime > 0)
            {
                pin.DebounceTimeout = TimeSpan.FromMilliseconds(debounceTime);
            }

            // Determine statate
            IsOn = (pin.Read() == onValue);

            // Subscribe to pin events
            pin.ValueChanged += Pin_ValueChanged;
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

        public void Dispose()
        {
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
            set
            {
                // Ensure changing
                if (value == isOn) { return; }

                // Update
                isOn = value;

                // Notify
                if (Switched != null)
                {
                    Switched(this, isOn);
                }
            }
        }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs when the switch is switched.
        /// </summary>
        public event EventHandler<bool> Switched;
        #endregion // Public Events
    }
}
