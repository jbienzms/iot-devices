// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using Microsoft.IoT.Devices.Input;
using Microsoft.IoT.Devices;
using Microsoft.IoT.Devices.Adc;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Microsoft.IoT.Devices.Sensors;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DeviceTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IAdcController adcController1;
        private IAdcController adcController2;
        private GpioController gpioController;
        private bool isRunning;
        private string lastOutput;
        //private ISwitch proximity;
        //private IPushButton pushButton;
        //private IThumbstick thumbstick;
        private Collection<IDevice> devices = new Collection<IDevice>();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void AddOutput(string output)
        {
            // Prefix DT
            output = DateTime.Now.ToString("HH:mm:ss") + " " + output;

            // Add to the list
            OutputList.Items.Add(output);

            // If there is no selection or the current selection is the last entry, auto scroll
            if ((OutputList.SelectedItem == null) || ((string)OutputList.SelectedItem == lastOutput))
            {
                // Select the new output
                OutputList.SelectedItem = output;

                // Auto scroll
                OutputList.ScrollIntoView(output, ScrollIntoViewAlignment.Leading);
            }

            // Update last output
            lastOutput = output;
        }

        private void Start()
        {
            if (isRunning) { return; }
            isRunning = true;
            StartButton.IsEnabled = false;
            StartDevices();
            StopButton.IsEnabled = true;
        }

        private void StartAlcohol()
        {
            // Create alcohol sensor
            var alcoholSensor = new AnalogSensor()
            {
                AdcChannel = adcController2.OpenChannel(0)
                //AdcChannel = adcController1.OpenChannel(1)
            };

            // Subscribe to events
            alcoholSensor.ReadingChanged += AlcoholSensor_ReadingChanged;

            // Add to device list
            devices.Add(alcoholSensor);
        }

        private void StartPushButton()
        {
            // Create a pushbutton
            var pushButton = new PushButton()
            {
                Pin = gpioController.OpenPin(5),
            };

            // Click on press
            // pushButton.ClickMode = ButtonClickMode.Press;

            // Subscribe to events
            pushButton.Click += PushButton_Click;
            pushButton.Pressed += PushButton_Pressed;
            pushButton.Released += PushButton_Released;

            // Add to device list
            devices.Add(pushButton);
        }

        private void StartSwitches()
        {
            // Create switches
            var proximity = new Switch()
            {
                Pin = gpioController.OpenPin(6),
            };

            // Subscribe to events
            proximity.Switched += Proximity_Switched;

            // Add to device list
            devices.Add(proximity);
        }

        private void StartThumbstick()
        {
            var thumbstick = new SS944()
            {
                ButtonPin = gpioController.OpenPin(25),
                ReportInterval = 250, // This demo doesn't need fast reports and it helps with responsiveness
                XChannel = adcController1.OpenChannel(0),
                YChannel = adcController1.OpenChannel(1),
            };

            // Subscribe to events
            thumbstick.ReadingChanged += Thumbstick_ReadingChanged;

            // Add to device list
            devices.Add(thumbstick);
        }

        private void StartDevices()
        {
            // Start GPIO
            gpioController = GpioController.GetDefault();
            if (gpioController == null)
            {
                AddOutput("GPIO Controller not found!");
                return;
            }

            // Start ADC
            adcController1 = new ADC0832()
            {
                ChipSelectPin = gpioController.OpenPin(18),
                ClockPin = gpioController.OpenPin(23),
                DataPin = gpioController.OpenPin(24),
            };

            adcController2 = new MCP3208(); // Defaults to SPI0 and ChipSelect 0

            StartAlcohol();
            StartPushButton();
            StartSwitches();
            // StartThumbstick();
        }

        private void Stop()
        {
            if (!isRunning) { return; }
            isRunning = false;
            StopButton.IsEnabled = false;
            StopDevices();
            StartButton.IsEnabled = true;
        }

        private void StopDevices()
        {
            if (adcController1 != null)
            {
                adcController1.Dispose();
                adcController1 = null;
            }

            if (adcController2 != null)
            {
                adcController2.Dispose();
                adcController2 = null;
            }

            for (int i = devices.Count -1; i >= 0; i--)
            {
                devices[i].Dispose();
                devices.RemoveAt(i);
            }
        }

        private void AlcoholSensor_ReadingChanged(IAnalogSensor sender, AnalogSensorReadingChangedEventArgs args)
        {
            // Get reading
            var r = args.Reading;

            Debug.WriteLine(string.Format("Value: {0}  Ratio: {1}", r.Value, r.Ratio));
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            OutputList.Items.Clear();
        }

        private void PushButton_Click(IPushButton sender, EmptyEventArgs args)
        {
            Dispatcher.Run(() => AddOutput("Click"));
        }

        private void PushButton_Pressed(IPushButton sender, EmptyEventArgs args)
        {
            Dispatcher.Run(() => AddOutput("Pressed"));
        }

        private void PushButton_Released(IPushButton sender, EmptyEventArgs args)
        {
            Dispatcher.Run(() => AddOutput("Released"));
        }

        private void Proximity_Switched(object sender, bool e)
        {
            Dispatcher.Run(() =>
            {
                if (e)
                {
                    AddOutput("Close");
                }
                else
                {
                    AddOutput("Far");
                }
            });
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void Thumbstick_ReadingChanged(IThumbstick sender, ThumbstickReadingChangedEventArgs args)
        {
            // Get reading
            var r = args.Reading;

            Debug.WriteLine(string.Format("X: {0}  Y: {1}  Button: {2}", r.XAxis, r.YAxis, r.IsPressed));
        }
    }
}
