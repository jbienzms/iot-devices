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

namespace Microsoft.IoT.Devices.Pwm
{
    /// <summary>
    /// Driver for the <see href="http://www.adafruit.com/products/815">PCA9685</see> 
    /// 16-Channel 12-bit PWM/Servo Driver.
    /// </summary>
    /// <remarks>
    /// This class is adapted from the original C++ ms-iot sample 
    /// <see href="https://github.com/ms-iot/BusProviders/tree/develop/PWM/PwmPCA9685">here</see>.
    /// </remarks>
    sealed public class PCA9685 : IPwmControllerProvider, IDisposable
    {
        #region Nested Types
        private enum PinBits : byte
        {
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
        };

        private struct PinControlRegister
        {
            public PinControlRegister(PinBits onLow, PinBits onHigh, PinBits offLow, PinBits offHigh)
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
        private const int MAX_FREQUENCY = 1000;
        private const int MIN_FREQUENCY = 40;
        private const int PIN_COUNT = 16;
        private const ushort PULSE_RESOLUTION = 4096;
        private const byte REG_ALL_OFF_H = 0xFD;
        private const byte REG_ALL_OFF_L = 0xFC;
        private const byte REG_ALL_ON_L = 0xFA;
        private const byte REG_ALL_ON_H = 0xFB;
        private const byte REG_MODE1 = 0x0;
        private const byte REG_MODE2 = 0x1;
        private const byte REG_PRESCALE = 0xFE;
        static private readonly byte[] RESET_COMMAND = new byte[] { 0x06 };

        static private readonly PinControlRegister[] PwmPinRegs= new PinControlRegister[16]
        {
            new PinControlRegister(PinBits.PIN0_ON_L, PinBits.PIN0_ON_H, PinBits.PIN0_OFF_L, PinBits.PIN0_OFF_H ),
            new PinControlRegister(PinBits.PIN1_ON_L, PinBits.PIN1_ON_H , PinBits.PIN1_OFF_L , PinBits.PIN1_OFF_H ),
            new PinControlRegister(PinBits.PIN2_ON_L, PinBits.PIN2_ON_H , PinBits.PIN2_OFF_L , PinBits.PIN2_OFF_H ),
            new PinControlRegister(PinBits.PIN3_ON_L, PinBits.PIN3_ON_H , PinBits.PIN3_OFF_L , PinBits.PIN3_OFF_H ),
            new PinControlRegister(PinBits.PIN4_ON_L, PinBits.PIN4_ON_H , PinBits.PIN4_OFF_L , PinBits.PIN4_OFF_H ),
            new PinControlRegister(PinBits.PIN5_ON_L, PinBits.PIN5_ON_H , PinBits.PIN5_OFF_L , PinBits.PIN5_OFF_H ),
            new PinControlRegister(PinBits.PIN6_ON_L, PinBits.PIN6_ON_H, PinBits.PIN6_OFF_L, PinBits.PIN6_OFF_H ),
            new PinControlRegister(PinBits.PIN7_ON_L, PinBits.PIN7_ON_H , PinBits.PIN7_OFF_L , PinBits.PIN7_OFF_H ),
            new PinControlRegister(PinBits.PIN8_ON_L, PinBits.PIN8_ON_H , PinBits.PIN8_OFF_L , PinBits.PIN8_OFF_H ),
            new PinControlRegister(PinBits.PIN9_ON_L, PinBits.PIN9_ON_H , PinBits.PIN9_OFF_L , PinBits.PIN9_OFF_H ),
            new PinControlRegister(PinBits.PIN10_ON_L, PinBits.PIN10_ON_H , PinBits.PIN10_OFF_L , PinBits.PIN10_OFF_H ),
            new PinControlRegister(PinBits.PIN11_ON_L, PinBits.PIN11_ON_H , PinBits.PIN11_OFF_L , PinBits.PIN11_OFF_H ),
            new PinControlRegister(PinBits.PIN12_ON_L, PinBits.PIN12_ON_H , PinBits.PIN12_OFF_L , PinBits.PIN12_OFF_H ),
            new PinControlRegister(PinBits.PIN13_ON_L, PinBits.PIN13_ON_H , PinBits.PIN13_OFF_L , PinBits.PIN13_OFF_H ),
            new PinControlRegister(PinBits.PIN14_ON_L, PinBits.PIN14_ON_H , PinBits.PIN14_OFF_L , PinBits.PIN14_OFF_H ),
            new PinControlRegister(PinBits.PIN15_ON_L, PinBits.PIN15_ON_H , PinBits.PIN15_OFF_L , PinBits.PIN15_OFF_H ),

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
        #endregion // Member Variables

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
            var primarySettings = new I2cConnectionSettings(I2C_PRIMARY_ADDRESS);
            primarySettings.BusSpeed = I2cBusSpeed.FastMode;
            primarySettings.SharingMode = I2cSharingMode.Exclusive;

            // Get the primary device
            primaryDevice = await I2cDevice.FromIdAsync(di.Id, primarySettings);
            if (primaryDevice == null) { throw new DeviceNotFoundException("PCA9685 primary device"); }


            // Connection settings for reset device
            var resetSettings = new I2cConnectionSettings(I2C_PRIMARY_ADDRESS);
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
            writeBuf[0] = REG_PRESCALE;
            writeBuf[1] = DEFAULT_PRESCALE;
            primaryDevice.Write(writeBuf);

            // Set ActualFrequency to default(200Hz)  	
            actualFrequency = Math.Round((CLOCK_FREQUENCY) / (double)((preScale + 1) * PULSE_RESOLUTION));


            writeBuf[0] = REG_ALL_OFF_H;
            writeBuf[1] = 0;
            primaryDevice.Write(writeBuf);

            writeBuf[0] = REG_ALL_ON_H;
            writeBuf[1] = (1 << 4);
            primaryDevice.Write(writeBuf);

            await RestartControllerAsync(0xA1);
        }

