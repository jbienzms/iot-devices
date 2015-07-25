// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Windows.Devices.IoT.Sensors
{
    public class DHT11 : AsyncScheduledDevice
    {
        #region Member Variables
        private GpioPin pin;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="DHT11"/> instance.
        /// </summary>
        /// <param name="pin">
        /// The pin that the device is connected to.
        /// </param>
        public DHT11(GpioPin pin) : base(new ScheduleOptions(reportInterval: 1000))
        {
            // Validate
            if (pin == null) throw new ArgumentNullException("pin");

            // Store
            this.pin = pin;

            // Create events
            //clickEvent = new SchedulingEvent<PushButton, EventArgs>(this);
            //pressedEvent = new SchedulingEvent<PushButton, EventArgs>(this);
            //releasedEvent = new SchedulingEvent<PushButton, EventArgs>(this);

            // Initialize IO
            InitIO();
        }
        #endregion // Constructors

        #region Internal Methods
        private void InitIO()
        {
            // Default to not pressed
            lastValue = releasedValue;

            // Check if input pull-up resistors are supported 
            if (pin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
            {
                pin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            }
            else
            {
                pin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Set a debounce timeout to filter out switch bounce noise from a button press 
            pin.DebounceTimeout = TimeSpan.FromMilliseconds(50);
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        protected override async Task UpdateAsync(CancellationToken cancellationToken)
        {
            GpioPinValue laststate = GpioPinValue.High;
            byte counter = 0;
            byte j = 0, i;
            float f; // fahrenheit

            // pull pin down for 18 milliseconds
            pin.SetDriveMode(GpioPinDriveMode.Output);
            pin.Write(GpioPinValue.Low);
            await Task.Delay(18, cancellationToken);
            if (cancellationToken.IsCancellationRequested) { return; }

            // then pull it up for 40 microseconds
            pin.Write(GpioPinValue.High);
            await Task.Delay(1, cancellationToken);
            if (cancellationToken.IsCancellationRequested) { return; }

            // prepare to read the pin
            pin.SetDriveMode(GpioPinDriveMode.Input);

            pin.R

            // detect change and read data
            for (i = 0; i < MAXTIMINGS; i++)
            {
                counter = 0;
                while (digitalRead(DHTPIN) == laststate)
                {
                    counter++;
                    delayMicroseconds(1);
                    if (counter == 255)
                    {
                        break;
                    }
                }
                laststate = digitalRead(DHTPIN);

                if (counter == 255) break;

                // ignore first 3 transitions
                if ((i >= 4) && (i % 2 == 0))
                {
                    // shove each bit into the storage bytes
                    dht11_dat[j / 8] <<= 1;
                    if (counter > 16)
                        dht11_dat[j / 8] |= 1;
                    j++;
                }
            }

            // check we read 40 bits (8bit x 5 ) + verify checksum in the last byte
            // print it out if data is good
            if ((j >= 40) &&
                    (dht11_dat[4] == ((dht11_dat[0] + dht11_dat[1] + dht11_dat[2] + dht11_dat[3]) & 0xFF)))
            {
                f = dht11_dat[2] * 9. / 5. + 32;
                printf("Humidity = %d.%d %% Temperature = %d.%d *C (%.1f *F)\n",
                        dht11_dat[0], dht11_dat[1], dht11_dat[2], dht11_dat[3], f);
            }
            else
            {
                printf("Data not good, skip\n");
            }

        }

        public override void Dispose()
        {
            base.Dispose();
            pin.Dispose();
        }
        #endregion // Overrides / Event Handlers
    }
}
