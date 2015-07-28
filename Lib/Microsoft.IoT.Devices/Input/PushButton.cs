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
    public sealed class PushButton : IPushButton, IDisposable
    {
        #region Member Variables
        private ObservableEvent<IPushButton,EmptyEventArgs> clickEvent;
        private double debounceTimeout = 50;
        private bool isInitialized;
        private bool isPressed;
        private GpioPin pin;
        private GpioPinValue pressedValue = GpioPinValue.Low;
        private ObservableEvent<IPushButton,EmptyEventArgs> pressedEvent;
        private ObservableEvent<IPushButton,EmptyEventArgs> releasedEvent;
        private bool usePullResistors = true;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PushButton"/> instance.
        /// </summary>
        public PushButton()
        {
            // Create events
            clickEvent = new ObservableEvent<IPushButton, EmptyEventArgs>(firstAdded: EnsureInitialized);
            pressedEvent = new ObservableEvent<IPushButton, EmptyEventArgs>(firstAdded: EnsureInitialized);
            releasedEvent = new ObservableEvent<IPushButton, EmptyEventArgs>(firstAdded: EnsureInitialized);
        }
        #endregion // Constructors


        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Validate that the pin has been set
            if (pin == null) { throw new MissingIoException(nameof(Pin)); }

            // Check if input pull-up resistors are supported 
            if (pin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
            {
                pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            }
            else
            {
                pin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Set a debounce timeout to filter out switch bounce noise from a button press 
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(debounceTimeout);

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
            if ((pressedValue == GpioPinValue.High) && (edge == GpioPinEdge.RisingEdge))
            {
                isPressed = true;
            }
            else if ((pressedValue == GpioPinValue.Low) && (edge == GpioPinEdge.FallingEdge))
            {
                isPressed = true;
            }
            else
            {
                isPressed = false;
            }

            // Notify
            if (isPressed)
            {
                pressedEvent.Raise(this, EmptyEventArgs.Instance);
                if (ClickMode == ButtonClickMode.Press)
                {
                    clickEvent.Raise(this, EmptyEventArgs.Instance);
                }
            }
            else
            {
                releasedEvent.Raise(this, EmptyEventArgs.Instance);
                if (ClickMode == ButtonClickMode.Release)
                {
                    clickEvent.Raise(this, EmptyEventArgs.Instance);
                }
            }
        }

        public void Dispose()
        {
            if (pin != null)
            {
                pin.Dispose();
                pin = null;
            }
        }
        #endregion // Overrides / Event Handlers


        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates when the Click event occurs. 
        /// </summary>
        public ButtonClickMode ClickMode { get; set; }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds that will be used to debounce the pushbutton.
        /// </summary>
        /// <value>
        /// The amount of time in milliseconds that will be used to debounce the pushbutton. The default 
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
        /// Gets a value that indicates if the button is pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the button is pressed; otherwise false.
        /// </value>
        public bool IsPressed { get { return isPressed; } }

        /// <summary>
        /// Gets or sets the pin that the button is connected to.
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
        /// Gets or sets the <see cref="GpioPinValue"/> that indicates the button is pressed.
        /// </summary>
        /// <value>
        /// The <see cref="GpioPinValue"/> that indicates the button is pressed. 
        /// The default is <see cref="GpioPinValue.Low"/>.
        /// </value>
        [DefaultValue(GpioPinValue.Low)]
        public GpioPinValue PressedValue { get { return pressedValue; } set { pressedValue = value; } }

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
        /// Occurs when the button is clicked.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Click
        {
            add
            {
                return clickEvent.Add(value);
            }
            remove
            {
                clickEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is pressed.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Pressed
        {
            add
            {
                return pressedEvent.Add(value);
            }
            remove
            {
                pressedEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is released.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Released
        {
            add
            {
                return releasedEvent.Add(value);
            }
            remove
            {
                releasedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}
