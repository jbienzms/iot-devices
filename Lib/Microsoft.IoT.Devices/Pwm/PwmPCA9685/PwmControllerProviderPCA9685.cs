// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Devices.Pwm.Provider;
using Windows.Foundation;
using Microsoft.IoT.DeviceCore;
using TaskExtensions = Microsoft.IoT.DeviceHelpers.TaskExtensions;
using Microsoft.IoT.DeviceHelpers;
using System.Diagnostics;

namespace Microsoft.IoT.Devices.Pwm.PwmPCA9685
{
    /// <summary>
    /// Initializes a new <see cref="PwmControllerProviderPCA9685"/> instance.
    /// </summary>
    /// <remarks>
    /// This class is adapted from the original C++ ms-iot sample 
    /// <see href="https://github.com/ms-iot/BusProviders/tree/develop/PWM/PwmPCA9685">here</see>.
    /// And SimulatedProvider code <see href="https://github.com/ms-iot/BusProviders/blob/develop/SimulatedProvider/SimulatedProvider/PwmControllerProvider.cs">here</see>/>
    /// <br/>
    /// Driver for the <see href="http://www.adafruit.com/products/815">16-Channel 12-bit PWM/Servo</see> 
    /// PCA9685 Driver and others.
    /// </remarks>
    public sealed class PwmControllerProviderPCA9685 : IPwmControllerProvider, IDisposable
    {
        #region Nested Types
        /// <summary>
        /// PCA9685 register addresses
        /// </summary>
        private enum Registers : byte
        {
            MODE1 = 0x00,
            MODE2 = 0x01,
            SUBADR1 = 0x02,
            SUBADR2 = 0x03,
            SUBADR3 = 0x04,
            ALLCALLADR = 0x05,
            PIN0_ON_L = 0x06,
            PIN0_ON_H = 0x07,
            PIN0_OFF_L = 0x08,
            PIN0_OFF_H = 0x09,
            PIN1_ON_L = 0x0A,
            PIN1_ON_H = 0x0B,
            PIN1_OFF_L = 0x0C,
            PIN1_OFF_H = 0x0D,
            PIN2_ON_L = 0x0E,
            PIN2_ON_H = 0x0F,
            PIN2_OFF_L = 0x10,
            PIN2_OFF_H = 0x11,
            PIN3_ON_L = 0x12,
            PIN3_ON_H = 0x13,
            PIN3_OFF_L = 0x14,
            PIN3_OFF_H = 0x15,
            PIN4_ON_L = 0x16,
            PIN4_ON_H = 0x17,
            PIN4_OFF_L = 0x18,
            PIN4_OFF_H = 0x19,
            PIN5_ON_L = 0x1A,
            PIN5_ON_H = 0x1B,
            PIN5_OFF_L = 0x1C,
            PIN5_OFF_H = 0x1D,
            PIN6_ON_L = 0x1E,
            PIN6_ON_H = 0x1F,
            PIN6_OFF_L = 0x20,
            PIN6_OFF_H = 0x21,
            PIN7_ON_L = 0x22,
            PIN7_ON_H = 0x23,
            PIN7_OFF_L = 0x24,
            PIN7_OFF_H = 0x25,
            PIN8_ON_L = 0x26,
            PIN8_ON_H = 0x27,
            PIN8_OFF_L = 0x28,
            PIN8_OFF_H = 0x29,
            PIN9_ON_L = 0x2A,
            PIN9_ON_H = 0x2B,
            PIN9_OFF_L = 0x2C,
            PIN9_OFF_H = 0x2D,
            PIN10_ON_L = 0x2E,
            PIN10_ON_H = 0x2F,
            PIN10_OFF_L = 0x30,
            PIN10_OFF_H = 0x31,
            PIN11_ON_L = 0x32,
            PIN11_ON_H = 0x33,
            PIN11_OFF_L = 0x34,
            PIN11_OFF_H = 0x35,
            PIN12_ON_L = 0x36,
            PIN12_ON_H = 0x37,
            PIN12_OFF_L = 0x38,
            PIN12_OFF_H = 0x39,
            PIN13_ON_L = 0x3A,
            PIN13_ON_H = 0x3B,
            PIN13_OFF_L = 0x3C,
            PIN13_OFF_H = 0x3D,
            PIN14_ON_L = 0x3E,
            PIN14_ON_H = 0x3F,
            PIN14_OFF_L = 0x40,
            PIN14_OFF_H = 0x41,
            PIN15_ON_L = 0x42,
            PIN15_ON_H = 0x43,
            PIN15_OFF_L = 0x44,
            PIN15_OFF_H = 0x45,
            ALL_LED_ON_L = 0xFA,
            ALL_LED_ON_H = 0xFB,
            ALL_LED_OFF_L = 0xFC,
            ALL_LED_OFF_H = 0xFD,
            PRESCALE = 0xFE,
        };

