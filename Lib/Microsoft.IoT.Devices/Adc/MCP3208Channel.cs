// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Represents a single channel for the <see href="http://www.microchip.com/wwwproducts/Devices.aspx?dDocName=en010534">MCP3208</see>.
    /// </summary>
    public sealed class MCP3208Channel : IAdcChannel
    {
        #region Constants
        private const int HALF_VALUE = 2047;
        static private readonly byte[] CONFIG_BUFFER = new byte[3] { 0x06, 0x00, 0x00 }; // 00000110 channel configuration data for the MCP3208
        #endregion // Constants

        #region Member Variables
        private int channel;
        private SpiDevice spiDevice;
        #endregion // Member Variables

        #region Constructors
        internal MCP3208Channel(int channel, SpiDevice spiDevice)
        {
            this.channel = channel;
            this.spiDevice = spiDevice;
        }
        #endregion // Constructors

        public void Close() { }

        public double ReadRatio()
        {
            // Read
            var val = (double)ReadValue();

            // Scale
            val -= HALF_VALUE;
            val /= HALF_VALUE;

            // Done
            return val;
        }

        public int ReadValue()
        {
            // Buffer to hold read data
            byte[] readBuffer = new byte[3];

            // Read data from the ADC
            spiDevice.TransferFullDuplex(CONFIG_BUFFER, readBuffer);

            // Convert the returned bytes into an integer value
            int result = readBuffer[1] & 0x0F;
            result <<= 8;
            result += readBuffer[2];

            // Done
            return result;
        }
    }
}
