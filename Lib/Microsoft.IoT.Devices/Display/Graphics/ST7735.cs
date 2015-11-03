// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Microsoft.IoT.DeviceCore.Display;
using Windows.Devices.Enumeration;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI;
using Microsoft.IoT.DeviceHelpers;

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

        private enum MirrorCommand
        {
            MADCTL_MY = 0x80,
            MADCTL_MX = 0x40,
            MADCTL_MV = 0x20,
            MADCTL_ML = 0x10,
            MADCTL_RGB = 0x00,
            MADCTL_BGR = 0x08,
            MADCTL_MH = 0x04,
        }

        static private readonly GpioPinValue CommandMode = GpioPinValue.Low;
        static private readonly GpioPinValue DataMode = GpioPinValue.High;
        private const int DefaultClockFrequency = 9500000;
        #endregion // Constants

        #region Member Variables
        private bool autoUpdate = true;         // Does the display automatically update after various drawing functions
        private int chipSelectLine = 0;         // The chip select line used on the SPI controller
        private int clockFrequency = DefaultClockFrequency;   // The clock speed SPI will run at
        // private int colStart;
        private string controllerName = "SPI0"; // The name of the SPI controller to use
        private byte[] displayBuffer;           // In memory allocation for display
        private ST7735DisplayType displayType = ST7735DisplayType.R; // Display type (version and color tab)
        private int height = 160;
        private bool isInitialized;
        private GpioPin dataCommandPin;                // Switches between command and data
        private DisplayOrientations orientation = DisplayOrientations.Portrait;
        private DisplayPixelFormat pixelFormat = DisplayPixelFormat.Rgb565;
        private GpioPin resetPin;               // Resets the display
        // int rowStart;
        private SpiDevice spiDevice;            // The SPI device the display is connected to
        private readonly byte[] spiByte = new byte[1]; // The allocated memory for a single byte command
        private int width = 128;
        #endregion // Member Variables

        #region Internal Methods
        private void ClearScreen(Color col)
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

        /// <summary>
        /// Initializes the display.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
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
            if (dataCommandPin == null) { throw new MissingIoException(nameof(DataCommandPin)); }
            if (resetPin == null) { throw new MissingIoException(nameof(ResetPin)); }

            // GPIO
            InitGpio();

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

            // If the orientation is not portrait we need to update orientation
            if (orientation != DisplayOrientations.Portrait)
            {
                // Set orientation, do not flip
                // Note, this also reverts to RAM mode when done
                SetOrientation(false);
            }

            // Set address window to full size of the display
            // Note, this also reverts to RAM mode when done
            SetAddressWindow(0, 0, (byte)(width - 1), (byte)(height - 1));
        }

        private async Task InitDisplayBAsync()
        {
            // 1: Software reset
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.SWRESET);
            await Task.Delay(50);

            // 2: Out of sleep mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.SLPOUT);
            await Task.Delay(500);

            // 3: Color mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.COLMOD);
            dataCommandPin.Write(DataMode);
            Write(0x05); // TODO: This is 16-bit color. Support additional display types.
            await Task.Delay(10);

            // 4: Frame rate control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR1);
            dataCommandPin.Write(DataMode);
            Write(0x00); // Fastest Refresh
            Write(0x06); // 6 lines front porch
            Write(0x03); // 3 lines back porch

            // 5: Memory access control (directions)
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);
            dataCommandPin.Write(DataMode);
            Write(0x08); // Row address, column address, bottom to top refresh

            // 6: Display settings #5
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.DISSET5);
            dataCommandPin.Write(DataMode);
            Write(0x15); // 1 clock cycle no overlap, 2 cycle gate
            Write(0x02); // Fix on VTL

            // 7: Display inversion control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.INVCTR);
            dataCommandPin.Write(DataMode);
            Write(0x00); // Line inversion

            // 8: Power control 1
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR1);
            dataCommandPin.Write(DataMode);
            Write(0x02); // 4.7v // TODO: What other options???
            Write(0x70); // 1.0uA
            await Task.Delay(10);

            // 9: Power control 2
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR2);
            dataCommandPin.Write(DataMode);
            Write(0x05); // VGH = 14.7v, VTL = -7.35v // TODO: What other options???

            // 10: Power control 3
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR3);
            dataCommandPin.Write(DataMode);
            Write(0x01); // Op amp current small
            Write(0x02); // Boost frequency

            // 11: Voltage control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.VMCTR1);
            dataCommandPin.Write(DataMode);
            Write(0x01); // VCOMH = 4v // TODO: What other options???
            Write(0x02); // VCOML = -1.1v // TODO: What other options???
            await Task.Delay(10);

            // 12: Power control 6
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR6);
            dataCommandPin.Write(DataMode);
            Write(0x11); // ?
            Write(0x15); // ?

            // 13: Unknown 1
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRP1);
            dataCommandPin.Write(DataMode);
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRN1);
            dataCommandPin.Write(DataMode);
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.CASET);
            dataCommandPin.Write(DataMode);
            Write(0x00);
            Write(0x02); // XSTART = 2
            Write(0x00);
            Write(0x81); // XEND = 129

            // 16: Row address set
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.RASET);
            dataCommandPin.Write(DataMode);
            Write(0x00);
            Write(0x02); // YSTART = 2
            Write(0x00);
            Write(0x81); // YEND = 129 // TODO: Doesn't look right

            // 17: Normal display on
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.NORON);
            await Task.Delay(10);

            // 18: Main screen turn on
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.DISPON);
            await Task.Delay(500);
        }


        private async Task InitDisplayRAsync()
        {
            /*****************************************
             * Common
             *****************************************/

            // 1: Software reset
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.SWRESET);
            await Task.Delay(150);

            // 2: Out of sleep mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.SLPOUT);
            dataCommandPin.Write(DataMode);
            await Task.Delay(500);

            // 3: Frame rate control - normal mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR1);
            dataCommandPin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Rate = fosc/(1x2+40) * (LINE+2C+2D)
            Write(0x2D); // ***********************************

            // 4: Frame rate control - idle mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR2);
            dataCommandPin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Rate = fosc/(1x2+40) * (LINE+2C+2D)
            Write(0x2D); // ***********************************

            // 5: Frame rate control - partial mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.FRMCTR3);
            dataCommandPin.Write(DataMode);
            Write(0x01); // ***********************************
            Write(0x2C); // Dot inversion mode
            Write(0x2D); // ***********************************
            Write(0x01); // ***********************************
            Write(0x2C); // Line inversion mode
            Write(0x2D); // ***********************************

            // 6: Display inversion control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.INVCTR);
            dataCommandPin.Write(DataMode);
            Write(0x07); // No inversion

            // 7: Power control 1
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR1);
            dataCommandPin.Write(DataMode);
            Write(0xA2);
            Write(0x02); // -4.6V
            Write(0x84); // Auto mode

            // 8: Power control 2
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR2);
            dataCommandPin.Write(DataMode);
            Write(0xC5); // VGH25 = 2.4C VGSEL = -10 VGH = 3 * AVDD

            // 9: Power control 3
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR3);
            dataCommandPin.Write(DataMode);
            Write(0x0A); // Op amp current small
            Write(0x00); // Boost frequency

            // 10: Power control 4
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR4);
            dataCommandPin.Write(DataMode);
            Write(0x8A); // BCLK/2, Op amp current small & Medium low
            Write(0x20);

            // 11: Power control 5
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.PWCTR5);
            dataCommandPin.Write(DataMode);
            Write(0x8A);
            Write(0xEE);

            // 12: VM control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.VMCTR1);
            dataCommandPin.Write(DataMode);
            Write(0x0E);

            // 13: Inversion control
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.INVOFF); // Inversion off

            // 14: Memory access
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);
            dataCommandPin.Write(DataMode);
            Write(0xC8); // Row address, column Address, bottom to top refresh

            // 15: Color mode
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.COLMOD);
            dataCommandPin.Write(DataMode);
            Write(0x05); // TODO: Hard coded to 16-bit color

            /*
            ///////////////////////////////////////////
            // Green Tab
            ///////////////////////////////////////////
            if (displayType == ST7735DisplayType.RGreen)
            {
                // Mem address
                colStart = 2;
                rowStart = 1;
            }

            ///////////////////////////////////////////
            // Green Tab Version 1.44
            ///////////////////////////////////////////
            else if (displayType == ST7735DisplayType.RGreen144)
            {
                // Mem Address
                colStart = 2;
                rowStart = 3;
            }

            ///////////////////////////////////////////
            // Red Tab or other
            ///////////////////////////////////////////
            else
            {
                // Mem set
                colStart = rowStart = 0;
            }
            */

            ///////////////////////////////////////////
            // Resume Common
            ///////////////////////////////////////////

            // 1: Unknown 1
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRP1);
            dataCommandPin.Write(DataMode);
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.GMCTRN1);
            dataCommandPin.Write(DataMode);
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.NORON);
            await Task.Delay(10);

            // 4: Main screen turn on
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.DISPON);
            await Task.Delay(100);


            ///////////////////////////////////////////
            // Black
            ///////////////////////////////////////////
            if (displayType == ST7735DisplayType.RBlack)
            {
                // If Black, change MADCTL color filter
                dataCommandPin.Write(CommandMode);
                Write((byte)LcdCommand.MADCTL);
                dataCommandPin.Write(DataMode);
                Write(0xC0);

            }
        }

        private void InitGpio()
        {
            dataCommandPin.SetDriveMode(GpioPinDriveMode.Output);
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.CASET);
            dataCommandPin.Write(DataMode);
            Write(0x00);
            Write(x0); // XSTART
            Write(0x00);
            Write(x1); // XEND

            // 2: Row address set
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.RASET);
            dataCommandPin.Write(DataMode);
            Write(0x00);
            Write(y0); // YSTART
            Write(0x00);
            Write(y1); // YEND

            // Return to RAM write mode
            SetRamMode();
        }

        private void SetOrientation(bool flipWidthAndHeight)
        {
            // MV - Vertical Addressing Mode
            // MX - Mirror X
            // MY - Mirror Y
            bool mv = false;
            bool mx = false;
            bool my = false;

            switch (orientation)
            {
                case DisplayOrientations.Landscape: // -90 degrees for this display
                    // 1 = Mirror Y and Vertical Addressing
                    my = true;
                    mv = true;
                    break;
                case DisplayOrientations.LandscapeFlipped: // +90 degrees for this display
                    // 3 = Mirror X and Vertical Addressing
                    mx = true;
                    mv = true;
                    break;
                case DisplayOrientations.Portrait: // 0 degrees for this display
                    // 2 = No Mirror
                    // Default - all false from above
                    break;
                case DisplayOrientations.PortraitFlipped: // +180 degrees for this display
                    // 0 = Mirror X and Mirror Y
                    mx = true; // TODO: Why does PortraitFlipped not work when the rest do...
                    my = true;
                    break;
            }

            // Send command
            DataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.MADCTL);

            // Compute data
            byte data = (byte)(displayType == ST7735DisplayType.RBlack ? MirrorCommand.MADCTL_RGB : MirrorCommand.MADCTL_BGR);
            if (mx) { data |= (byte)MirrorCommand.MADCTL_MX; }
            if (my) { data |= (byte)MirrorCommand.MADCTL_MY; }
            if (mv) { data |= (byte)MirrorCommand.MADCTL_MV; }

            // Send data
            DataCommandPin.Write(DataMode);
            Write(data);

            // Flip?
            if (flipWidthAndHeight)
            {
                // Flip
                var ow = width;
                width = height;
                height = ow;

                // Set address mode which also goes back to RAM when done
                SetAddressWindow(0, 0, (byte)(width - 1), (byte)(height - 1));
            }
            else
            {
                // No flip, just go back to RAM mode
                SetRamMode();
            }
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
            dataCommandPin.Write(CommandMode);
            Write((byte)LcdCommand.RAMWR);
            dataCommandPin.Write(DataMode);
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

        /// <inheritdoc/>
        public void Clear()
        {
            ClearScreen(Windows.UI.Colors.Black);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (dataCommandPin != null)
            {
                dataCommandPin.Dispose();
                dataCommandPin = null;
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

        /// <inheritdoc/>
        public void DrawPixel(int x, int y, Color color)
        {
            var nativeColor = GraphicsTools.GetNativeColor(pixelFormat, color);
            SetPixel(x, y, nativeColor);
            if (autoUpdate) { Update(); }
        }
        
        /// <inheritdoc/>
        public void DrawPixel(int x, int y, byte red, byte green, byte blue)
        {
            var nativeColor = GraphicsTools.GetNativeColor(pixelFormat, red, green, blue);
            SetPixel(x, y, nativeColor);
            if (autoUpdate) { Update(); }
        }

        /// <inheritdoc/>
        public bool IsOrientationSupported(DisplayOrientations orientation)
        {
            return true;
        }

        /// <inheritdoc/>
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
        /// Gets or sets the pin used to change between sending data and commands.
        /// </summary>
        /// <value>
        /// The pin used to change between sending data and commands.
        /// </value>
        /// <remarks>
        /// This pin is usually marked 'DC' or 'RS'.
        /// </remarks>
        public GpioPin DataCommandPin
        {
            get
            {
                return dataCommandPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                dataCommandPin = value;
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
        /// Gets or sets the orientation of the display.
        /// </summary>
        /// <value>
        /// A <see cref="DisplayOrientations"/> that specifies the orientation. 
        /// The default is <see cref="DisplayOrientations.Portrait"/>.
        /// </value>
        [DefaultValue(DisplayOrientations.Portrait)]
        public DisplayOrientations Orientation
        {
            get
            {
                return orientation;
            }
            set
            {
                // Make sure changing
                if (value != orientation)
                {
                    // Validate
                    if (value == DisplayOrientations.None) { throw new ArgumentOutOfRangeException("value"); }

                    // Hold onto old
                    var oldOrientation = orientation;

                    // Update
                    orientation = value;

                    // If already initialized, update actual display
                    if (isInitialized)
                    {
                        // Need to flip?
                        bool flip = GraphicsTools.IsAspectChanging(oldOrientation: oldOrientation, newOrientation: value);
                        SetOrientation(flipWidthAndHeight: flip);
                    }
                }
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