        /// <summary>
        /// PCA9685 mode 1 register flags
        /// </summary>
        [Flags]
        private enum Mode1Flags : byte
        {                           // * denoted power-on state
            RESTART = 0x80,         // Restart: 0*: disabled, 1:eneabled
            EXTCLK = 0x40,          // clock source: 0*: internal, 1: external
            AI = 0x20,              // register auto-increment: *0: disabled, 1: enabled
            SLEEP = 0x10,           // mode: 0: normal, *1: low-power
            SUB1 = 0x08,            // subaddress 1: *0: disabled, 1: enabled
            SUB2 = 0x04,            // subaddress 2: *0: disabled, 1: enabled
            SUB3 = 0x02,            // subaddress 3: *0: disabled, 1: enabled
            ALLCALL = 0x01,         // all call address: 0: disabled, *1: enabled
        }                           // power-on state of this register is 0x11 (00010001)

        /// <summary>
        /// PCA9685 mode 2 register flags
        /// </summary>
        [Flags]
        private enum Mode2Flags : byte
        {                           // * denoted power-on state
            RESERVED1 = 0x80,       // Reserved *0
            RESERVED2 = 0x40,       // Reserved *0
            RESERVED3 = 0x20,       // Reserved *0
            INVRT = 0x10,           // When output drivers enabled, *0: not inverted, 1: inverted
            OCH = 0x08,             // Outputs change on: *0: STOP command, 1: ACK command
            OUTDRV = 0x04,          // Output type: 0: open drain, *1: totem pole
            // *00: When output drivers disabled, LEDn=0
            // 01: When output drivers disabled, LEDn=1 when OUTDRV=1, otherwise high-impedence
            // 1x: When output drivers disabled, LEDn=high-impedence
            OUTNE1 = 0x02,
            OUTNE0 = 0x01,
        }                           // power-on state of this register is 0x04 (00000100)

        private struct PinControlRegister
        {
            public PinControlRegister(Registers onLow, Registers onHigh, Registers offLow, Registers offHigh)
            {
                OnLow = (byte)onLow;
                OnHigh = (byte)onHigh;
                OffLow = (byte)offLow;
                OffHigh = (byte)offHigh;
            }
            public byte OnLow;
            public byte OnHigh;
            public byte OffLow;
            public byte OffHigh;
        };
        #endregion // Nested Types

        #region Constants
        private const double CLOCK_FREQUENCY = 25000000;
        private const byte DEFAULT_PRESCALE = 0x1E;
        private const int I2C_PRIMARY_ADDRESS = 0x40;
        private const int I2C_RESET_ADDRESS = 0x0;
        private const int I2C_ALL_CALL_ADDRESS = 0xE0;      // Default ALLCALL address for all PCA9685 on I2C bus
        private const int MAX_FREQUENCY = 1526;
        private const int MIN_FREQUENCY = 24;
        private const int PIN_COUNT = 16;                   // 16 LED outputs
        private const ushort PULSE_RESOLUTION = 4096;       // 12-bit resolution
        static private readonly byte[] RESET_COMMAND = new byte[] { 0x06 };

