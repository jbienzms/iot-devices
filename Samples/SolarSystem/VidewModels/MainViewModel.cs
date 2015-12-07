using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using SolarSystem.Model;

namespace SolarSystem.VidewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Member Variables
        private ObservableCollection<CelestialBody> selectedBodies = new ObservableCollection<CelestialBody>();
        private CelestialSystem system;
        #endregion // Member Variables


        public MainViewModel()
        {
            //if (this.IsInDesignMode)
            //{

            //}
            LoadData();
        }

        public void LoadData()
        {
            var sun = new CelestialBody()
            {
                BodyName = "Sun",
                Description = "The Sun is the star at the center of the Solar System and is by far the most important source of energy for life on Earth. It is a nearly perfect spherical ball of hot plasma,[12][13] with internal convective motion that generates a magnetic field via a dynamo process.",
            };

            var earth = new CelestialBody()
            {
                BodyName = "Earth",
                Description = "Earth is the third planet from the Sun, the densest planet in the Solar System, the largest of the Solar System's four terrestrial planets, and the only astronomical object known to harbor life.",
                Day = new TimeSpan(24, 0, 0),
                Orbit = 150,
                Year = TimeSpan.FromDays(365)
            };

            var mars = new CelestialBody()
            {
                BodyName = "Mars",
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

            System = new CelestialSystem()
            {
                CentralBody = sun,
                Bodies = new List<CelestialBody>()
                {
                    sun,
                    earth,
                    mars
                }

            };

            string ssjson = JsonConvert.SerializeObject(System, Formatting.Indented);

        }


        #region Public Properties
        /// <summary>
        /// Gets or sets the collection of selected bodies.
        /// </summary>
        /// <value>
        /// The selected bodies.
        /// </value>
        public ObservableCollection<CelestialBody> SelectedBodies => selectedBodies;

        /// <summary>
        /// Gets or sets the celestial system.
        /// </summary>
        /// <value>
        /// The celestial system.
        /// </value>
        public CelestialSystem System
        {
            get { return system; }
            set { Set(ref system, value); }
        }
        #endregion // Public Properties
    }
}
