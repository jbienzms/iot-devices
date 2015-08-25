// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Microsoft.IoT.Devices
{
    static public class GpioExtensions
    {
        /// <summary>
        /// Sets a drive mode with a fallback mode if the requested mode is not supported.
        /// </summary>
        /// <param name="pin">
        /// The pin to set.
        /// </param>
        /// <param name="driveMode">
        /// The requested drive mode.
        /// </param>
        /// <param name="fallbackMode">
        /// The fallback drive mode.
        /// </param>
        static public void SetDriveModeWithFallback(this GpioPin pin, GpioPinDriveMode driveMode, GpioPinDriveMode fallbackMode)
        {
            if (pin.IsDriveModeSupported(driveMode))
            {
                pin.SetDriveMode(driveMode);
            }
            else
            {
                pin.SetDriveMode(fallbackMode);
            }
        }

        /// <summary>
        /// Sets a drive mode with automatic fallback if the requested mode is not supported.
        /// </summary>
        /// <param name="pin">
        /// The pin to set.
        /// </param>
        /// <param name="driveMode">
        /// The requested drive mode.
        /// </param>
        static public void SetDriveModeWithFallback(this GpioPin pin, GpioPinDriveMode driveMode)
        {
            switch (driveMode)
            {
                case GpioPinDriveMode.InputPullDown:
                case GpioPinDriveMode.InputPullUp:
                    SetDriveModeWithFallback(pin, driveMode, GpioPinDriveMode.Input);
                        break;
                case GpioPinDriveMode.OutputOpenDrain:
                case GpioPinDriveMode.OutputOpenDrainPullUp:
                    SetDriveModeWithFallback(pin, driveMode, GpioPinDriveMode.Output);
                    break;
                case GpioPinDriveMode.OutputOpenSourcePullDown:
                    SetDriveModeWithFallback(pin, driveMode, GpioPinDriveMode.OutputOpenSource);
                    break;
                default:
                    pin.SetDriveMode(driveMode);
                    break;
            }
        }
    }
}
