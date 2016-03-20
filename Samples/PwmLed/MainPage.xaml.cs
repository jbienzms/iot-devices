// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.IoT.DeviceCore.Pwm;
using Microsoft.IoT.Devices.Pwm;
using Microsoft.IoT.Devices.Lights;
using Microsoft.IoT.Devices.Pwm;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.IoT.Devices.Pwm.PwmSoft;
using Microsoft.IoT.Devices.Pwm.PwmPCA9685;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PwmLed
{
    /******************************************************
    * Note, this sample uses the ColorPicker control created 
    * by Tareq Ateik. You can read more about it here:
    * http://www.tareqateik.com/colorpicker-control-for-universal-apps 
    *
    * Very nice work Tareq!
    *******************************************************/

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Member Variables
        private RgbLed led;
        #endregion // Member Variables

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Start GPIO
            var gpioController = GpioController.GetDefault();

            // Create PWM manager
            var pwmManager = new PwmProviderManager();

            // Add providers
            //pwmManager.Providers.Add(new PwmProviderPCA9685());
            pwmManager.Providers.Add(new PwmProviderSoft());

            // Get the well-known controller collection back
            var pwmControllers = await pwmManager.GetControllersAsync();

            // Using the first PWM controller
            var controller = pwmControllers[0];

            // Set desired frequency
            controller.SetDesiredFrequency(60);

            // Create light sensor
            led = new RgbLed()
            {
                RedPin = controller.OpenPin(4),
                GreenPin = controller.OpenPin(5),
                BluePin = controller.OpenPin(6),
            };
        }

        private void ColorPick_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.IsInContact)
            {
                var selCol = ColorPick.SelectedColor;
                if (selCol != null)
                {
                    led.Color = ColorPick.SelectedColor.Color;
                }
            }
        }

        private void ColorPick_SelectedColorChanged(object sender, EventArgs e)
        {
            if (ColorPick.SelectedColor != null)
            {
                led.Color = ColorPick.SelectedColor.Color;
            }
        }
    }
}
