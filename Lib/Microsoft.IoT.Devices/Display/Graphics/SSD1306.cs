// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.Devices.Spi;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// Defines the address modes for the SSD1306.
    /// </summary>
    public enum SSD1306AddressMode : UInt32
    {
        Horizontal = 0x00,
        Vertical = 0x01,
        Page = 0x10
    };

    /// <summary>
    /// A driver for the <see href="http://www.adafruit.com/datasheets/SSD1306.pdf">SSD1306</see> 
    /// SPI display controller.
    /// </summary>
    public sealed class SSD1306 : IDisposable, ISpiBasedDevice
    {
        #region Constants
        private static readonly byte[] CMD_DISPLAY_OFF = { 0xAE };              // Turns the display off
        private static readonly byte[] CMD_DISPLAY_ON = { 0xAF };               // Turns the display on
        private static readonly byte[] CMD_CHARGEPUMP_ON = { 0x8D, 0x14 };      // Turn on internal charge pump to supply power to display
        private static readonly byte PCMD_MEMADDRMODE = 0x20;                   // Horizontal memory mode
        private static readonly byte[] CMD_SEGREMAP = { 0xA1 };                 // Remaps the segments, which has the effect of mirroring the display horizontally
        private static readonly byte[] CMD_COMSCANDIR = { 0xC8 };               // Set the COM scan direction to inverse, which flips the screen vertically
        private static readonly byte[] CMD_RESETCOLADDR = { 0x21, 0x00, 0x7F }; // Reset the column address pointer
        private static readonly byte[] CMD_RESETPAGEADDR = { 0x22, 0x00, 0x07 };// Reset the page address pointer
        #endregion // Constants

        #region Member Variables
        private SSD1306AddressMode addressMode; // The address mode of the display
        private int bitsPerPixel = 1;           // The number of bits per pixel
        private byte[,] buffer;                 // A local buffer we use to store graphics data for the display
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private GpioPin dataPin;                // Pin for data
        private int height = 64;             // Number of horizontal pixels on the display
        private bool isInitialized;             // If IO has been initialized
        private int pages;                   // Number of pages on the display
        private DisplayPixelFormat pixelForamt; // The format of the pixels on the display
        private int pixelsPerPage = 8;       // Number of pixels in each page on the display
        private GpioPin resetPin;               // Pin for reset
        private byte[] serializedBuffer;        // A temporary buffer used to prepare graphics data for sending over SPI
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        private int width = 128;             // Number of horizontal pixels on the display
        #endregion // Member Variables

        #region Internal Methods
        /// <summary>
        /// Clears the internal display buffer.
        /// </summary>
        private void ClearBuffer()
        {
            Array.Clear(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Ensures that the display has been initialized and can be updated.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Wait for initialization
            await InitAllAsync();

            // Done initializing
            isInitialized = true;
        }

        /// <summary>
        /// Initializes GPIO, SPI, and the display
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        private async Task InitAllAsync()
        {
            // Initialize buffers
            InitBuffers();

            // Initialize the SPI controller
            await InitSpiAsync();

            // Initialize the display
            await InitDisplayAsync();
        }

        private void InitBuffers()
        {
            // Validate
            if (width < 1) { throw new MissingIoException(nameof(Width)); }
            if (height < 1) { throw new MissingIoException(nameof(Height)); }
            if (pixelsPerPage < 1) { throw new MissingIoException(nameof(PixelsPerPage)); }

            // Calculate bits per pixel
            switch (pixelForamt)
            {
                case DisplayPixelFormat.OneBit:
                    bitsPerPixel = 1;
                    break;
                case DisplayPixelFormat.Rgb16:
                    bitsPerPixel = 16;
                    break;
                default:
                    throw new NotSupportedException("Unknown pixel format");
            }

            // Calculate pages
            pages = height / pixelsPerPage;

            // Create buffers
            buffer = new byte[width, pages]; // * bitsPerPixel]; // TODO: How do we handle more than 1 bit
            serializedBuffer = new byte[width * pages]; // * bitsPerPixel]; // TODO: How do we handle more than 1 bit
        }

        /// <summary>
        /// Sends the SPI commands to power up and initialize the display 
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        private async Task InitDisplayAsync()
        {
            await ResetAsync();              // Perform a hardware reset on the display
            SendCommand(CMD_CHARGEPUMP_ON);  // Turn on the internal charge pump to provide power to the screen
            SetAddressMode(addressMode);     // Set the addressing mode to "horizontal"
            SendCommand(CMD_DISPLAY_ON);     // Turn the display on
        }

        /// <summary>
        /// Initializes GPIO
        /// </summary>
        private void InitGpio()
        {
            // Validate
            if (dataPin == null) { throw new MissingIoException(nameof(DataPin)); }
            if (resetPin == null) { throw new MissingIoException(nameof(ResetPin)); }

            // Initialize a pin as output for the data / command line on the display
            dataPin.Write(GpioPinValue.High);
            dataPin.SetDriveMode(GpioPinDriveMode.Output);

            // Initialize a pin as output for the hardware reset line on the display
            resetPin.Write(GpioPinValue.High);
            resetPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        /// <summary>
        /// Initializes the SPI bus.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        private async Task InitSpiAsync()
        {
            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }

            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(chipSelectLine);

            // Datasheet specifies maximum SPI clock frequency of 10MHz
            settings.ClockFrequency = 10000000;

            // The display expects an idle-high clock polarity, we use Mode3
            // to set the clock polarity and phase to: CPOL = 1, CPHA = 1
            settings.Mode = SpiMode.Mode3;

            // Find the selector string for the SPI bus controller
            string spiAqs = SpiDevice.GetDeviceSelector(controllerName);

            // Find the SPI bus controller device with our selector string
            var deviceInfo = (await DeviceInformation.FindAllAsync(spiAqs)).FirstOrDefault();

            // Make sure device was found
            if (deviceInfo == null) { throw new DeviceNotFoundException(controllerName); }

            // Create an SpiDevice with our bus controller and SPI settings
            spiDevice = await SpiDevice.FromIdAsync(deviceInfo.Id, settings);
        }

        /// <summary>
        /// Sends a command to the controller.
        /// </summary>
        /// <param name="command">
        /// The command to send.
        /// </param>
        private void SendCommand(byte[] command)
        {
            // When the data / command pin is low, SPI data is treated 
            // as commands for the display controller
            dataPin.Write(GpioPinValue.Low);
            spiDevice.Write(command);
        }

        /// <summary>
        /// Sends data to the controller.
        /// </summary>
        /// <param name="data">
        /// The data to send.
        /// </param>
        private void SendData(byte[] data)
        {
            // When the data / command pin is high, SPI data is treated 
            // as graphics data
            dataPin.Write(GpioPinValue.High);
            spiDevice.Write(data);
        }

        private void SetAddressMode(SSD1306AddressMode mode)
        {
            // Build packet
            var packet = new byte[] { PCMD_MEMADDRMODE, (byte)mode };

            // Send command
            SendCommand(packet);
        }

        /// <summary>
        /// Writes the buffer out to the physical display.
        /// </summary>
        private Task WriteBufferAsync()
        {
            return Task.Run(() =>
            {
                int index = 0;
                // Convert our 2-dimensional array into a serialized string of bytes 
                // that will be sent out to the display
                for (int pageY = 0; pageY < pages; pageY++)
                {
                    for (int pixelX = 0; pixelX < width; pixelX++)
                    {
                        // TODO: How do we copy more than one bit per pixel?
                        serializedBuffer[index] = buffer[pixelX, pageY];

                        // TODO: How do we move more than one bit per pixel?
                        index++;
                    }
                }

                // Write the data out to the screen
                // Reset the column address pointer back to 0
                SendCommand(CMD_RESETCOLADDR);

                // Reset the page address pointer back to 0
                SendCommand(CMD_RESETPAGEADDR);

                // Send the data over SPI
                SendData(serializedBuffer);
            });
        }

        /*
        /// <summary>
        /// Writes a string to the display screen buffer.
        /// </summary>
        /// <param name="text">
        /// The text to write.
        /// </param>
        /// <param name="col">
        /// The horizontal column to start drawing at. This is equivalent to the 'X' axis pixel position.
        /// </param>
        /// <param name="row">
        /// The vertical row to start drawing at. The screen is divided up into 4 rows of 16 pixels each, so valid values for Row are 0,1,2,3.
        /// </param>
        /// <remarks>
        /// <see cref="WriteBufferAsync"/> needs to be called subsequently to output the buffer to the screen.
        /// </remarks>
        private void WriteText(string text, UInt32 col, UInt32 row)
        {
            UInt32 charWidth = 0;
            foreach (Char character in text)
            {
                charWidth = WriteChar(character, col, row);
                col += charWidth;   // Increment the column so we can track where to write the next character
                if (charWidth == 0) // Quit if we encounter a character that couldn't be printed
                {
                    return;
                }
            }
        }
        */

        /*
        /// <summary>
        /// Writes one character to the display screen buffer.
        /// </summary>
        /// <param name="chr">
        /// The character to draw.
        /// </param>
        /// <param name="col">
        /// The horizontal column to start drawing at. This is equivalent to the 'X' axis pixel position.
        /// </param>
        /// <param name="row">
        /// The vertical row to start drawing at. The screen is divided up into 4 rows of 16 pixels each, so valid values for Row are 0,1,2,3.
        /// </param>
        /// <returns>
        /// The number of horizontal pixels used. This value is 0 if <paramref name="row"/> or 
        /// <paramref name="col"/> are out-of-bounds, or if the character isn't available in the font.
        /// </returns>
        /// <remarks>
        /// <see cref="WriteBufferAsync"/> needs to be called subsequently to output the buffer to the screen.
        /// </remarks>
        private UInt32 WriteChar(Char chr, UInt32 col, UInt32 row)
        {
            // Check that we were able to find the font corresponding to our character
            FontCharacterDescriptor CharDescriptor = DisplayFontTable.GetCharacterDescriptor(chr);
            if (CharDescriptor == null)
            {
                return 0;
            }

            // Make sure we're drawing within the boundaries of the screen buffer
            UInt32 MaxRowValue = (pages / DisplayFontTable.FontHeightBytes) - 1;
            UInt32 MaxColValue = width;
            if (row > MaxRowValue)
            {
                return 0;
            }
            if ((col + CharDescriptor.CharacterWidthPx + DisplayFontTable.FontCharSpacing) > MaxColValue)
            {
                return 0;
            }

            UInt32 CharDataIndex = 0;
            UInt32 StartPage = row * 2;
            UInt32 EndPage = StartPage + CharDescriptor.CharacterHeightBytes;
            UInt32 StartCol = col;
            UInt32 EndCol = StartCol + CharDescriptor.CharacterWidthPx;
            UInt32 CurrentPage = 0;
            UInt32 CurrentCol = 0;

            // Copy the character image into the display buffer
            for (CurrentPage = StartPage; CurrentPage < EndPage; CurrentPage++)
            {
                for (CurrentCol = StartCol; CurrentCol < EndCol; CurrentCol++)
                {
                    buffer[CurrentCol, CurrentPage] = CharDescriptor.CharacterData[CharDataIndex];
                    CharDataIndex++;
                }
            }

            // Pad blank spaces to the right of the character so there exists space between adjacent characters
            for (CurrentPage = StartPage; CurrentPage < EndPage; CurrentPage++)
            {
                for (; CurrentCol < EndCol + DisplayFontTable.FontCharSpacing; CurrentCol++)
                {
                    buffer[CurrentCol, CurrentPage] = 0x00;
                }
            }

            // Return the number of horizontal pixels used by the character
            return CurrentCol - StartCol;
        }
        */
        #endregion // Internal Methods


        #region Public Methods
        /// <summary>
        /// Clears the display.
        /// </summary>
        public IAsyncAction ClearAsync()
        {
            if (isInitialized)
            {
                ClearBuffer();
                return WriteBufferAsync().AsAsyncAction();
            }
            else
            {
                return TaskExtensions.CompletedTask.AsAsyncAction();
            }
        }

        public void Dispose()
        {
            if (spiDevice != null)
            {
                spiDevice.Dispose();
                spiDevice = null;
            }
            if (resetPin != null)
            {
                resetPin.Dispose();
                resetPin = null;
            }
            if (dataPin != null)
            {
                dataPin.Dispose();
                dataPin = null;
            }
        }

        public IAsyncAction WritePixelAsync(int x, int y, Color color)
        {
            return Task.Run(async () =>
            {
                await EnsureInitializedAsync();

                // Calculate page and remainder
                int page = (y / pixelsPerPage);
                int pix = y % pixelsPerPage;

                // TODO: Doing 1-bit pixel
                bool white = (color != Colors.Black);

                // Which bit mask?
                byte bits = (byte) (1 << (int)pix);

                // Get current pixel
                byte cur = buffer[x, page];

                // Toggle
                if (white)
                {
                    cur |= bits;
                }
                else
                {
                    cur |= (byte)~bits;
                }

                // Update buffer
                buffer[x, page] = cur;

            }).AsAsyncAction();
        }

        /// <summary>
        /// Flips the display vertically.
        /// </summary>
        /// <returns>
        /// A <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        public IAsyncAction FlipAsync()
        {
            return Task.Run(async ()=>
            {
                // Ensure initialized
                await EnsureInitializedAsync();

                // Flip the display vertically
                SendCommand(CMD_COMSCANDIR);

                // Changing the scan direction takes effect immediately
                // and does not require rewriting the buffer.
            }).AsAsyncAction();
        }

        /// <summary>
        /// Mirrors the display horizontally.
        /// </summary>
        /// <returns>
        /// A <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        public IAsyncAction MirrorAsync()
        {
            return Task.Run(async () =>
            {
                // Ensure initialized
                await EnsureInitializedAsync();

                // Mirror the display horizontally
                SendCommand(CMD_SEGREMAP);

                // Update the display
                await WriteBufferAsync();
            }).AsAsyncAction();
        }

        /// <summary>
        /// Performs a hardware reset of the display 
        /// </summary>
        /// <returns>
        /// A <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        public IAsyncAction ResetAsync()
        {
            return Task.Run(async () =>
            {
                await EnsureInitializedAsync();
                resetPin.Write(GpioPinValue.Low);   // Put display into reset
                await Task.Delay(1);                // Wait at least 3uS (We wait 1mS since that is the minimum delay we can specify for Task.Delay()
                resetPin.Write(GpioPinValue.High);  // Bring display out of reset
                await Task.Delay(100);              // Wait at least 100mS before sending commands
            }).AsAsyncAction();
        }

        public IAsyncAction UpdateAsync()
        {
            return WriteBufferAsync().AsAsyncAction();
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the address mode of the display.
        /// </summary>
        /// <value>
        /// The address mode of the display. The default is <see cref="SSD1306AddressMode.Horizontal"/>.
        /// </value>
        [DefaultValue(SSD1306AddressMode.Horizontal)]
        public SSD1306AddressMode AddressMode
        {
            get
            {
                return addressMode;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                addressMode = value;
            }
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

        /// <summary>
        /// Gets or sets the data pin for the display.
        /// </summary>
        /// <value>
        /// The data pin for the display.
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

        /// <summary>
        /// Gets or sets the reset pin for the display.
        /// </summary>
        /// <value>
        /// The reset pin for the display.
        /// </value>
        public GpioPin ResetPin
        {
            get
            {
                return resetPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                resetPin = value;
            }
        }

        /// <summary>
        /// Gets or sets the width of the display in pixels.
        /// </summary>
        /// <value>
        /// The width of the display in pixels. The default is 128.
        /// </value>
        [DefaultValue(128)]
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                width = value;
            }
        }

        /// <summary>
        /// Gets or sets the height of the display in pixels.
        /// </summary>
        /// <value>
        /// The height of the display in pixels. The default is 64.
        /// </value>
        [DefaultValue(64)]
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the format of the pixels on the display.
        /// </summary>
        /// <value>
        /// The format of the pixels on the display.
        /// </value>
        [DefaultValue(DisplayPixelFormat.OneBit)]
        public DisplayPixelFormat PixelFormat
        {
            get
            {
                return pixelForamt;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                pixelForamt = value;
            }
        }


        /// <summary>
        /// Gets or sets the number of pixels per page on the display.
        /// </summary>
        /// <value>
        /// The number of pixels per page on the display. The default is 8.
        /// </value>
        [DefaultValue(8)]
        public int PixelsPerPage
        {
            get
            {
                return pixelsPerPage;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                pixelsPerPage = value;
            }
        }
        #endregion // Public Properties
    }
}
