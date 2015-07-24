// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.IoT.Input
{
    /// <summary>
    /// Specifies when the Click event should be raised for a button.
    /// </summary>
    public enum ButtonClickMode
    {
        /// <summary>
        /// Specifies that the Click event should be raised when the 
        /// input device is pressed and released.
        /// </summary>
        Release,

        /// <summary>
        /// Specifies that the Click event should be raised when the 
        /// input device is pressed. 
        /// </summary>
        Press
    }

    /// <summary>
    /// The interface for a basic push button.
    /// </summary>
    public interface IPushButton
    {
        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates when the Click event occurs. 
        /// </summary>
        ButtonClickMode ClickMode { get; set; }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs when the button is clicked.
        /// </summary>
        event TypedEventHandler<PushButton, EventArgs> Click;

        /// <summary>
        /// Occurs when the button is pressed.
        /// </summary>
        event TypedEventHandler<PushButton, EventArgs> Pressed;

        /// <summary>
        /// Occurs when the button is released.
        /// </summary>
        event TypedEventHandler<PushButton, EventArgs> Released;
        #endregion // Public Events
    }
}
