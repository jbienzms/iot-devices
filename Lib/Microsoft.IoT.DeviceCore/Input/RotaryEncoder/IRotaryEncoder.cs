// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Windows.Devices.Gpio;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore.Input
{
    public interface IRotaryEncoder : IPushButton, IDisposable
    {
        #region Public Events
        /// <summary>
        /// Occurs when the encoder is rotated.
        /// </summary>
        event TypedEventHandler<IRotaryEncoder, RotaryEncoderRotatedEventArgs> Rotated;
        #endregion // Public Events
    }
}
