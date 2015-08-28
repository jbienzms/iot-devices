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
using Microsoft.IoT.DeviceCore.Input;
using Microsoft.IoT.Devices.Input;
using Windows.Devices.Gpio;
using Windows.UI;

namespace AvoidanceSample
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            var controller = GpioController.GetDefault();
            var SwitchSensor = new Switch()
            {
                Pin = controller.OpenPin(6)
            };
            SwitchSensor.Switched += SwitchSensor_Switched;
        }

        private async void SwitchSensor_Switched(ISwitch sender, bool args)
        {
            await Dispatcher.RunIdleAsync((s) =>
            {
                if (args)
                {
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    StatusText.Text = "Close";
                }
                else
                {
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                    StatusText.Text = "Far";
                }
            });
        }
    }
}