        private void ResetController()
        {
            resetDevice.Write(RESET_COMMAND);
        }

        private async Task RestartControllerAsync(byte mode1)
        {
            var writeBuf = new byte[2];

            writeBuf[0] = REG_MODE1;
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
            modeAddr[0] = REG_MODE1;
            primaryDevice.WriteRead(modeAddr, mode);

            // Disable Oscillator  	
            writeBuf[0] = REG_MODE1;
            writeBuf[1] = (byte)(mode[0] | (1 << 4));
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            await Task.Delay(1);

            return mode[0];
        }
        #endregion // Internal Methods

        public void AcquirePin(int pin)
        {
            if ((pin < 0) || (pin > (PIN_COUNT-1))) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            lock (pinAccess)
            {
                if (pinAccess[pin]) { throw new UnauthorizedAccessException(); }
                pinAccess[pin] = true;
            }
        }

        public void DisablePin(int pin)
        {
            if ((pin < 0) || (pin > (PIN_COUNT - 1))) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            // Since we are using the totem-pole mode, we just need to  	
            // make sure that the pin is fully OFF.
            var buffer = new byte[2];
            buffer[0] = PwmPinRegs[pin].OffHigh;
            buffer[1] = 0x1 << 4;
            primaryDevice.Write(buffer);
        }

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

        public void EnablePin(int pin)
        {
            if ((pin < 0) || (pin > (PIN_COUNT - 1))) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            //  	
            // Since we are using the totem-pole mode, we just need to  	
            // make sure that the pin is not fully OFF(bit 4 of LEDn_OFF_H should be zero).  	
            // We set the OFF and ON counter to zero so that the pin is held Low.  	
            // Subsequent calls to SetPulseParameters should set the pulse width.
            var buffer = new byte[5];
            buffer[0] = PwmPinRegs[pin].OnLow;
            buffer[1] = buffer[2] = buffer[3] = buffer[4] = 0x0;
            primaryDevice.Write(buffer);
        }

        public void ReleasePin(int pin)
        {
            if ((pin < 0) || (pin > (PIN_COUNT - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pinAccess)
            {
                pinAccess[pin] = false;
            }
        }

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
            buffer[0] = REG_PRESCALE;
            buffer[1] = preScale;
            primaryDevice.Write(buffer);

            // Restart  	
            TaskExtensions.UISafeWait(RestartControllerAsync, mode1);

            return actualFrequency;
        }

        public void SetPulseParameters(int pin, double dutyCycle, bool invertPolarity)
        {
            if ((pin < 0) || (pin > (PIN_COUNT - 1))) throw new ArgumentOutOfRangeException("pin");

            // Make sure we're initialized
            TaskExtensions.UISafeWait(EnsureInitializedAsync);

            var buffer = new byte[5];
            ushort onRatio = (ushort)Math.Round(dutyCycle * (PULSE_RESOLUTION - 1));

            // Set the initial Address. AI flag is ON and hence  	
            // address will auto-increment after each byte.
            buffer[0] = PwmPinRegs[pin].OnLow;

            if (invertPolarity)
            {
                onRatio = (ushort)(PULSE_RESOLUTION - onRatio);
                buffer[1] = (byte)(onRatio & 0xFF);
                buffer[2] = (byte)((onRatio & 0xFF00) >> 8);
                buffer[3] = buffer[4] = 0;
            }
            else
            {
                buffer[1] = buffer[2] = 0;
                buffer[3] = (byte)(onRatio & 0xFF);
                buffer[4] = (byte)((onRatio & 0xFF00) >> 8);
            }
            primaryDevice.Write(buffer);
        }


        #region Public Properties
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

        public double MaxFrequency
        {
            get
            {
                return MAX_FREQUENCY;
            }
        }

        public double MinFrequency
        {
            get
            {
                return MIN_FREQUENCY;
            }
        }

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
