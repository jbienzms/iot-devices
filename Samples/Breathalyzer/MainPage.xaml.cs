using Microsoft.IoT.DeviceCore.Adc;
using Microsoft.IoT.DeviceCore.Display;
using Microsoft.IoT.Devices.Adc;
using Microsoft.IoT.Devices.Display;
using Microsoft.IoT.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.IoT.DeviceCore.Sensors;
using Microsoft.WindowsAzure.MobileServices;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Breathalyzer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Member Variables
        public static MobileServiceClient MobileService = new MobileServiceClient(
        "https://breathalyzer.azure-mobile.net/",
        "wMZVVcPRvcYQypUxSbffbaLWSWSimJ57"
        );

        private MobileServiceCollection<BreathMeasurement, BreathMeasurement> breathMeasurements;
        private IMobileServiceTable<BreathMeasurement> breathMeasurementTable = MobileService.GetTable<BreathMeasurement>();

        private ObservableCollection<BreathMeasurement> globalHistory;
        private AdcProviderManager adcManager;
        private IReadOnlyList<AdcController> adcControllers;
        private GpioController gpioController;
        private DispatcherTimer countdownTimer;

        private const int captureSeconds = 10;
        private int currentcaptureSecond = 0;
        private const double ambientThreshold = .14;
        private double currentReading = 0;
        private double maxReading = 0;

        private enum States {Init, Ready, Capturing, Sending, Cooldown};
        private States currentState = States.Init;
        #endregion // Member Variables

        #region Constructors
        public MainPage()
        {
            this.InitializeComponent();

            // Create Data
            globalHistory = new ObservableCollection<BreathMeasurement>();
            //FakeData();

            // Bind Data
            GlobalSeries.ItemsSource = globalHistory;

            this.Loaded += MainPage_Loaded;
        }
        #endregion // Constructors

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync(); 

            foreach (var measurement in breathMeasurements)
                globalHistory.Add(measurement);

            await SetupAsync();
        }

        private async Task LoadDataAsync()
        {
            breathMeasurements = await (from m in breathMeasurementTable
                                        orderby m.Value descending
                                        select m).Take(30).ToCollectionAsync();

            LeaderGridView.ItemsSource = breathMeasurements;
        }

        private async Task SetupAsync()
        {
            // Start GPIO
            gpioController = GpioController.GetDefault();

            if (gpioController == null)
            {
                Debug.WriteLine("GPIO Controller not found!");
                return;
            }

            // ADC
            // Create the manager
            adcManager = new AdcProviderManager();

            adcManager.Providers.Add(
                new MCP3208()
                {
                    ChipSelectLine = 0,
                    ControllerName = "SPI1",
                });

            // Get the well-known controller collection back
            adcControllers = await adcManager.GetControllersAsync();

            await StartDisplayAsync();
            StartAnalog();
            SetupTimers();
            await TestAndUpdateStateAsync();
        }


        private async Task StartDisplayAsync()
        {
            // Create the display
            var disp = new ST7735()
            {
                ChipSelectLine = 0,
                ClockFrequency = 40000000, // Attempt to run at 40 MHz
                ControllerName = "SPI0",
                DataCommandPin = gpioController.OpenPin(12),
                DisplayType = ST7735DisplayType.RRed,
                ResetPin = gpioController.OpenPin(16),

                Orientation = DisplayOrientations.Portrait,
                Width = 128,
                Height = 160,
            };

            // Initialize the display
            await disp.InitializeAsync();

            // Update the display faster than the default of 1 second
            GraphicsPanel.UpdateInterval = 500;

            // Associate with display panel
            GraphicsPanel.Display = disp;

            // Start updates
            GraphicsPanel.AutoUpdate = true;
        }

        private void StartAnalog()
        {
            // Analog sensor is on controller 1 for this sample
            var controller = adcControllers[0];

            // Create analog sensor
            var analogSensor = new AnalogSensor()
            {
                AdcChannel = controller.OpenChannel(0),
                ReportInterval = 1000, // This demo doesn't need fast reports and it helps with responsiveness
            };

            // Subscribe to events
            analogSensor.ReadingChanged += AnalogSensor_ReadingChanged;
        }

        private void AnalogSensor_ReadingChanged(IAnalogSensor sender, AnalogSensorReadingChangedEventArgs args)
        {
            // Get reading
            var r = args.Reading;

            currentReading = args.Reading.Ratio - ambientThreshold;
            maxReading = Math.Max(maxReading, currentReading);

            var t = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (currentState == States.Cooldown)
                {
                    CurrentValueProg.Value = currentReading;
                }
                else
                {
                    CurrentValueProg.Value = maxReading;
                }
                PercentBlock.Text = string.Format("{0:N0}%", maxReading * 100);
                await TestAndUpdateStateAsync();
            });

            // Print
            //Debug.WriteLine(string.Format("Value: {0}  Ratio: {1}", r.Value, r.Ratio));
        }

        private void SetupTimers()
        {
            countdownTimer = new DispatcherTimer();
            countdownTimer.Interval = TimeSpan.FromSeconds(1);
            countdownTimer.Tick += countdownTimer_Tick; ;
        }

        private async void countdownTimer_Tick(object sender, object e)
        {
            currentcaptureSecond--;
            CounterBlock.Text = currentcaptureSecond.ToString();

            if (currentcaptureSecond == 0)
                await TestAndUpdateStateAsync();
        }

        private async Task TestAndUpdateStateAsync()
        {
            switch (currentState)
            {
                case States.Init:
                    currentState = States.Ready;
                    VisualStateManager.GoToState(this, currentState.ToString(), true);
                    break;
                case States.Ready:
                    if(currentReading > 0)
                    {
                        currentReading = 0;
                        maxReading = 0;
                        currentcaptureSecond = captureSeconds;
                        currentState = States.Capturing;
                        VisualStateManager.GoToState(this, currentState.ToString(), true);
                        countdownTimer.Start();
                    }
                    break;
                case States.Capturing:
                    if (currentcaptureSecond <= 0)
                    {
                        currentState = States.Sending;
                        VisualStateManager.GoToState(this, currentState.ToString(), true);
                        await SendReadingAsync();
                        currentState = States.Cooldown;
                        VisualStateManager.GoToState(this, currentState.ToString(), true);
                    }
                    break;
                case States.Cooldown:
                    if (currentReading < 1)
                    {
                        currentState = States.Ready;
                        VisualStateManager.GoToState(this, currentState.ToString(), true);
                        AliasBlock.Text = string.Empty;
                    }

                    break;
            }
        }

        private async Task SendReadingAsync()
        {
            var reading = new BreathMeasurement()
            {
                Alias = AliasBlock.Text,
                Value = maxReading,
                TimeStamp = DateTime.Now,
            };
            if (!string.IsNullOrWhiteSpace(reading.Alias))
            {
                await breathMeasurementTable.InsertAsync(reading);
                await LoadDataAsync();
            }
            globalHistory.Add(reading);
        }

        #region Internal Methods
        private void FakeData()
        {
            globalHistory.Add(new BreathMeasurement()
            {
                Alias = "jbienz",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(34),
                Value = 0.5,
            });
            globalHistory.Add(new BreathMeasurement()
            {
                Alias = "jbienz",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(60),
                Value = 0.25,
            });
            globalHistory.Add(new BreathMeasurement()
            {
                Alias = "pdecarlo",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(64),
                Value = 0.75,
            });

            for (int i = 0; i < 10; i++)
            {
                globalHistory.Add(globalHistory[globalHistory.Count - 3]);
            }

        }
        #endregion // Internal Methods


    }
}
