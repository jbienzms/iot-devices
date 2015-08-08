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
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.IoT.Devices.Display
{
    /// <summary>
    /// Display types are marked by sticky tabs that ship applied to the displays themselves.
    /// </summary>
    public enum ST7735DisplayType
    {
        /// <summary>
        /// ST7735B
        /// </summary>
        B,
        /// <summary>
        /// ST7735R
        /// </summary>
        R,
        /// <summary>
        /// ST7735R with Black tab
        /// </summary>
        RBlack,
        /// <summary>
        /// ST7735R with Green tab
        /// </summary>
        RGreen,
        /// <summary>
        /// ST7735R with Green tab version 1.44
        /// </summary>
        RGreen144,
        /// <summary>
        /// ST7735R with Red tab
        /// </summary>
        RRed,
    }

    /// <summary>
    /// A driver for displays controlled by the <see href="http://www.sitronix.com.tw/sitronix/product.nsf/Doc/ST7735?OpenDocument">ST7735</see> 
    /// controller such as the <see href="http://www.adafruit.com/products/358">Adafruit 1.8" color display</see>.
    /// </summary>
    /// <remarks>
    /// This driver is adapted from several resources including <see href="http://netduinohelpers.codeplex.com/SourceControl/latest#Hardware/AdaFruitST7735.cs">Netduino Helpres</see> 
    /// and the <see href="https://github.com/adafruit/Adafruit-ST7735-Library/blob/master/Adafruit_ST7735.cpp">Adafruit library for ST7735</see>.
    /// </remarks>
    public sealed class ST7735 : IGraphicsDisplay, IDevice, IDisposable
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
        private const int DefaultClockFrequency = 9500000;
        #endregion // Constants

        #region Member Variables
        private bool autoUpdate = true;         // Does the display automatically update after various drawing functions
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private int clockFrequency = DefaultClockFrequency;   // The clock speed SPI will run at
        private int colStart;
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private byte[] displayBuffer;           // In memory allocation for display
        private ST7735DisplayType displayType = ST7735DisplayType.R; // Display type (version and color tab)
        private int height = 160;
        private bool isInitialized;
        private GpioPin modePin;                // Switches between command and data
        private DisplayPixelFormat pixelFormat = DisplayPixelFormat.Rgb565;
        private GpioPin resetPin;               // Resets the display
        int rowStart;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        private readonly byte[] spiByte = new byte[1]; // The allocated memory for a single byte command
        private int width = 128;
        #endregion // Member Variables

        #region Internal Methods
        public IAsyncAction InitializeAsync()
        {
            return EnsureInitializedAsync().AsAsyncAction();
        }
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }
            if (modePin == null) { throw new MissingIoException(nameof(ModePin)); }
            if (resetPin == null) { throw new MissingIoException(nameof(ResetPin)); }

            // GPIO
            InitGpio();

            // SPI
            await InitSpiAsync();

            // Display
            await InitDisplayAsync();
            // await OldInitDisplayAsync();

            // Done initializing
            isInitialized = true;
        }

        private async Task OldInitDisplayAsync()
        {
            // Allocate buffers
            var bytesPerPixel = GraphicsTools.GetBitsPerPixel(pixelFormat) / 8;
            displayBuffer = new byte[Width * Height * bytesPerPixel];
            
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(50);
            resetPin.Write(GpioPinValue.Low);
            await Task.Delay(50);
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(50);

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

        private async Task InitDisplayAsync()
        {
            // Allocate buffers
            var bytesPerPixel = GraphicsTools.GetBitsPerPixel(pixelFormat) / 8;
            displayBuffer = new byte[Width * Height * bytesPerPixel];
            // displayBuffer = new byte[Width * Height * 3];

            // Hardware Reset
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(500);
            resetPin.Write(GpioPinValue.Low);
            await Task.Delay(500);
            resetPin.Write(GpioPinValue.High);
            await Task.Delay(500);

            // Which display type?
            switch (displayType)
            {
                case ST7735DisplayType.B:
                    await InitDisplayBAsync();
                    break;
                case ST7735DisplayType.R:
                case ST7735DisplayType.RBlack:
                case ST7735DisplayType.RGreen:
                case ST7735DisplayType.RGreen144:
                case ST7735DisplayType.RRed:
                    await InitDisplayRAsync();
                    break;
                default:
                    throw new InvalidOperationException(string.Format(Strings.UnknownDisplayType, displayType));
            }

            // Breathe
            await Task.Delay(10);

            // Set memory address space to full size of the display
            // Note, this also goes to RAM mode
            SetAddressWindow(0, 0, (byte)(Width - 1), (byte)(Height - 1));
        }

        private async Task InitDisplayBAsync()
        {
            // 1: Software reset
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SWRESET);
            await Task.Delay(50);

            // 2: Out of sleep mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SLPOUT);
            await Task.Delay(500);

            // 3: Color mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.COLMOD);
            modePin.Write(DataMode);
            Write(0x05); // TODO: This is 16-bit color. Support additional display types.
            await Task.Delay(10);

            // 4: Frame rate control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR1);
            modePin.Write(DataMode);
            Write(0x00); // Fastest Refresh
            Write(0x06); // 6 lines front porch
            Write(0x03); // 3 lines back porch

            // 5: Memory access control (directions)
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);
            modePin.Write(DataMode);
            Write(0x08); // Row address, column address, bottom to top refresh

            // 6: Display settings #5
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.DISSET5);
            modePin.Write(DataMode);
            Write(0x15); // 1 clock cycle no overlap, 2 cycle gate
            Write(0x02); // Fix on VTL

            // 7: Display inversion control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.INVCTR);
            modePin.Write(DataMode);
            Write(0x00); // Line inversion

            // 8: Power control 1
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR1);
            modePin.Write(DataMode);
            Write(0x02); // 4.7v // TODO: What other options???
            Write(0x70); // 1.0uA
            await Task.Delay(10);

            // 9: Power control 2
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR2);
            modePin.Write(DataMode);
            Write(0x05); // VGH = 14.7v, VTL = -7.35v // TODO: What other options???

            // 10: Power control 3
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR3);
            modePin.Write(DataMode);
            Write(0x01); // Op amp current small
            Write(0x02); // Boost frequency

            // 11: Voltage control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.VMCTR1);
            modePin.Write(DataMode);
            Write(0x01); // VCOMH = 4v // TODO: What other options???
            Write(0x02); // VCOML = -1.1v // TODO: What other options???
            await Task.Delay(10);

            // 12: Power control 6
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR6);
            modePin.Write(DataMode);
            Write(0x11); // ?
            Write(0x15); // ?

            // 13: Unknown 1
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRP1);
            modePin.Write(DataMode);
            Write(0x09);
            Write(0x16);
            Write(0x09);
            Write(0x20);
            Write(0x21);
            Write(0x1B);
            Write(0x13);
            Write(0x19);
            Write(0x17);
            Write(0x15);
            Write(0x1E);
            Write(0x2B);
            Write(0x04);
            Write(0x05);
            Write(0x02);
            Write(0x0E);

            // 14: Unknown 2
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRN1);
            modePin.Write(DataMode);
            Write(0x0B);
            Write(0x14);
            Write(0x08);
            Write(0x1E);
            Write(0x22);
            Write(0x1D);
            Write(0x18);
            Write(0x1E);
            Write(0x1B);
            Write(0x1A);
            Write(0x24);
            Write(0x2B);
            Write(0x06);
            Write(0x06);
            Write(0x02);
            Write(0x0F);
            await Task.Delay(10);

            // 15: Column address set
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.CASET);
            modePin.Write(DataMode);
            Write(0x00);
            Write(0x02); // XSTART = 2
            Write(0x00);
            Write(0x81); // XEND = 129

            // 16: Row address set
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.RASET);
            modePin.Write(DataMode);
            Write(0x00);
            Write(0x02); // YSTART = 2
            Write(0x00);
            Write(0x81); // YEND = 129 // TODO: Doesn't look right

            // 17: Normal display on
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.NORON);
            await Task.Delay(10);

            // 18: Main screen turn on
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.DISPON);
            await Task.Delay(500);
        }


        private async Task InitDisplayRAsync()
        {
            /*****************************************
             * Common
             *****************************************/
            
            // 1: Software reset
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SWRESET);
            await Task.Delay(150);

            // 2: Out of sleep mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.SLPOUT);
            modePin.Write(DataMode);
            await Task.Delay(500);

            // 3: Frame rate control - normal mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR1);
            modePin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Rate = fosc/(1x2+40) * (LINE+2C+2D)
            Write(0x2D); // ***********************************

            // 4: Frame rate control - idle mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR2);
            modePin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Rate = fosc/(1x2+40) * (LINE+2C+2D)
            Write(0x2D); // ***********************************

            // 5: Frame rate control - partial mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR3);
            modePin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Dot inversion mode
            Write(0x2D); // ***********************************
            Write(0x01); // ***********************************
            Write(0x2C); // Line inversion mode
            Write(0x2D); // ***********************************

            // 6: Display inversion control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.INVCTR);
            modePin.Write(DataMode);
            Write(0x07); // No inversion

            // 7: Power control 1
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR1);
            modePin.Write(DataMode);
            Write(0xA2);
            Write(0x02); // -4.6V
            Write(0x84); // Auto mode

            // 8: Power control 2
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR2);
            modePin.Write(DataMode);
            Write(0xC5); // VGH25 = 2.4C VGSEL = -10 VGH = 3 * AVDD

            // 9: Power control 3
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR3);
            modePin.Write(DataMode);
            Write(0x0A); // Op amp current small
            Write(0x00); // Boost frequency

            // 10: Power control 4
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR4);
            modePin.Write(DataMode);
            Write(0x8A); // BCLK/2, Op amp current small & Medium low
            Write(0x20);

            // 11: Power control 5
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR5);
            modePin.Write(DataMode);
            Write(0x8A);
            Write(0xEE);

            // 12: VM control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.VMCTR1);
            modePin.Write(DataMode);
            Write(0x0E);

            // 13: Inversion control
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.INVOFF); // Inversion off

            // 14: Memory access
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);
            modePin.Write(DataMode);
            Write(0xC8); // Row address, column Address, bottom to top refresh

            // 15: Color mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.COLMOD);
            modePin.Write(DataMode);
            Write(0x05); // TODO: Hard coded to 16-bit color

            /*****************************************
             * Green Tab
             *****************************************/
            if (displayType == ST7735DisplayType.RGreen)
            {
                // Mem address
                colStart = 2;
                rowStart = 1;
            }

            /*****************************************
             * Green Tab Version 1.44
             *****************************************/
            else if (displayType == ST7735DisplayType.RGreen144)
            {
                // Mem Address
                colStart = 2;
                rowStart = 3;
            }

            /*****************************************
             * Red Tab or other
             *****************************************/
            else
            {
                // Mem set
                colStart = rowStart = 0;
            }


            /*****************************************
             * Resume Common
             *****************************************/
            
            // 1: Unknown 1
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRP1);
            modePin.Write(DataMode);
            Write(0x02);
            Write(0x1C);
            Write(0x07);
            Write(0x12);
            Write(0x37);
            Write(0x32);
            Write(0x29);
            Write(0x2D);
            Write(0x29);
            Write(0x25);
            Write(0x2B);
            Write(0x39);
            Write(0x00);
            Write(0x01);
            Write(0x03);
            Write(0x10);

            // 2: Unknown 2
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRN1);
            modePin.Write(DataMode);
            Write(0x03);
            Write(0x1D);
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


            // 3: Normal display on
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.NORON);
            await Task.Delay(10);

            // 4: Main screen turn on
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.DISPON);
            await Task.Delay(100);


            /*****************************************
             * Black
             *****************************************/
             if (displayType == ST7735DisplayType.RBlack)
            {
                // If Black, change MADCTL color filter
                modePin.Write(CommandMode);
                Write((byte)LcdCommand.MADCTL);
                modePin.Write(DataMode);
                Write(0xC0);

            }
        }

        private void InitGpio()
        {
            modePin.SetDriveMode(GpioPinDriveMode.Output);
            resetPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private async Task InitSpiAsync()
        {
            // Create SPI initialization settings
            var settings = new SpiConnectionSettings(chipSelectLine);

            // Use configured clock speed
            settings.ClockFrequency = clockFrequency;

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

        private void SetAddressWindow(byte x0, byte y0, byte x1, byte y1)
        {
            if (displayType == ST7735DisplayType.RGreen)
            {
                // Green tab needs x incremented by 0x02
                x0 += 0x02;
                x1 += 0x02;

                // Green tab needs y incremented by 0x01
                y0 += 0x01;
                y1 += 0x01;
            }

            // 1: Column address set
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.CASET);
            modePin.Write(DataMode);
            Write(0x00);
            Write(x0); // XSTART
            Write(0x00);
            Write(x1); // XEND

            // 2: Row address set
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.RASET);
            modePin.Write(DataMode);
            Write(0x00);
            Write(y0); // YSTART
            Write(0x00);
            Write(y1); // YEND

            // Return to RAM write mode
            SetRamMode();
        }

        private void SetPixel(int x, int y, uint nativeColor) 
        {
            // TODO: Should not be ushort
            ushort uscolor = (ushort)nativeColor;

            if ((x < 0) || (x >= width) || (y < 0) || (y >= height)) return;
            var index = ((y * width) + x) * sizeof(ushort);
            Write(index, (byte)(uscolor >> 8));
            Write(++index, (byte)(uscolor));
        }

        private void SetRamMode()
        {
            // Set to RAM write mode
            modePin.Write(CommandMode);
            Write((byte)LcdCommand.RAMWR);
            modePin.Write(DataMode);
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
        public void ClearScreen(Color col)
        {
            var color = (ushort)GraphicsTools.GetNativeColor(pixelFormat, col);
            var high = (byte)(color >> 8);
            var low = (byte)color;

            var index = 0;

            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;
            displayBuffer[index++] = high;
            displayBuffer[index++] = low;

            Array.Copy(displayBuffer, 0, displayBuffer, 16, 16);
            Array.Copy(displayBuffer, 0, displayBuffer, 32, 32);
            Array.Copy(displayBuffer, 0, displayBuffer, 64, 64);
            Array.Copy(displayBuffer, 0, displayBuffer, 128, 128);
            Array.Copy(displayBuffer, 0, displayBuffer, 256, 256);

            index = 512;
            var line = 0;
            var Half = Height / 2;
            while (++line < Half - 1)
            {
                Array.Copy(displayBuffer, 0, displayBuffer, index, 256);
                index += 256;
            }

            Array.Copy(displayBuffer, 0, displayBuffer, index, displayBuffer.Length / 2);

            if (autoUpdate)
            {
                Update();
            }
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
            var nativeColor = GraphicsTools.GetNativeColor(pixelFormat, color);
            SetPixel(x, y, nativeColor);
            if (autoUpdate) { Update(); }
        }
        public void DrawPixel(int x, int y, byte red, byte green, byte blue)
        {
            var nativeColor = GraphicsTools.GetNativeColor(pixelFormat, red, green, blue);
            SetPixel(x, y, nativeColor);
            if (autoUpdate) { Update(); }
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
        /// Gets or sets the clock frequency that SPI will run at in MHz.
        /// </summary>
        /// <value>
        /// The clock frequency in that SPI will run at in MHz. The default is 9500000.
        /// </value>
        [DefaultValue(DefaultClockFrequency)]
        public int ClockFrequency
        {
            get
            {
                return clockFrequency;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("value");
                if (isInitialized) { throw new IoChangeException(); }
                clockFrequency = value;
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
        /// Gets or sets the type of display connected to the controller.
        /// </summary>
        /// <value>
        /// A <seealso cref="ST7735DisplayType"/> that describes the type of display. 
        /// The default is <see cref="ST7735DisplayType.R"/>.
        /// </value>
        [DefaultValue(ST7735DisplayType.R)]
        public ST7735DisplayType DisplayType
        {
            get
            {
                return displayType;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                displayType = value;
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
