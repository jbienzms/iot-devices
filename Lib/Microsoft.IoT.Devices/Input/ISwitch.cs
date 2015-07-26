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
    /// The interface for an input switch.
    /// </summary>
    public interface ISwitch : IDevice
    {
        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the switch is on.
        /// </summary>
        /// <remarks>
        /// <c>true</c> if the switch is on; otherwise false.
        /// </remarks>
        bool IsOn { get; }
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs when the switch is switched.
        /// </summary>
        event TypedEventHandler<ISwitch,bool> Switched;
        #endregion // Public Events
    }
}
