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

namespace Windows.Devices.IoT.Input
{
    public class PushButton : ScheduledDevice, IPushButton
    {
        #region Member Variables
        private SchedulingEvent<EventArgs> clickEvent;
        private GpioPinValue pressedValue = GpioPinValue.Low;
        private GpioPinValue releasedValue = GpioPinValue.High;
        private GpioPinValue lastValue;
        private GpioPin pin;
        private SchedulingEvent<EventArgs> pressedEvent;
        private SchedulingEvent<EventArgs> releasedEvent;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PushButton"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        public PushButton(GpioPin pin) : base(new ScheduleOptions(reportInterval: 200))
        {
            // Validate
            if (pin == null) throw new ArgumentNullException("pin");

            // Store
            this.pin = pin;

            // Create events
            clickEvent = new SchedulingEvent<EventArgs>(this);
            pressedEvent = new SchedulingEvent<EventArgs>(this);
            releasedEvent = new SchedulingEvent<EventArgs>(this);

            // Initialize IO
            InitIO();
        }
        #endregion // Constructors


        #region Internal Methods
        private void InitIO()
        {
            // Default to not pressed
            lastValue = releasedValue;

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
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.RisingEdge)
            {
                Debug.WriteLine("Rising");
            }
            else
            {
                Debug.WriteLine("Falling");
            }
        }

        protected override void Update()
        {
            var currentValue = pin.Read();
            if (lastValue != currentValue)
            {
                // Update last
                lastValue = currentValue;

                if (currentValue == pressedValue)
                {
                    pressedEvent.Raise(this, EventArgs.Empty);
                    if (ClickMode == ButtonClickMode.Press)
                    {
                        clickEvent.Raise(this, EventArgs.Empty);
                    }
                }
                else
                {
                    releasedEvent.Raise(this, EventArgs.Empty);
                    if (ClickMode == ButtonClickMode.Release)
                    {
                        clickEvent.Raise(this, EventArgs.Empty);
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            pin.Dispose();
        }
        #endregion // Overrides / Event Handlers


        #region Public Properties
        public ButtonClickMode ClickMode { get; set; }
        #endregion // Public Properties


        #region Public Events
        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        public event EventHandler<EventArgs> Click
        {
            add
            {
                clickEvent.Add(value);
            }
            remove
            {
                clickEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is pressed.
        /// </summary>
        public event EventHandler<EventArgs> Pressed
        {
            add
            {
                pressedEvent.Add(value);
            }
            remove
            {
                pressedEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is released.
        /// </summary>
        public event EventHandler<EventArgs> Released
        {
            add
            {
                releasedEvent.Add(value);
            }
            remove
            {
                releasedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}
