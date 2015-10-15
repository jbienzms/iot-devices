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
using SolarSystem.Model;
using Newtonsoft.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SolarSystem
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var sun = new CelestialBody()
            {
                Name = "Sun",
                Description = "The Sun is the star at the center of the Solar System and is by far the most important source of energy for life on Earth. It is a nearly perfect spherical ball of hot plasma,[12][13] with internal convective motion that generates a magnetic field via a dynamo process.",
            };

            var earth = new CelestialBody()
            {
                Name = "Earth",
                Description = "Earth is the third planet from the Sun, the densest planet in the Solar System, the largest of the Solar System's four terrestrial planets, and the only astronomical object known to harbor life.",
                Day = new TimeSpan(24, 0, 0),
                Orbit = 150,
                Year = TimeSpan.FromDays(365)
            };

            var mars = new CelestialBody()
            {
                Name = "Mars",
                Description = "Mars is the fourth planet from the Sun and the second smallest planet in the Solar System, after Mercury. Named after the Roman god of war, it is often referred to as the \"Red Planet\" because the iron oxide prevalent on its surface gives it a reddish appearance.",
                Day = new TimeSpan(24, 39, 0),
                Orbit = 230,
                Year = TimeSpan.FromDays(687)
            };


            var iceFact = new CelestialFact()
            {
                Name = "PlanetsWithIce",
                Description = "Planets that have ice",
                Bodies = new List<CelestialBody>()
                {
                    earth,
                    mars
                }
            };

            var sol = new CelestialSystem()
            {
                CentralBody = sun,
                Bodies = new List<CelestialBody>()
                {
                    sun,
                    earth,
                    mars
                }

            };

            string ssjson = JsonConvert.SerializeObject(sol, Formatting.Indented);

        }
    }
}
