using Microsoft.IoT.DeviceHelpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Adc.Provider;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Driver for the <see href="http://www.microchip.com/wwwproducts/Devices.aspx?product=MCP3008">MCP3008</see> 
    /// 10-bit A/D converter.
    /// </summary>
    public sealed class MCP3008 : IAdcControllerProvider, IDisposable
    {
        #region Constants
        private const int channelCount = 8;
        private const int maxValue = 1023;
        private const int minValue = 0;
        private const int resolutionInBits = 10;
        #endregion // Constants

        #region Member Variables
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private bool isInitialized;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="MCP3008"/> instance.
        /// </summary>
        public MCP3008()
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

        #region Public Methods
        /// <inheritdoc/>
        public void AcquireChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");
            // This devices does not operate in exclusive mode, so we'll just ignore
        }
        /// <inheritdoc/>
        public int ReadValue(int channelNumber)
        {
            // Validate
            if ((channelNumber < 0) || (channelNumber > ChannelCount)) throw new ArgumentOutOfRangeException("channelNumber");
            EnsureInitializedAsync().Wait();
            // The code below is based on the MCP3008 spec sheet by Microchip
            // Buffers to hold write and read data
            byte[] writeBuffer = new byte[3] { 0x00, 0x00, 0x00 };
            byte[] readBuffer = new byte[3];
            //this is two bytes.
            UInt16 command = 0x0;
            if (ChannelMode == ProviderAdcChannelMode.Differential)
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
        /// <inheritdoc/>
        public void ReleaseChannel(int channel)
        {
            // Validate
            if ((channel < 0) || (channel > ChannelCount)) throw new ArgumentOutOfRangeException("channel");
            // This devices does not operate in exclusive mode, so we'll just ignore
        }
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public int MaxValue
        {
            get
            {
                return maxValue;
            }
        }
        /// <inheritdoc/>
        public int MinValue
        {
            get
            {
                return minValue;
            }
        }
        /// <inheritdoc/>
        public int ResolutionInBits
        {
            get
            {
                return resolutionInBits;
            }
        }
        #endregion // Public Properties
    }
}
