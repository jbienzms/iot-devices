// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using Windows.Devices.Adc;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Gpio;
using Microsoft.IoT.DeviceCore.Adc;
using Microsoft.IoT.DeviceHelpers;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.ti.com/product/adc0832-n">ADC0832</see> 
    /// 8-bit A/D converter.
    /// </summary>
    /// <remarks>
    /// This class is not designed to be used directly to read data. Instead, once it 
    /// has been created and configured it can be passed to the 
    /// <see cref="AdcController.GetControllersAsync">GetControllersAsync</see> 
    /// method of the <see cref="AdcController"/> class or it can be added to the 
    /// <see cref="AdcProviderManager.Providers">Providers</see> collection in a 
    /// <see cref="AdcProviderManager"/>.
    /// </remarks>
    public sealed class ADC0832 : IAdcControllerProvider, IAdcProvider, IDisposable
    {
        #region Constants
        private const int CHANNEL_COUNT = 2;
        #endregion // Constants

        #region Member Variables
        private ProviderAdcChannelMode channelMode = ProviderAdcChannelMode.SingleEnded;
        private GpioPin chipSelectPin;
        private GpioPin clockPin;
        private GpioPin dataPin;
        private bool isInitialized;
        #endregion // Member Variables

        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }
            if (chipSelectPin == null) { throw new MissingIoException(nameof(ChipSelectPin)); }
            if (clockPin == null) { throw new MissingIoException(nameof(ClockPin)); }
            if (dataPin == null) { throw new MissingIoException(nameof(DataPin)); }
            chipSelectPin.SetDriveMode(GpioPinDriveMode.Output);
            clockPin.SetDriveMode(GpioPinDriveMode.Output);
        }
        #endregion // Internal Methods

        #region IAdcControllerProvider Interface
        void IAdcControllerProvider.AcquireChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > CHANNEL_COUNT)) throw new ArgumentOutOfRangeException("channel");

            // This devices does not operate in exclusive mode, so we'll just ignore
        }

        bool IAdcControllerProvider.IsChannelModeSupported(ProviderAdcChannelMode channelMode)
        {
            // All modes currently supported, but in case another mode is added later.
            switch (channelMode)
            {
                case ProviderAdcChannelMode.Differential:
                case ProviderAdcChannelMode.SingleEnded:
                    return true;
                default:
                    return false;
            }
        }

        int IAdcControllerProvider.ReadValue(int channelNumber)
        {
            // Validate
            if ((channelNumber < 0) || (channelNumber > CHANNEL_COUNT)) throw new ArgumentOutOfRangeException("channelNumber");

            // Make sure we're initialized
            EnsureInitialized();

            // The code below is based on the ADC0832 spec sheet by TI
            // http://www.ti.com/lit/ds/symlink/adc0831-n.pdf

            // NOTE: All delays have been commented out as the original c code 
            // used delays in the 2 microsecond range, a granularity which is 
            // not currently supported by Win10 on IoT. The granularity of 
            // the managed WinRT code scheduler seems to be wide enough to 
            // meet the minimum times between pin writes.
            // Jared Bienz - 2015/07/31

            byte i;
            int dat1 = 0, dat2 = 0;

            // Chip Select
            chipSelectPin.Write(GpioPinValue.Low);

            // According to the spec sheet:
            // "The start bit is the first logic “1” that appears 
            // on this line (all leading zeros are ignored). 
            //
            // Following the start bit the converter expects the next 
            // 2 to 4 bits to be the MUX assignment word.

            // Clock Pulse and Start Bit
            clockPin.Write(GpioPinValue.Low);
            dataPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
            clockPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds

            // Clock Pulse and Set MUX Mode - 0 = Differential, 1 = Single-Ended
            clockPin.Write(GpioPinValue.Low);
            if (channelMode == ProviderAdcChannelMode.SingleEnded)
            {
                dataPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
            }
            else
            {
                dataPin.Write(GpioPinValue.Low); // await Task.Delay(1); // 2 microseconds
            }
            clockPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds


            // Clock Pulse and Set Channel Number
            clockPin.Write(GpioPinValue.Low);
            if (channelNumber == 0)
            {
                dataPin.Write(GpioPinValue.Low); //CH0 0 // await Task.Delay(1); // 2 microseconds
            }
            else
            {
                dataPin.Write(GpioPinValue.High); //CH1 1 // await Task.Delay(1); // 2 microseconds
            }
            clockPin.Write(GpioPinValue.High);


            // Clock Pulse and Enter Read Mode
            dataPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
            clockPin.Write(GpioPinValue.Low);
            dataPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
            dataPin.SetDriveMode(GpioPinDriveMode.Input);

            // Read 8 bits and convert to bytes
            for (i = 0; i < 8; i++)
            {
                clockPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.Low); // await Task.Delay(1); // 2 microseconds
                dat1 = (dat1 << 1) | (dataPin.Read() == GpioPinValue.High ? 1 : 0);
            }

            for (i = 0; i < 8; i++)
            {
                dat2 = dat2 | ((dataPin.Read() == GpioPinValue.High ? 1 : 0) << i);
                clockPin.Write(GpioPinValue.High); // await Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.Low); // await Task.Delay(1); // 2 microseconds
            }

            // Back to output mode
            chipSelectPin.Write(GpioPinValue.High);
            dataPin.SetDriveMode(GpioPinDriveMode.Output);

            // Verify and return
            return (dat1 == dat2) ? dat1 : 0;
        }

        void IAdcControllerProvider.ReleaseChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > CHANNEL_COUNT)) throw new ArgumentOutOfRangeException("channel");

            // This devices does not operate in exclusive mode, so we'll just ignore
        }

        int IAdcControllerProvider.ChannelCount
        {
            get
            {
                return CHANNEL_COUNT;
            }
        }

        ProviderAdcChannelMode IAdcControllerProvider.ChannelMode
        {
            get
            {
                return channelMode;
            }
            set
            {
                channelMode = value;
            }
        }

        int IAdcControllerProvider.MaxValue
        {
            get
            {
                return 255;
            }
        }

        int IAdcControllerProvider.MinValue
        {
            get
            {
                return 0;
            }
        }

        int IAdcControllerProvider.ResolutionInBits
        {
            get
            {
                return 8;
            }
        }
        #endregion // IAdcControllerProvider Interface

        #region IAdcProvider Interface
        IReadOnlyList<IAdcControllerProvider> IAdcProvider.GetControllers()
        {
            return new List<IAdcControllerProvider>() { this };
        }
        #endregion // IAdcProvider Interface

        #region Public Methods
        /// <inheritdoc/>
        public void Dispose()
        {
            if (chipSelectPin != null)
            {
                chipSelectPin.Dispose();
                chipSelectPin = null;
            }
            if (clockPin != null)
            {
                clockPin.Dispose();
                clockPin = null;
            }
            if (dataPin != null)
            {
                dataPin.Dispose();
                dataPin = null;
            }
            isInitialized = false;
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the chip select pin.
        /// </summary>
        /// <value>
        /// The chip select pin.
        /// </value>
        public GpioPin ChipSelectPin
        {
            get
            {
                return chipSelectPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                chipSelectPin = value;
            }
        }

        /// <summary>
        /// Gets or sets the clock pin.
        /// </summary>
        /// <value>
        /// The clock pin.
        /// </value>
        public GpioPin ClockPin
        {
            get
            {
                return clockPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                clockPin = value;
            }
        }

        /// <summary>
        /// Gets or sets the data pin.
        /// </summary>
        /// <value>
        /// The data pin.
        /// </value>
        public GpioPin DataPin
        {
            get
            {
                return dataPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                dataPin = value;
            }
        }
        #endregion // Public Properties
    }
}
