// Copyright (c) Microsoft. All rights reserved.
//
using Microsoft.IoT.DeviceCore.Adc;
using Microsoft.IoT.DeviceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP3008">MCP3008</see> 
    /// 10-bit A/D converter.
    /// </summary>
    /// <remarks>
    /// This class is not designed to be used directly to read data. Instead, once it 
    /// has been created and configured it can be passed to the 
    /// <see cref="AdcController.GetControllersAsync">GetControllersAsync</see> 
    /// method of the <see cref="AdcController"/> class or it can be added to the 
    /// <see cref="AdcProviderManager.Providers">Providers</see> collection in a 
    /// <see cref="AdcProviderManager"/>.
    /// </remarks>
    public sealed class MCP3008 : IAdcControllerProvider, IAdcProvider, IDisposable
    {
        #region Constants
        private const int CHANNEL_COUNT = 8;
        #endregion // Constants

        #region Member Variables
        private ProviderAdcChannelMode channelMode = ProviderAdcChannelMode.SingleEnded;
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private bool isInitialized;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        #endregion // Member Variables

        #region Internal Methods
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }

            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(chipSelectLine);

            settings.ClockFrequency = 1000000;

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

            // Done initializing
            isInitialized = true;
        }
        #endregion // Internal Methods

        #region IAdcControllerProvider Interface
        void IAdcControllerProvider.AcquireChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > CHANNEL_COUNT)) throw new ArgumentOutOfRangeException("channel");
            // This devices does not operate in exclusive mode, so we'll just ignore
        }

        int IAdcControllerProvider.ReadValue(int channelNumber)
        {
            // Validate
            if ((channelNumber < 0) || (channelNumber > CHANNEL_COUNT)) throw new ArgumentOutOfRangeException("channelNumber");
            EnsureInitializedAsync().Wait();
            // The code below is based on the MCP3008 spec sheet by Microchip
            // Buffers to hold write and read data
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };
            byte[] readBuffer = new byte[3];
            //this is two bytes.
            UInt16 command = 0x0;
            if (channelMode == ProviderAdcChannelMode.Differential)
            {
                //leading bits changes depending on resolution of ADC
                int shiftLeftNum = 8; // 15 - 7 
                //shift the channel selection to be after start bit
                //and mode bit
                int channelShifter = 13;
                //0x01 is 0001 in binary
                command = (UInt16)((0x01 << shiftLeftNum) //start bit + single bit
                           | ((Int16)channelNumber << channelShifter));
            }
            else
            {
                //leading bits changes depending on resolution of ADC
                int shiftLeftNum = 8; // 15 - 7 
                //shift the channel selection to be after start bit
                //and mode bit
                int channelShifter = 13;
                //0x03 is 0011 in binary
                command = (UInt16)((0x03 << shiftLeftNum) //start bit + single bit
                           | ((Int16)channelNumber << channelShifter));
            }
            var commandBytes = BitConverter.GetBytes(command);
            writeBuffer[0] = commandBytes[1];
            writeBuffer[1] = commandBytes[0];

            // Write command and read data from the ADC in one line
            spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

            //bit mask result to ditch all of first 
            //byte except value
            //This changes depending on ADC resolution
            int result = readBuffer[1] & 0x03;

            //Shift these bits by a full byte 
            //as they are most significant bits
            result <<= 8;

            //Add the second byte as an int to the result.
            //C# rocks.
            result += readBuffer[2];

            //return the result
            return (int)result;
        }

        void IAdcControllerProvider.ReleaseChannel(int channel)
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

        int IAdcControllerProvider.ChannelCount
        {
            get
            {
                return 8;
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
                return 1023;
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
                return 10;
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
            if (spiDevice != null)
            {
                spiDevice.Dispose();
                spiDevice = null;
            }
            isInitialized = false;
        }
        #endregion // Public Methods

        #region Public Properties
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
        #endregion // Public Properties
    }
}
