using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Breathalyzer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Member Variables
        private ObservableCollection<BreathMeasurement> globalHistory;
        #endregion // Member Variables


        #region Constructors
        public MainPage()
        {
            this.InitializeComponent();

            // Create Data
            globalHistory = new ObservableCollection<BreathMeasurement>();
            FakeData();

            // Bind Data
            GlobalSeries.ItemsSource = globalHistory;
        }
        #endregion // Constructors


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
