// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Windows.Devices.IoT.Input
{
    public class PushButton : ScheduledDevice
    {
        private GpioPin pin;

        public PushButton(GpioPin pin) : base(new ScheduleOptions(reportInterval:200))
        {
            // Validate
            if (pin == null) throw new ArgumentNullException("pin");

            // Store
            this.pin = pin;
        }

        // TODO: This should be on click subscribe
        public void Start()
        {
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


            // Register for the ValueChanged event so our pin_ValueChanged  
            // function is called when the button is pressed 
            // pin.ValueChanged += Pin_ValueChanged;

            StartUpdates();
        }

        private void Pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs e)
        {
            if (e.Edge == GpioPinEdge.RisingEdge)
            {
                Debug.WriteLine("Rising");
            }
            else
            {
                Debug.WriteLine("Falling");
            }
        }

        public void Stop()
        {
            // pin.ValueChanged -= Pin_ValueChanged;
            StopUpdates();
        }

        protected override void Update()
        {
            if (pin.Read() == GpioPinValue.High)
            {
                Debug.WriteLine("PIN HIGH");
            }
            else
            {
                Debug.WriteLine("PIN LOW");
            }
        }

        public override void Dispose()
        {
            Stop();
            pin.Dispose();
            base.Dispose();
        }
    }
}