        static private readonly PinControlRegister[] PwmPinRegs = new PinControlRegister[16]
        {
            new PinControlRegister(Registers.PIN0_ON_L, Registers.PIN0_ON_H, Registers.PIN0_OFF_L, Registers.PIN0_OFF_H ),
            new PinControlRegister(Registers.PIN1_ON_L, Registers.PIN1_ON_H , Registers.PIN1_OFF_L , Registers.PIN1_OFF_H ),
            new PinControlRegister(Registers.PIN2_ON_L, Registers.PIN2_ON_H , Registers.PIN2_OFF_L , Registers.PIN2_OFF_H ),
            new PinControlRegister(Registers.PIN3_ON_L, Registers.PIN3_ON_H , Registers.PIN3_OFF_L , Registers.PIN3_OFF_H ),
            new PinControlRegister(Registers.PIN4_ON_L, Registers.PIN4_ON_H , Registers.PIN4_OFF_L , Registers.PIN4_OFF_H ),
            new PinControlRegister(Registers.PIN5_ON_L, Registers.PIN5_ON_H , Registers.PIN5_OFF_L , Registers.PIN5_OFF_H ),
            new PinControlRegister(Registers.PIN6_ON_L, Registers.PIN6_ON_H, Registers.PIN6_OFF_L, Registers.PIN6_OFF_H ),
            new PinControlRegister(Registers.PIN7_ON_L, Registers.PIN7_ON_H , Registers.PIN7_OFF_L , Registers.PIN7_OFF_H ),
            new PinControlRegister(Registers.PIN8_ON_L, Registers.PIN8_ON_H , Registers.PIN8_OFF_L , Registers.PIN8_OFF_H ),
            new PinControlRegister(Registers.PIN9_ON_L, Registers.PIN9_ON_H , Registers.PIN9_OFF_L , Registers.PIN9_OFF_H ),
            new PinControlRegister(Registers.PIN10_ON_L, Registers.PIN10_ON_H , Registers.PIN10_OFF_L , Registers.PIN10_OFF_H ),
            new PinControlRegister(Registers.PIN11_ON_L, Registers.PIN11_ON_H , Registers.PIN11_OFF_L , Registers.PIN11_OFF_H ),
            new PinControlRegister(Registers.PIN12_ON_L, Registers.PIN12_ON_H , Registers.PIN12_OFF_L , Registers.PIN12_OFF_H ),
            new PinControlRegister(Registers.PIN13_ON_L, Registers.PIN13_ON_H , Registers.PIN13_OFF_L , Registers.PIN13_OFF_H ),
            new PinControlRegister(Registers.PIN14_ON_L, Registers.PIN14_ON_H , Registers.PIN14_OFF_L , Registers.PIN14_OFF_H ),
            new PinControlRegister(Registers.PIN15_ON_L, Registers.PIN15_ON_H , Registers.PIN15_OFF_L , Registers.PIN15_OFF_H ),

        };

        #endregion // Constants

        #region Member Variables
        private double actualFrequency;
        private string controllerName = "I2C1"; // The name of the I2C controller to use
        private bool isInitialized;
        private byte preScale = DEFAULT_PRESCALE;
        private bool[] pinAccess = new bool[PIN_COUNT];
        private I2cDevice primaryDevice;
        private I2cDevice resetDevice;
        private int i2cAddress;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PwmControllerProviderPCA9685"/> instance.
        /// </summary>
        /// <param name="address">Optional I2C address parameter. Defaut is 0x40.</param>
        internal PwmControllerProviderPCA9685(byte address = I2C_PRIMARY_ADDRESS)
        {
            this.i2cAddress = address;
        }
        #endregion // Constructors

        #region Internal Methods
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Validate
            if (string.IsNullOrWhiteSpace(controllerName)) { throw new MissingIoException(nameof(ControllerName)); }

            // Get a query for I2C
            var aqs = I2cDevice.GetDeviceSelector(controllerName);

            // Find the first I2C device
            var di = (await DeviceInformation.FindAllAsync(aqs)).FirstOrDefault();

            // Make sure we found an I2C device
            if (di == null) { throw new DeviceNotFoundException(controllerName); }

            // Connection settings for primary device
            var primarySettings = new I2cConnectionSettings(this.i2cAddress);
            primarySettings.BusSpeed = I2cBusSpeed.FastMode;
            primarySettings.SharingMode = I2cSharingMode.Exclusive;

            // Get the primary device
            primaryDevice = await I2cDevice.FromIdAsync(di.Id, primarySettings);
            if (primaryDevice == null) { throw new DeviceNotFoundException("PCA9685 primary device"); }

            // Connection settings for reset device
            var resetSettings = new I2cConnectionSettings(this.i2cAddress);
            resetSettings.SlaveAddress = I2C_RESET_ADDRESS;

