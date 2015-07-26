// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices
{
    /// <summary>
    /// The interface for an IoT device that updates on a shedule.
    /// </summary>
    public interface IScheduledDevice : IDevice
    {
        /// <summary>
        /// Gets or sets the update interval for the device.
        /// </summary>
        /// <value>
        /// The current update interval for the device. 
        /// </value>
        /// <remarks>
        /// The update interval will be set to a default value that will vary 
        /// based on the devices implementation. If your app does not 
        /// want to use this default value, you should set the update interval 
        /// to a non-zero value prior to registering any event handlers.
        /// </remarks>
        uint UpdateInterval { get; set; }
    }
}
