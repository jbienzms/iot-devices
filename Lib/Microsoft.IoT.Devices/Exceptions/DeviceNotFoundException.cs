// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices
{
    // TODO: Export
    /// <summary>
    /// Thrown when a required device cannot be found.
    /// </summary>
    internal class DeviceNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="DeviceNotFoundException"/> with a device name.
        /// </summary>
        public DeviceNotFoundException(string deviceName) : base(string.Format(Strings.DeviceNotFound, deviceName)) { }
    }
}
