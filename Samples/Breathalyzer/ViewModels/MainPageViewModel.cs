using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Breathalyzer
{
    public class MainPageViewModel
    {
        private ObservableCollection<BreathMeasurement> breathHistory;

        public MainPageViewModel()
        {
            breathHistory = new ObservableCollection<BreathMeasurement>();
            FakeData();
        }

        private void FakeData()
        {
            breathHistory.Add(new BreathMeasurement()
            {
                Alias = "jbienz",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(34),
                Value = 0.5,
            });
            breathHistory.Add(new BreathMeasurement()
            {
                Alias = "jbienz",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(60),
                Value = 0.25,
            });
            breathHistory.Add(new BreathMeasurement()
            {
                Alias = "pdecarlo",
                TimeStamp = DateTime.Now - TimeSpan.FromMinutes(64),
                Value = 0.75,
            });

            for (int i =0; i < 10; i++)
            {
                breathHistory.Add(breathHistory[breathHistory.Count - 3]);
            }

        }

        /// <summary>
        /// Gets the historical collection of breath measurements.
        /// </summary>
        public ObservableCollection<BreathMeasurement> BreathHistory => breathHistory;
    }
}
