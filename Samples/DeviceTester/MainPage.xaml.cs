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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace DeviceTester
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private GpioController gpioController;
        private bool isRunning;
        private string lastOutput;
        private PushButton pushButton;
        private Switch swtch;
        private Switch proximity;

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
            StartDevice();
            StopButton.IsEnabled = true;
        }

        private void StartPushButton(GpioPin pin)
        {
            // Create a pushbutton
            pushButton = new PushButton();
            pushButton.Pin = pin;

            // Click on press
            // pushButton.ClickMode = ButtonClickMode.Press;

            // Subscribe to events
            pushButton.Click += PushButton_Click;
            pushButton.Pressed += PushButton_Pressed;
            pushButton.Released += PushButton_Released;
        }

        private void StartSwitches(GpioPin switchPin, GpioPin proxPin)
        {
            // Create switches
            swtch = new Switch()
            {
                Pin = switchPin,
                OnValue = GpioPinValue.Low
            };
            proximity = new Switch()
            {
                Pin = proxPin,
                OnValue = GpioPinValue.Low
            };

            // Subscribe to events
            swtch.Switched += Switch_Switched;
            proximity.Switched += Proximity_Switched;
        }

        private void StartDevice()
        {
            gpioController = GpioController.GetDefault();
            if (gpioController == null)
            {
                AddOutput("GPIO Controller not found!");
                return;
            }

            // Open the pins
            var switchPin = gpioController.OpenPin(5);
            var proxPin = gpioController.OpenPin(6);

            // StartPushButton(pin);
            StartSwitches(switchPin, proxPin);
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
            if (pushButton != null)
            {
                pushButton.Dispose();
                pushButton = null;
            }

            if (proximity != null)
            {
                proximity.Dispose();
                proximity = null;
            }

            if (swtch != null)
            {
                swtch.Dispose();
                swtch = null;
            }
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

        private void Switch_Switched(object sender, bool e)
        {
            Dispatcher.Run(() =>
            {
                if (e)
                {
                    AddOutput("Switched On");
                }
                else
                {
                    AddOutput("Switched Off");
                }
            });
        }
    }
}
