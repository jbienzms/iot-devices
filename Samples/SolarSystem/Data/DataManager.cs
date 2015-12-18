using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SolarSystem.Model;
using Windows.ApplicationModel;
using Windows.Storage;

namespace SolarSystem.Data
{
    public class DataManager
    {
        #region Constants
        private const string DATA_FILE = "Data\\SystemData.json";
        #endregion // Constants

        #region Public Methods
        /// <summary>
        /// Loads the full data set.
        /// </summary>
        /// <returns>
        /// The loaded <see cref="CelestialSystem"/>.
        /// </returns>
        public async Task<CelestialSystem> LoadDataAsync()
        {
            // Get data file
            var dataFile = await Package.Current.InstalledLocation.GetFileAsync(DATA_FILE);

            // Read to string
            var sjson = await FileIO.ReadTextAsync(dataFile);

            // Deserialize
            return JsonConvert.DeserializeObject<CelestialSystem>(sjson);
        }

        /// <summary>
        /// Loads a minimized data set for design time.
        /// </summary>
        /// <returns>
        /// A minimized <see cref="CelestialSystem"/>.
        /// </returns>
        public CelestialSystem LoadSampleData()
        {
            var sun = new CelestialBody()
            {
                Name = "Sun",
                IoPin = 4,
                Description = "The Sun is the star at the center of the Solar System and is by far the most important source of energy for life on Earth. It is a nearly perfect spherical ball of hot plasma, with internal convective motion that generates a magnetic field via a dynamo process.",
            };

            var earth = new CelestialBody()
            {
                Name = "Earth",
                IoPin = 26,
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
                Title = "Planets with ice",
                Contributor = "Bienz / Vasek Family",
                Description = "Both Earth and Mars have ice but Mars contains very little ice.",
                Bodies = new List<CelestialBody>()
                {
                    earth,
                    mars
                }
            };

            var system = new CelestialSystem()
            {
                Bodies = new List<CelestialBody>()
                {
                    sun,
                    earth,
                    mars
                },
                Facts = new List<CelestialFact>()
                {
                    iceFact
                }
            };

            string ssjson = JsonConvert.SerializeObject(system, Formatting.Indented);

            return system;
        }
        #endregion // Public Methods
    }
}
