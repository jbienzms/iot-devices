// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.microchip.com/wwwproducts/Devices.aspx?dDocName=en010534">MCP3208</see> 
    /// 12-bit A/D converter.
    /// </summary>
    public sealed class MCP3208 : IAdcController, IDisposable
    {
        #region Member Variables
        private MCP3208Channel[] channels;
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private bool isInitialized;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        #endregion // Member Variables

        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }

            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(chipSelectLine);

            // Datasheet specifies maximum SPI clock frequency of 0.5MHz
            settings.ClockFrequency = 500000;
            
            // The ADC expects idle-low clock polarity so we use Mode0
            settings.Mode = SpiMode.Mode0;

            // Find the selector string for the SPI bus controller
            string spiAqs = SpiDevice.GetDeviceSelector(controllerName);

            // Find the SPI bus controller device with our selector string
            var deviceInfo = (await DeviceInformation.FindAllAsync(spiAqs)).FirstOrDefault();

            // Make sure device was found
            if (deviceInfo == null) { throw new DeviceNotFoundException(controllerName); }

            // Create an SpiDevice with our bus controller and SPI settings
            spiDevice = await SpiDevice.FromIdAsync(deviceInfo.Id, settings);

            // Initialize channel array
            channels = new MCP3208Channel[8];

            // Done initializing
            isInitialized = true;
        }

        public IAdcChannel OpenChannel(int channelNumber)
        {
            EnsureInitializedAsync().Wait();

            if ((channelNumber < 0) || (channelNumber > 1)) throw new ArgumentOutOfRangeException("channelNumber");
            
            if (channels[channelNumber] == null)
            {
                channels[channelNumber] = new MCP3208Channel(channelNumber, spiDevice);
            }
            return channels[channelNumber];
        }

        public void Dispose()
        {
            if (spiDevice != null)
            {
                spiDevice.Dispose();
                spiDevice = null;
            }
            channels = null;
            isInitialized = false;
        }

        /// <summary>
        /// Gets or sets the chip select line to use on the SPIO controller.
        /// </summary>
        /// <value>
        /// The chip select line to use on the SPIO controller. The default is 0.
        /// </value>
        [DefaultValue(0)]
        public int ChipSelectLine
        {
            get
            {
                return chipSelectLine;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                chipSelectLine = value;
            }
        }

        public int ChannelCount
        {
            get
            {
                return 8;
            }
        }

        /// <summary>
        /// Gets or sets the name of the SPIO controller to use.
        /// </summary>
        /// <value>
        /// The name of the SPIO controller to use. The default is "SPI0".
        /// </value>
        [DefaultValue("SPI0")]
        public string ControllerName
        {
            get
            {
                return controllerName;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                controllerName = value;
            }
        }

        public int MaxValue
        {
            get
            {
                return 4095;
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
                return 12;
            }
        }
    }
}
