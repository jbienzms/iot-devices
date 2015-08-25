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

namespace Microsoft.IoT.Devices.Input
{
    public sealed class RotaryEncoder : IPushButton, IDisposable
    {
        #region Member Variables
        private PushButtonHelper buttonHelper;
        private GpioPin clockPin;
        private GpioPin directionPin;
        private bool isInitialized;
        private GpioPinValue lastDirValue;
        private ObservableEvent<RotaryEncoder, RotaryEncoderRotatedEventArgs> rotatedEvent;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PushButton"/> instance.
        /// </summary>
        public RotaryEncoder()
        {
            // Create helper
            buttonHelper = new PushButtonHelper(this);

            // Lower debounce timeout
            buttonHelper.DebounceTimeout = 10;

            // Create events
            rotatedEvent = new ObservableEvent<RotaryEncoder, RotaryEncoderRotatedEventArgs>(firstAdded: EnsureInitialized);
        }
        #endregion // Constructors


        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Make sure helper is initialized
            buttonHelper.EnsureInitialized();

            // Validate that required pins have been set
            if (clockPin == null) { throw new MissingIoException(nameof(ClockPin)); }
            if (directionPin == null) { throw new MissingIoException(nameof(DirectionPin)); }

            // Use pull resistors?
            if (buttonHelper.UsePullResistors)
            {
                clockPin.SetDriveModeWithFallback(GpioPinDriveMode.InputPullUp);
                directionPin.SetDriveModeWithFallback(GpioPinDriveMode.InputPullUp);
            }
            else
            {
                clockPin.SetDriveMode(GpioPinDriveMode.Input);
                directionPin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Set a debounce timeout to filter out bounce noise
            //clockPin.DebounceTimeout = TimeSpan.FromMilliseconds(buttonHelper.DebounceTimeout);
            //directionPin.DebounceTimeout = TimeSpan.FromMilliseconds(buttonHelper.DebounceTimeout);
            clockPin.DebounceTimeout = TimeSpan.Zero;
            directionPin.DebounceTimeout = TimeSpan.Zero;

            // Update last value
            lastDirValue = directionPin.Read();

            // Subscribe to pin events
            clockPin.ValueChanged += ClockPin_ValueChanged;

            // Consider ourselves initialized now
            isInitialized = true;
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        private void ClockPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            if (args.Edge == GpioPinEdge.RisingEdge)
            {
                var dirValue = directionPin.Read();
                if ((lastDirValue == GpioPinValue.Low) && (dirValue == GpioPinValue.High))
                {
                    rotatedEvent.Raise(this, new RotaryEncoderRotatedEventArgs(RotationDirection.Counterclockwise));
                }
                if ((lastDirValue == GpioPinValue.High) && (dirValue == GpioPinValue.Low))
                {
                    rotatedEvent.Raise(this, new RotaryEncoderRotatedEventArgs(RotationDirection.Clockwise));
                }
            }
            else
            {
                lastDirValue = directionPin.Read();
            }
        }
        #endregion // Overrides / Event Handlers

        #region Public Methods
        public void Dispose()
        {
            if (buttonHelper != null)
            {
                buttonHelper.Dispose();
                buttonHelper = null;
            }
            if (clockPin != null)
            {
                clockPin.Dispose();
                clockPin = null;
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the pin that the button is connected to.
        /// </summary>
        public GpioPin ButtonPin { get { return buttonHelper.Pin; } set { buttonHelper.Pin = value; } }

        /// <summary>
        /// Gets or sets the clock pin.
        /// </summary>
        public GpioPin ClockPin
        {
            get
            {
                return clockPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                clockPin = value;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates when the Click event occurs. 
        /// </summary>
        public ButtonClickMode ClickMode { get { return buttonHelper.ClickMode; } set { buttonHelper.ClickMode = value; } }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds that will be used to debounce the button.
        /// </summary>
        /// <value>
        /// The amount of time in milliseconds that will be used to debounce the button. The default 
        /// is 1.
        /// </value>
        [DefaultValue(10)]
        public double DebounceTimeout { get { return buttonHelper.DebounceTimeout; } set { buttonHelper.DebounceTimeout = value; } }

        /// <summary>
        /// Gets or sets the direction pin.
        /// </summary>
        public GpioPin DirectionPin
        {
            get
            {
                return directionPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                directionPin = value;
            }
        }

        /// <summary>
        /// Gets a value that indicates if the button is pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the button is pressed; otherwise false.
        /// </value>
        public bool IsPressed { get { return buttonHelper.IsPressed; } }

        /// <summary>
        /// Gets or sets the <see cref="GpioPinValue"/> that indicates the button is pressed.
        /// </summary>
        /// <value>
        /// The <see cref="GpioPinValue"/> that indicates the button is pressed. 
        /// The default is <see cref="GpioPinValue.Low"/>.
        /// </value>
        [DefaultValue(GpioPinValue.Low)]
        public GpioPinValue PressedValue { get { return buttonHelper.PressedValue; } set { buttonHelper.PressedValue = value; } }

        /// <summary>
        /// Gets or sets a value that indicates if integrated pull up or pull 
        /// down resistors should be used to help maintain the state of the pin.
        /// </summary>
        /// <value>
        /// <c>true</c> if integrated pull up or pull down resistors should; 
        /// otherwise false. The default is <c>true</c>.
        /// </value>
        [DefaultValue(true)]
        public bool UsePullResistors { get { return buttonHelper.UsePullResistors; } set { buttonHelper.UsePullResistors = value; } }
        #endregion // Public Properties


        #region Public Events
        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        public event TypedEventHandler<IPushButton, EmptyEventArgs> Click
        {
            add
            {
                return buttonHelper.ClickEvent.Add(value);
            }
            remove
            {
                buttonHelper.ClickEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is pressed.
        /// </summary>
        public event TypedEventHandler<IPushButton, EmptyEventArgs> Pressed
        {
            add
            {
                return buttonHelper.PressedEvent.Add(value);
            }
            remove
            {
                buttonHelper.PressedEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is released.
        /// </summary>
        public event TypedEventHandler<IPushButton, EmptyEventArgs> Released
        {
            add
            {
                return buttonHelper.ReleasedEvent.Add(value);
            }
            remove
            {
                buttonHelper.ReleasedEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the encoder is rotated.
        /// </summary>
        public event TypedEventHandler<RotaryEncoder, RotaryEncoderRotatedEventArgs> Rotated
        {
            add
            {
                return rotatedEvent.Add(value);
            }
            remove
            {
                rotatedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}
