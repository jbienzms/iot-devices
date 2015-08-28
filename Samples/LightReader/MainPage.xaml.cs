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
// ...
using Microsoft.IoT.DeviceCore.Adc;
using Microsoft.IoT.Devices.Adc;
using Microsoft.IoT.DeviceCore.Sensors;
using Microsoft.IoT.Devices.Sensors;
using Windows.Devices.Gpio;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace LightReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Start GPIO
            var gpioController = GpioController.GetDefault();

            // Create ADC manager
            var adcManager = new AdcProviderManager();

            // Add ADC chips
            adcManager.Providers.Add(
                new ADC0832()
                {
                    ChipSelectPin = gpioController.OpenPin(18),
                    ClockPin = gpioController.OpenPin(23),
                    DataPin = gpioController.OpenPin(24),
                });


            // Get the well-known controller collection back
            var adcControllers = await adcManager.GetControllersAsync();

            // Create light sensor
            var lightSensor = new AnalogSensor()
            {
                AdcChannel = adcControllers[0].OpenChannel(0),
                ReportInterval = 250,
            };

            // Subscribe to events
            lightSensor.ReadingChanged += LightSensor_ReadingChanged;
        }

        private async void LightSensor_ReadingChanged(IAnalogSensor sender, AnalogSensorReadingChangedEventArgs args)
        {
            // Invert
            var reading = 1 - args.Reading.Ratio;

            // Update UI
            await Dispatcher.RunIdleAsync((s) =>
            {
                // Value
                LightProgress.Value = reading;

                // Color
                if (reading < .25)
                {
                    LightProgress.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (reading < .75 )
                {
                    LightProgress.Foreground = new SolidColorBrush(Colors.Yellow);
                }
                else
                {
                    LightProgress.Foreground = new SolidColorBrush(Colors.Green);
                }
            });

        }
    }
}
