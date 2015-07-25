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
    public sealed class PushButton : IPushButton, IDisposable
    {
        #region Member Variables
        private ObservableEvent<IPushButton,EmptyEventArgs> clickEvent;
        private GpioPinValue pressedValue = GpioPinValue.Low;
        private GpioPinValue releasedValue = GpioPinValue.High;
        private GpioPinValue lastValue;
        private GpioPin pin;
        private ObservableEvent<IPushButton,EmptyEventArgs> pressedEvent;
        private ObservableEvent<IPushButton,EmptyEventArgs> releasedEvent;
        private ScheduledUpdater updater;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PushButton"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        public PushButton(GpioPin pin)
        {
            // Validate
            if (pin == null) throw new ArgumentNullException("pin");

            // Store
            this.pin = pin;

            // Create updater
            updater = new ScheduledUpdater(new ScheduleOptions(reportInterval: 200));
            updater.SetUpdateAction(Update);

            // Initialize IO
            InitIO();

            // Create events
            clickEvent = new ObservableEvent<IPushButton,EmptyEventArgs>(updater);
            pressedEvent = new ObservableEvent<IPushButton,EmptyEventArgs>(updater);
            releasedEvent = new ObservableEvent<IPushButton,EmptyEventArgs>(updater);
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

        private void Update()
        {
            var currentValue = pin.Read();
            if (lastValue != currentValue)
            {
                // Update last
                lastValue = currentValue;

                if (currentValue == pressedValue)
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
        }

        public void Dispose()
        {
            if (pin != null)
            {
                updater.Dispose();
                updater = null;
                pin.Dispose();
                pin = null;
            }
        }
        #endregion // Overrides / Event Handlers


        #region Public Properties
        public ButtonClickMode ClickMode { get; set; }
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
