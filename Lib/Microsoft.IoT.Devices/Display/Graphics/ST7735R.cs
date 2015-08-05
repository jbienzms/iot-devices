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
using Windows.UI;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// A driver for the Adafruit <see href="http://www.adafruit.com/products/358">ST7735R</see> display.
    /// </summary>
    /// <remarks>
    /// This is a port of the <see href="http://netduinohelpers.codeplex.com/SourceControl/latest#Hardware/AdaFruitST7735.cs">AdaFruitST7735</see> 
    /// class from the awesome <see href="http://netduinohelpers.codeplex.com/">Netduino Helpers</see> project.
    /// </remarks>
    public sealed class ST7735R : IGraphicsDisplay, IDevice, IDisposable
    {
        #region Constants
        private enum Colors
        {
            Black = 0x0000,
            Blue = 0x001F,
            Red = 0xF800,
            Green = 0x07E0,
            Cyan = 0x07FF,
            Magenta = 0xF81F,
            Yellow = 0xFFE0,
            White = 0xFFFF
        }

        private enum LcdCommand
        {
            NOP = 0x0,
            SWRESET = 0x01,
            RDDID = 0x04,
            RDDST = 0x09,
            SLPIN = 0x10,
            SLPOUT = 0x11,
            PTLON = 0x12,
            NORON = 0x13,
            INVOFF = 0x20,
            INVON = 0x21,
            DISPOFF = 0x28,
            DISPON = 0x29,
            CASET = 0x2A,
            RASET = 0x2B,
            RAMWR = 0x2C,
            RAMRD = 0x2E,
            COLMOD = 0x3A,
            MADCTL = 0x36,
            FRMCTR1 = 0xB1,
            FRMCTR2 = 0xB2,
            FRMCTR3 = 0xB3,
            INVCTR = 0xB4,
            DISSET5 = 0xB6,
            PWCTR1 = 0xC0,
            PWCTR2 = 0xC1,
            PWCTR3 = 0xC2,
            PWCTR4 = 0xC3,
            PWCTR5 = 0xC4,
            VMCTR1 = 0xC5,
            RDID1 = 0xDA,
            RDID2 = 0xDB,
            RDID3 = 0xDC,
            RDID4 = 0xDD,
            PWCTR6 = 0xFC,
            GMCTRP1 = 0xE0,
            GMCTRN1 = 0xE1
        }

        static private readonly GpioPinValue CommandMode = GpioPinValue.Low;
        static private readonly GpioPinValue DataMode = GpioPinValue.High;
        #endregion // Constants

        #region Member Variables
        private bool autoUpdate;                // Does the display automatically update after various drawing functions
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private byte[] displayBuffer;               // In memory allocation for display
        private int height = 160;
        private bool isInitialized;
        private GpioPin modePin;                // Switches between command and data
        private DisplayPixelFormat pixelFormat = DisplayPixelFormat.Rgb565;
        private GpioPin resetPin;               // Resets the display
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        private readonly byte[] spiByte = new byte[1]; // The allocated memory for a single byte command
        private int width = 128;
        #endregion // Member Variables

        #region Internal Methods
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }
            if (modePin == null) { throw new MissingIoException(nameof(ModePin)); }
            if (resetPin == null) { throw new MissingIoException(nameof(ResetPin)); }

            // SPI
            await InitSpiAsync();

            // Display
            await InitDisplayAsync();

            // Done initializing
            isInitialized = true;
        }

        private async Task InitDisplayAsync()
        {
            // Allocate buffers
            displayBuffer = new byte[Width * Height * GraphicsTools.GetBitsPerPixel(pixelFormat)];
            
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(50);
            resetPin.Write(GpioPinValue.Low);
            await Task.Delay(50);
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(50);
            // new OutputPort

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SWRESET); // software reset
            await Task.Delay(150);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SLPOUT);  // out of sleep mode
            await Task.Delay(150);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR1);  // frame rate control - normal mode
            modePin.Write(DataMode);
            Write(0x01);  // frame rate = fosc / (1 x 2 + 40) * (LINE + 2C + 2D)
            Write(0x2C);
            Write(0x2D);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR2);  // frame rate control - idle mode
            modePin.Write(DataMode);
            Write(0x01);  // frame rate = fosc / (1 x 2 + 40) * (LINE + 2C + 2D)
            Write(0x2C);
            Write(0x2D);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR3);  // frame rate control - partial mode
            modePin.Write(DataMode);
            Write(0x01); // dot inversion mode
            Write(0x2C);
            Write(0x2D);
            Write(0x01); // line inversion mode
            Write(0x2C);
            Write(0x2D);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.INVCTR);  // display inversion control
            modePin.Write(DataMode);
            Write(0x07);  // no inversion

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR1);  // power control
            modePin.Write(DataMode);
            Write(0xA2);
            Write(0x02);      // -4.6V
            Write(0x84);      // AUTO mode

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR2);  // power control
            modePin.Write(DataMode);
            Write(0xC5);      // VGH25 = 2.4C VGSEL = -10 VGH = 3 * AVDD

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR3);  // power control
            modePin.Write(DataMode);
            Write(0x0A);      // Opamp current small 
            Write(0x00);      // Boost frequency

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR4);  // power control
            modePin.Write(DataMode);
            Write(0x8A);      // BCLK/2, Opamp current small & Medium low
            Write(0x2A);

            Write((byte)LcdCommand.PWCTR5);  // power control
            modePin.Write(DataMode);
            Write(0x8A);
            Write(0xEE);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.VMCTR1);  // power control
            modePin.Write(DataMode);
            Write(0x0E);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.INVOFF);    // don't invert display

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);  // memory access control (directions)
            modePin.Write(DataMode);
            Write(0xC8);  // row address/col address, bottom to top refresh

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.COLMOD);  // set color mode
            modePin.Write(DataMode);
            Write(0x05);        // 16-bit color

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.CASET);  // column addr set
            modePin.Write(DataMode);
            Write(0x00);
            Write(0x00);   // XSTART = 0
            Write(0x00);
            Write(0x7F);   // XEND = 127

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.RASET);  // row addr set
            modePin.Write(DataMode);
            Write(0x00);
            Write(0x00);    // XSTART = 0
            Write(0x00);
            Write(0x9F);    // XEND = 159

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRP1);
            modePin.Write(DataMode);
            Write(0x02);
            Write(0x1c);
            Write(0x07);
            Write(0x12);
            Write(0x37);
            Write(0x32);
            Write(0x29);
            Write(0x2d);
            Write(0x29);
            Write(0x25);
            Write(0x2B);
            Write(0x39);
            Write(0x00);
            Write(0x01);
            Write(0x03);
            Write(0x10);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRN1);
            modePin.Write(DataMode);
            Write(0x03);
            Write(0x1d);
            Write(0x07);
            Write(0x06);
            Write(0x2E);
            Write(0x2C);
            Write(0x29);
            Write(0x2D);
            Write(0x2E);
            Write(0x2E);
            Write(0x37);
            Write(0x3F);
            Write(0x00);
            Write(0x00);
            Write(0x02);
            Write(0x10);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.DISPON);
            await Task.Delay(50);

            modePin.Write(CommandMode);
            Write((byte)LcdCommand.NORON);  // normal display on
            await Task.Delay(10);

            modePin.Write(DataMode);


        }

        private async Task InitSpiAsync()
        {
            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(chipSelectLine);

            // Ported code specifies clock frequency of 9500KHz
            settings.ClockFrequency = 9500000;

            // The port says idle is low and polarity is not specified. Using Mode0.
            settings.Mode = SpiMode.Mode0;

            // 8 bits per transfer
            settings.DataBitLength = 8;

            // Find the selector string for the SPI bus controller
            string spiAqs = SpiDevice.GetDeviceSelector(controllerName);

            // Find the SPI bus controller device with our selector string
            var deviceInfo = (await DeviceInformation.FindAllAsync(spiAqs)).FirstOrDefault();

            // Make sure device was found
            if (deviceInfo == null) { throw new DeviceNotFoundException(controllerName); }

            // Create an SpiDevice with our bus controller and SPI settings
            spiDevice = await SpiDevice.FromIdAsync(deviceInfo.Id, settings);
        }

        public void SetPixel(int x, int y, uint nativeColor) 
        {
            // TODO: Should not be ushort
            ushort uscolor = (ushort)nativeColor;

            if ((x < 0) || (x >= width) || (y < 0) || (y >= height)) return;
            var index = ((y * width) + x) * sizeof(ushort);
            Write(index, (byte)(uscolor >> 8));
            Write(++index, (byte)(uscolor));
        }

        private void Write(byte command)
        {
            spiByte[0] = command;
            spiDevice.Write(spiByte);
        }

        private void Write(long address, byte data)
        {
            displayBuffer[address] = data;
        }
        #endregion // Internal Methods


        public void Clear()
        {

        }

        public void Dispose()
        {
            if (modePin != null)
            {
                modePin.Dispose();
                modePin = null;
            }
            if (resetPin != null)
            {
                resetPin.Dispose();
                resetPin = null;
            }
            if (spiDevice != null)
            {
                spiDevice.Dispose();
                spiDevice = null;
            }
            displayBuffer = null;
        }

        public void DrawPixel(int x, int y, Color color)
        {
            // TODO: BAD
            EnsureInitializedAsync().Wait();

            // Get color
            var nativeColor = GraphicsTools.GetNativeColor(pixelFormat, color);
            SetPixel(x, y, nativeColor);
            if (autoUpdate)
            {
                Update();
            }
        }

        public void Update()
        {
            spiDevice.Write(displayBuffer);
        }



        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Update"/> should automatically be called 
        /// after drawing operations.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Update"/> should automatically be called 
        /// after drawing operations; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// This property can be set to <c>false</c> to have more fine grained control over 
        /// how many drawing operations are batched before they are sent to the display.
        /// </remarks>
        [DefaultValue(true)]
        public bool AutoUpdate
        {
            get
            {
                return autoUpdate;
            }
            set
            {
                autoUpdate = value;
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
        /// Gets or sets the height of the display in pixels.
        /// </summary>
        /// <value>
        /// The height of the display in pixels. The default is 160.
        /// </value>
        [DefaultValue(160)]
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");
                if (isInitialized) { throw new IoChangeException(); }
                height = value;
            }
        }

        /// <summary>
        /// Gets or sets the pin used to change the mode of the display between data and command.
        /// </summary>
        /// <value>
        /// The pin used to change the mode of the display between data and command.
        /// </value>
        /// <remarks>
        /// On some displays this pin is marked 'DC' or even 'RS'.
        /// </remarks>
        public GpioPin ModePin
        {
            get
            {
                return modePin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                modePin = value;
            }
        }

        /// <summary>
        /// Gets or sets the format for each pixel on the display.
        /// </summary>
        /// <value>
        /// A <seealso cref="DisplayPixelFormat"/> that describes the pixel format. 
        /// The default is <see cref="DisplayPixelFormat.Rgb565"/>.
        /// </value>
        [DefaultValue(DisplayPixelFormat.Rgb565)]
        public DisplayPixelFormat PixelFormat
        {
            get
            {
                return pixelFormat;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                pixelFormat = value;
            }
        }

        /// <summary>
        /// Gets or sets the pin used to reset the display.
        /// </summary>
        /// <value>
        /// The pin used to reset the display.
        /// </value>
        /// <remarks>
        /// This pin is usually marked 'RES'.
        /// </remarks>
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
                if (value < 1) throw new ArgumentOutOfRangeException("value");
                if (isInitialized) { throw new IoChangeException(); }
                width = value;
            }
        }
        #endregion // Public Properties
    }
}
