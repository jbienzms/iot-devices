// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.microchip.com/wwwproducts/Devices.aspx?dDocName=en010534">MCP3208</see> 
    /// 12-bit A/D converter.
    /// </summary>
    public sealed class MCP3208 : IAdcControllerProvider, IDisposable
    {
        #region Constants
        private const byte COMMAND_SINGLE = 0x06;
        private const byte COMMAND_DIFF = 0x04;
        // static private readonly byte[] CONFIG_BUFFER = new byte[3] { 0x06, 0x00, 0x00 }; // 00000110 channel configuration data for the MCP3208
        #endregion // Constants

        #region Member Variables
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private bool isInitialized;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        #endregion // Member Variables

        #region Constructors
        public MCP3208()
        {
            // Set Defaults
            ChannelMode = ProviderAdcChannelMode.SingleEnded;
        }
        #endregion // Constructors

        #region Internal Methods
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

            // Done initializing
            isInitialized = true;
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
            EnsureInitializedAsync().Wait();

            // The code below is based on the MCP3208 spec sheet by Microchip
            // http://ww1.microchip.com/downloads/en/DeviceDoc/21298e.pdf

            // Buffers to hold write and read data
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };
            byte[] readBuffer = new byte[3];

            // From http://forum.arduino.cc/index.php?topic=53082.0:
            // 2 bytes need to be written to the ADC before values will start coming out:
            // 000001 < S / D >< D2 >  < D1 >< D0 > XXXXXX
            // S / D represents single mode or differential mode ADC calculation:  1 for single, 0 for differential
            // D2, D1, D0 represent the channel select.
            UInt16 command;
            if (ChannelMode == ProviderAdcChannelMode.Differential)
            {
                command = (UInt16)(COMMAND_DIFF << 8 | channelNumber << 6);
            }
            else
            {
                command = (UInt16)(COMMAND_SINGLE << 8 | channelNumber << 6);
            }
            var commandBytes = BitConverter.GetBytes(command);
            writeBuffer[0] = commandBytes[1];
            writeBuffer[1] = commandBytes[0];

            // Write command and read data from the ADC in one line
            spiDevice.TransferFullDuplex(writeBuffer, readBuffer);

            // Convert the returned bytes into an integer value
            int result = readBuffer[1] & 0x0F;
            result <<= 8;
            result += readBuffer[2];

            // Done
            return result;
        }

        public void ReleaseChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");

            // This devices does not operate in exclusive mode, so we'll just ignore
        }

        public void Dispose()
        {
            if (spiDevice != null)
            {
                spiDevice.Dispose();
                spiDevice = null;
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
        #endregion // Public Properties
    }
}
