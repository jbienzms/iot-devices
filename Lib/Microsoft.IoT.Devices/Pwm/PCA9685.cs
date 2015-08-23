// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Devices.Pwm.Provider;

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
    public class PCA9685 : IPwmControllerProvider, IDisposable
    {
        #region Constants
        private const double CLOCK_FREQUENCY = 25000000;
        private const byte DEFAULT_PRESCALE = 0x1E;
        private const int I2C_PRIMARY_ADDRESS = 0x40;
        private const int I2C_RESET_ADDRESS = 0x0;
        private const int MAX_FREQUENCY = 1000;
        private const int MIN_FREQUENCY = 40;
        private const int PIN_COUNT = 16;
        private const int PULSE_RESOLUTION = 4096;
        private const byte REG_ALL_OFF_H = 0xFD;
        private const byte REG_ALL_OFF_L = 0xFC;
        private const byte REG_ALL_ON_L = 0xFA;
        private const byte REG_ALL_ON_H = 0xFB;
        private const byte REG_MODE1 = 0x0;
        private const byte REG_MODE2 = 0x1;
        private const byte REG_PRESCALE = 0xFE;
        static private readonly byte[] RESET_COMMAND = new byte[] { 0x06 };
        #endregion // Constants

        #region Member Variables
        private double actualFrequency;
        private bool isInitialized;
        private byte preScale = DEFAULT_PRESCALE;
        private I2cDevice primaryDevice;
        private I2cDevice resetDevice;
        #endregion // Member Variables

        #region Internal Methods
        private async Task EnsureInitializedAsync()
        {
            // If already initialized, done
            if (isInitialized) { return; }

            // Get a query for I2C
            var aqs = I2cDevice.GetDeviceSelector();

            // Find the first I2C device
            var di = (await DeviceInformation.FindAllAsync(aqs)).FirstOrDefault();

            // Make sure we found an I2C device
            if (di == null) { throw new DeviceNotFoundException("I2C"); }

            // Connection settings for primary device
            var primarySettings = new I2cConnectionSettings(I2C_PRIMARY_ADDRESS);
            primarySettings.BusSpeed = I2cBusSpeed.FastMode;
            primarySettings.SharingMode = I2cSharingMode.Exclusive;

            // Get the primary device
            primaryDevice = await I2cDevice.FromIdAsync(di.Id, primarySettings);
            if (di == null) { throw new DeviceNotFoundException("PCA9685 primary device"); }

            
            // Connection settings for reset device
            var resetSettings = new I2cConnectionSettings(I2C_PRIMARY_ADDRESS);
            resetSettings.SlaveAddress = I2C_RESET_ADDRESS;

            // Get the reset device
            resetDevice = await I2cDevice.FromIdAsync(di.Id, resetSettings);
            if (di == null) { throw new DeviceNotFoundException("PCA9685 reset device"); }

            // Initialize the controller
            InitializeController();

            // Done initializing
            isInitialized = true;
        }

        private void InitializeController()
        {
            if (primaryDevice == null) return;

            ResetController();

            var writeBuf = new byte[2];

            SleepController();

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

            RestartController(0xA1);
        }

        private void ResetController()
        {
            resetDevice.Write(RESET_COMMAND);
        }

        private void RestartController(byte mode1)
        {
            var writeBuf = new byte[2];

            writeBuf[0] = REG_MODE1;
            writeBuf[1] = mode1;
            primaryDevice.Write(writeBuf);

            // Wait for more than 500us to stabilize.  	
            Task.Delay(1).Wait();
        }
        private byte SleepController()
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
            Task.Delay(1).Wait();

            return mode[0];
        }

        #endregion // Internal Methods

        public void AcquirePin(int pin)
        {
            throw new NotImplementedException();
        }

        public void DisablePin(int pin)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void EnablePin(int pin)
        {
            throw new NotImplementedException();
        }

        public void ReleasePin(int pin)
        {
            throw new NotImplementedException();
        }

        public double SetDesiredFrequency(double frequency)
        {
            if (frequency < MIN_FREQUENCY || frequency > MAX_FREQUENCY)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency));
            }

            preScale = (byte)(Math.Round((CLOCK_FREQUENCY / (frequency * PULSE_RESOLUTION))) - 1);
            actualFrequency = CLOCK_FREQUENCY / (double)((preScale + 1) * PULSE_RESOLUTION);

            byte mode1 = SleepController();

            var buffer = new byte[2];
            // Set PRE_SCALE  	
            buffer[0] = REG_PRESCALE;
            buffer[1] = preScale;
            primaryDevice.Write(buffer);

            // Restart  	
            RestartController(mode1);

            return actualFrequency;
        }

        public void SetPulseParameters(int pin, double dutyCycle, bool invertPolarity)
        {
            throw new NotImplementedException();
        }


        #region Public Properties
        public double ActualFrequency
        {
            get
            {
                return actualFrequency;
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
