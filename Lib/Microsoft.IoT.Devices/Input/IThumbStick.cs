// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Input
{
    /// <summary>
    /// The interface for a thubmstick that can be moved on two axis and clicked.
    /// </summary>
    public interface IThumbstick : IDevice
    {
        #region Public Methods
        /// <summary>
        /// Gets the current thumbstick reading. 
        /// </summary>
        /// <returns>
        /// A <see cref="ThumbstickReading"/>.
        /// </returns>
        ThumbstickReading GetCurrentReading();
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the current report interval for the thumbstick.
        /// </summary>
        uint ReportInterval { get; set; }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs each time the thumbstick reports a new reading. 
        /// </summary>
        event TypedEventHandler<IThumbstick, ThumbstickReadingChangedEventArgs> ReadingChanged;
        #endregion // Public Events
    }
}