            // Get the reset device
            resetDevice = await I2cDevice.FromIdAsync(di.Id, resetSettings);
            if (resetDevice == null) { throw new DeviceNotFoundException("PCA9685 reset device"); }

            // Initialize the controller
            await InitializeControllerAsync();

            // Done initializing
            isInitialized = true;
        }

        private async Task InitializeControllerAsync()
        {
            if (primaryDevice == null) return;

            ResetController();

            var writeBuf = new byte[2];

            await SleepControllerAsync();

            // Set PRE_SCALE to default
            writeBuf[0] = (byte)Registers.PRESCALE;
            writeBuf[1] = DEFAULT_PRESCALE;
            primaryDevice.Write(writeBuf);

            // Set ActualFrequency to default(200Hz)
            actualFrequency = Math.Round((CLOCK_FREQUENCY) / (double)((preScale + 1) * PULSE_RESOLUTION));

            // Turn them all on - Why are we turning them all on. Seems like we should be turning them off.
            writeBuf[0] = (byte)Registers.ALL_LED_OFF_H;
            writeBuf[1] = 0;
            primaryDevice.Write(writeBuf);
            writeBuf[0] = (byte)Registers.ALL_LED_ON_H;
            writeBuf[1] = (1 << 4);             // 0x10 = LED FULL ON
            primaryDevice.Write(writeBuf);

            await RestartControllerAsync((byte)(Mode1Flags.RESTART | Mode1Flags.AI | Mode1Flags.ALLCALL));
        }

        private void ResetController()
        {
            resetDevice.Write(RESET_COMMAND);
        }

        private async Task RestartControllerAsync(byte mode1)
        {
            var writeBuf = new byte[2];

            writeBuf[0] = (byte)Registers.MODE1;
            writeBuf[1] = mode1;
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            await Task.Delay(1);
        }

        private async Task<byte> SleepControllerAsync()
        {
            var writeBuf = new byte[2];
            var mode = new byte[1];
            var modeAddr = new byte[1];

            // Read MODE1 register  	
            modeAddr[0] = (byte)Registers.MODE1;
            primaryDevice.WriteRead(modeAddr, mode);

            // Disable Oscillator  	
            writeBuf[0] = (byte)Registers.MODE1;
            writeBuf[1] = (byte)(mode[0] | (byte)Mode1Flags.SLEEP);
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            await Task.Delay(1);

            return mode[0];
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public void AcquirePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Acquiring pin {0}", pin);

            lock (pinAccess)
            {
                if (pinAccess[pin]) { throw new UnauthorizedAccessException(); }
                pinAccess[pin] = true;
            }
        }

        /// <inheritdoc/>
        public void DisablePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Disabling pin {0}", pin);

            // Since we are using the totem-pole mode, we just need to  	
            // make sure that the pin is fully OFF.

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            // Stop PWM signal on the specified pin

            // Read the OffHigh register
            var addr = new byte[1];
            var msb = new byte[1];
            addr[0] = PwmPinRegs[pin].OffHigh;
            primaryDevice.WriteRead(addr, msb);

