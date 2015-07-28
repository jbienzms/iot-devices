// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.ti.com/product/adc0832-n">ADC0832</see> 
    /// 8-bit A/D converter.
    /// </summary>
    public sealed class ADC0832 : IAdcController, IDisposable
    {
        #region Member Variables
        private GpioPin chipSelectPin;
        private GpioPin clockPin;
        private GpioPin dataPin;
        private bool isInitialized;
        private ADC0832Channel[] channels;
        #endregion // Member Variables

        private void EnsureInitialized()
        {
            if (isInitialized) { return; }
            if (chipSelectPin == null) { throw new MissingIoException(nameof(ChipSelectPin)); }
            if (clockPin == null) { throw new MissingIoException(nameof(ClockPin)); }
            if (dataPin == null) { throw new MissingIoException(nameof(DataPin)); }
            channels = new ADC0832Channel[2];
            chipSelectPin.SetDriveMode(GpioPinDriveMode.Output);
            clockPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        public IAdcChannel OpenChannel(int channelNumber)
        {
            if ((channelNumber < 0) || (channelNumber > 1)) throw new ArgumentOutOfRangeException("channelNumber");
            EnsureInitialized();
            if (channels[channelNumber] == null)
            {
                channels[channelNumber] = new ADC0832Channel(this, channelNumber);
            }
            return channels[channelNumber];
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
            channels = null;
            isInitialized = false;
        }

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
    }
}
