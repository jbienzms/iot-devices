// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;

namespace Microsoft.IoT.DeviceCore.Spi
{
    /// <summary>
    /// The interface for a SPI based device.
    /// </summary>
    public interface ISpiBasedDevice : IDevice
    {
        /// <summary>
        /// Gets or sets the chip select line to use on the SPIO controller.
        /// </summary>
        /// <value>
        /// The chip select line to use on the SPIO controller. The default is 0.
        /// </value>
        int ChipSelectLine { get; set; }

        /// <summary>
        /// Gets or sets the name of the SPIO controller to use.
        /// </summary>
        /// <value>
        /// The name of the SPIO controller to use. The default is "SPI0".
        /// </value>
        string ControllerName { get; set; }
    }
}