            // Make sure that bit 4 of LEDn_OFF_H is 1 (pin disabled = fully off)
            var buffer = new byte[2];
            buffer[0] = PwmPinRegs[pin].OffHigh;
            buffer[1] = (byte)(msb[0] | (0x10));
            primaryDevice.Write(buffer);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (resetDevice != null)
            {
                resetDevice.Dispose();
                resetDevice = null;
            }
            if (primaryDevice != null)
            {
                primaryDevice.Dispose();
                primaryDevice = null;
            }
            pinAccess = null;
        }

        /// <inheritdoc/>
        public void EnablePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //Debug.WriteLine("PwmControllerProviderPCA9685: Enabling pin {0}", pin);

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            // Start PWM signal on the specified pin

            // Read the OffHigh register
            var addr = new byte[1];
            var msb = new byte[1];
            addr[0] = PwmPinRegs[pin].OffHigh;
            primaryDevice.WriteRead(addr, msb);

            // Make sure that bit 4 of LEDn_OFF_H is 0 (pin enabled = 12-bit PWM active)
            var buffer = new byte[2];
            buffer[0] = PwmPinRegs[pin].OffHigh;
            buffer[1] = (byte)(msb[0] & ~(0x1 << 4));   // Mask off bit 4
            primaryDevice.Write(buffer);
        }

        /// <inheritdoc/>
        public void ReleasePin(int pin)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");

            //Debug.WriteLine("PwmControllerProviderPCA9685: Releasing pin {0}", pin);

            lock (pinAccess)
            {
                if (!pinAccess[pin])
                    throw new InvalidOperationException("Pin is not acquired");
                pinAccess[pin] = false;
            }
        }

        /// <inheritdoc/>
        public double SetDesiredFrequency(double frequency)
        {
            if (frequency < MIN_FREQUENCY || frequency > MAX_FREQUENCY)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency));
            }

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            preScale = (byte)(Math.Round((CLOCK_FREQUENCY / (frequency * PULSE_RESOLUTION))) - 1);
            actualFrequency = CLOCK_FREQUENCY / (double)((preScale + 1) * PULSE_RESOLUTION);

            byte mode1 = TaskExtensions.UISafeWait(SleepControllerAsync);

            var buffer = new byte[2];
            // Set PRE_SCALE  	
            buffer[0] = (byte)Registers.PRESCALE;
            buffer[1] = preScale;
            primaryDevice.Write(buffer);

            // Restart  	
            TaskExtensions.UISafeWait(RestartControllerAsync, mode1);

            return actualFrequency;
        }

        /// <inheritdoc/>
        public void SetPulseParameters(int pin, double dutyCycle, bool invertPolarity)
        {
            if ((pin < 0) || (pin >= PIN_COUNT)) throw new ArgumentOutOfRangeException("pin");
            if ((dutyCycle < 0) || (dutyCycle > 1)) throw new ArgumentOutOfRangeException("dutyCycle");

            //Debug.WriteLine("PwmControllerProviderPCA9685: Setting {0} pin duty cycle to {1}", pin, dutyCycle);

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            if (!pinAccess[pin])
                throw new InvalidOperationException("Pin is not acquired");

            var buffer = new byte[5];
            ushort onRatio = (ushort)Math.Round(dutyCycle * (PULSE_RESOLUTION - 1));

            // Read the OffHigh register
            var addr = new byte[1];
            var msb = new byte[4];
            addr[0] = PwmPinRegs[pin].OnLow;
            primaryDevice.WriteRead(addr, msb);

            // Set the initial Address. AI flag is ON and hence  	
            // address will auto-increment after each byte.
            buffer[0] = PwmPinRegs[pin].OnLow;

            if (invertPolarity)
            {
                onRatio = (ushort)(PULSE_RESOLUTION - onRatio);
                buffer[1] = (byte)(onRatio & 0xFF);
                buffer[2] = (byte)((onRatio & 0x0F00) >> 8);
                buffer[3] = 0;
                buffer[4] = 0;
            }
            else
            {
                buffer[1] = 0;
                buffer[2] = 0;
                buffer[3] = (byte)(onRatio & 0xFF);
                buffer[4] = (byte)((onRatio & 0x0F00) >> 8);
            }
            // Make sure special full ON and full OFF bits are maintained
            buffer[2] = (byte)(buffer[2] | (msb[1] & 0x10));
            buffer[4] = (byte)(buffer[4] | (msb[3] & 0x10));
            primaryDevice.Write(buffer);
        }
        #endregion // Public Methods

        #region Public Properties

        /// <summary>
        /// The I2C address of this controller
        /// </summary>
        public int I2CAddress
        {
            get
            {
                return this.i2cAddress;
            }
        }

        /// <inheritdoc/>
        public double ActualFrequency
        {
            get
            {
                return actualFrequency;
            }
        }

        /// <summary>
        /// Gets or sets the name of the I2C controller to use.
        /// </summary>
        /// <value>
        /// The name of the I2C controller to use. The default is "I2C1".
        /// </value>
        [DefaultValue("I2C1")]
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
        public double MaxFrequency
        {
            get
            {
                return MAX_FREQUENCY;
            }
        }

        /// <inheritdoc/>
        public double MinFrequency
        {
            get
            {
                return MIN_FREQUENCY;
            }
        }

        /// <inheritdoc/>
        public int PinCount
        {
            get
            {
                return PIN_COUNT;
            }
        }
        #endregion // Public Properties
    }
}
