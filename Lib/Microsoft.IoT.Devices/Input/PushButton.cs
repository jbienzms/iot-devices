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
        private PushButtonHelper helper;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PushButton"/> instance.
        /// </summary>
        public PushButton()
        {
            // Create helper
            helper = new PushButtonHelper(this);
        }
        #endregion // Constructors

        #region Public Methods
        public void Dispose()
        {
            if (helper != null)
            {
                helper.Dispose();
                helper = null;
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates when the Click event occurs. 
        /// </summary>
        public ButtonClickMode ClickMode { get { return helper.ClickMode; } set { helper.ClickMode = value; } }

        /// <summary>
        /// Gets or sets the amount of time in milliseconds that will be used to debounce the pushbutton.
        /// </summary>
        /// <value>
        /// The amount of time in milliseconds that will be used to debounce the pushbutton. The default 
        /// is 50.
        /// </value>
        [DefaultValue(50)]
        public double DebounceTimeout { get { return helper.DebounceTimeout; } set { helper.DebounceTimeout = value; } }

        /// <summary>
        /// Gets a value that indicates if the button is pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if the button is pressed; otherwise false.
        /// </value>
        public bool IsPressed { get { return helper.IsPressed; } }

        /// <summary>
        /// Gets or sets the pin that the button is connected to.
        /// </summary>
        public GpioPin Pin { get { return helper.Pin; } set { helper.Pin = value; } }

        /// <summary>
        /// Gets or sets the <see cref="GpioPinValue"/> that indicates the button is pressed.
        /// </summary>
        /// <value>
        /// The <see cref="GpioPinValue"/> that indicates the button is pressed. 
        /// The default is <see cref="GpioPinValue.Low"/>.
        /// </value>
        [DefaultValue(GpioPinValue.Low)]
        public GpioPinValue PressedValue { get { return helper.PressedValue; } set { helper.PressedValue = value; } }

        /// <summary>
        /// Gets or sets a value that indicates if integrated pull up or pull 
        /// down resistors should be used to help maintain the state of the pin.
        /// </summary>
        /// <value>
        /// <c>true</c> if integrated pull up or pull down resistors should; 
        /// otherwise false. The default is <c>true</c>.
        /// </value>
        [DefaultValue(true)]
        public bool UsePullResistors { get { return helper.UsePullResistors; } set { helper.UsePullResistors = value; } }
        #endregion // Public Properties


        #region Public Events
        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Click
        {
            add
            {
                return helper.ClickEvent.Add(value);
            }
            remove
            {
                helper.ClickEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is pressed.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Pressed
        {
            add
            {
                return helper.PressedEvent.Add(value);
            }
            remove
            {
                helper.PressedEvent.Remove(value);
            }
        }

        /// <summary>
        /// Occurs when the button is released.
        /// </summary>
        public event TypedEventHandler<IPushButton,EmptyEventArgs> Released
        {
            add
            {
                return helper.ReleasedEvent.Add(value);
            }
            remove
            {
                helper.ReleasedEvent.Remove(value);
            }
        }
        #endregion // Public Events
    }
}
