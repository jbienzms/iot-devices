// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Adc
{
    /// <summary>
    /// Represents a single channel for the <see href="http://www.ti.com/product/adc0832-n">ADC0832</see>.
    /// </summary>
    public sealed class ADC0832Channel : IAdcChannel
    {
        #region Member Variables
        private int channel;
        private GpioPin chipSelectPin;
        private GpioPin clockPin;
        private GpioPin dataPin;
        #endregion // Member Variables

        #region Constructors
        internal ADC0832Channel(ADC0832 controller, int channel)
        {
            chipSelectPin = controller.ChipSelectPin;
            clockPin = controller.ClockPin;
            dataPin = controller.DataPin;
            this.channel = channel;
        }
        #endregion // Constructors

        public void Close() { }

        public int ReadValue()
        {
            byte i;
            int dat1 = 0, dat2 = 0;

            var task = Task.Run<int>(() =>
            {
                chipSelectPin.Write(GpioPinValue.Low);

                clockPin.Write(GpioPinValue.Low);
                dataPin.Write(GpioPinValue.High);
                Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.High);
                Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.Low);

                dataPin.Write(GpioPinValue.High); // CH0 10
                Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.High);
                Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.Low);

                if (channel == 0)
                {
                    dataPin.Write(GpioPinValue.Low); //CH0 0
                }
                else
                {
                    dataPin.Write(GpioPinValue.High); //CH1 1
                }
                Task.Delay(1); // 2 microseconds

                clockPin.Write(GpioPinValue.High);
                dataPin.Write(GpioPinValue.High);
                Task.Delay(1); // 2 microseconds
                clockPin.Write(GpioPinValue.Low);
                dataPin.Write(GpioPinValue.High);
                Task.Delay(1); // 2 microseconds

                // Start reading input
                dataPin.SetDriveMode(GpioPinDriveMode.Input);

                // Read 8 bits and convert to bytes
                for (i = 0; i < 8; i++)
                {
                    clockPin.Write(GpioPinValue.High);
                    Task.Delay(1); // 2 microseconds
                    clockPin.Write(GpioPinValue.Low);
                    Task.Delay(1); // 2 microseconds

                    dat1 = (dat1 << 1) | (dataPin.Read() == GpioPinValue.High ? 1 : 0);
                }

                for (i = 0; i < 8; i++)
                {
                    dat2 = dat2 | ((dataPin.Read() == GpioPinValue.High ? 1 : 0) << i);
                    clockPin.Write(GpioPinValue.High);
                    Task.Delay(1); // 2 microseconds
                    clockPin.Write(GpioPinValue.Low);
                    Task.Delay(1); // 2 microseconds
                }

                chipSelectPin.Write(GpioPinValue.High);

                // Back to output mode
                dataPin.SetDriveMode(GpioPinDriveMode.Output);

                return (dat1 == dat2) ? dat1 : 0;
            });

            // Wait for task to complete
            task.Wait();

            // Return the result
            return task.Result;
        }
    }
}
