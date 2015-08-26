// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Gpio;
using Microsoft.IoT.DeviceHelpers;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.ti.com/product/adc0832-n">ADC0832</see> 
    /// 8-bit A/D converter.
    /// </summary>
    public sealed class ADC0832 : IAdcControllerProvider, IDisposable
    {
        #region Member Variables
        private GpioPin chipSelectPin;
        private GpioPin clockPin;
        private GpioPin dataPin;
        private bool isInitialized;
        #endregion // Member Variables

        #region Constructors
        public ADC0832()
        {
            // Set Defaults
            ChannelMode = ProviderAdcChannelMode.SingleEnded;
        }
        #endregion // Constructors

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

        #region Public Methods
        public void AcquireChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");

            // This devices does not operate in exclusive mode, so we'll just ignore
        }

        public int ReadValue(int channelNumber)
        {
            // Validate
            if ((channelNumber < 0) || (channelNumber > ChannelCount)) throw new ArgumentOutOfRangeException("channelNumber");

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
            if (ChannelMode == ProviderAdcChannelMode.SingleEnded)
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

        public void ReleaseChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");

            // This devices does not operate in exclusive mode, so we'll just ignore
        }

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

        public bool IsChannelModeSupported(ProviderAdcChannelMode channelMode)
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

        public int ChannelCount
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// Gets or sets the reading mode of the ADC.
        /// </summary>
        /// <value>
        /// A <see cref="ProviderAdcChannelMode"/> that represents the reading mode of the ADC. 
        /// The default is <see cref="ProviderAdcChannelMode.SingleEnded"/>.
        /// </value>
        /// <remarks>
        /// For more information see 
        /// <see href="http://www.maximintegrated.com/en/app-notes/index.mvp/id/1108">
        /// Understanding Single-Ended, Pseudo-Differential and Fully-Differential ADC Inputs
        /// </see>
        /// </remarks>
        [DefaultValue(ProviderAdcChannelMode.SingleEnded)]
        public ProviderAdcChannelMode ChannelMode { get; set; }

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

        public int MaxValue
        {
            get
            {
                return 255;
            }
        }

        public int MinValue
        {
            get
            {
                return 0;
            }
        }

        public int ResolutionInBits
        {
            get
            {
                return 8;
            }
        }
        #endregion // Public Properties
    }
}
