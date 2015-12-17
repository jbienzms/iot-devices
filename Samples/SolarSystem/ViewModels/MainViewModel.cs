using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using SolarSystem.IO;
using SolarSystem.Model;
using SolarSystem.Speech;

namespace SolarSystem.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Member Variables
        private IoManager ioManager;
        private ObservableCollection<CelestialBody> selectedBodies;
        private SpeechManager speechManager;
        private CelestialSystem system;
        #endregion // Member Variables

        #region Constructors
        public MainViewModel()
        {
            if (this.IsInDesignMode)
            {
                LoadSampleData();
            }
            else
            {
                Initialize();
            }
        }
        #endregion // Constructors

        #region Internal Methods
        private async void Initialize()
        {
            SelectedBodies = new ObservableCollection<CelestialBody>();
            await LoadDataAsync();
            await InitSpeechAsync();
            InitIO();
        }

        private void InitIO()
        {
            // Create
            ioManager = new IoManager();

            // Initialize
            ioManager.Initialize();

            // Turn off pins if we're using GPIO
            if (ioManager.IsEnabled)
            {
                // Get all bodies that are connected to IO and select the IO pins themselves
                var pins = (from b in system.Bodies
                            where b.IoPin.HasValue
                            select b.IoPin.Value).ToArray();

                // Turn off all IO pins (to deal with starting floating values).
                ioManager.SetOff(pins);
            }
        }

        private async Task InitSpeechAsync()
        {
            // Create
            speechManager = new SpeechManager();

            // Subscribe to events
            speechManager.ResultRecognized += SpeechManager_ResultRecognized;

            // Initialize
            await speechManager.InitializeAsync(system);
        }

        private async Task LoadDataAsync()
        {
            // Simulate for now
            await Task.Delay(50);
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            var sun = new CelestialBody()
            {
                BodyName = "Sun",
                IoPin = 4,
                Description = "The Sun is the star at the center of the Solar System and is by far the most important source of energy for life on Earth. It is a nearly perfect spherical ball of hot plasma, with internal convective motion that generates a magnetic field via a dynamo process.",
            };

            var earth = new CelestialBody()
            {
                BodyName = "Earth",
                IoPin = 26,
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
                Title = "Planets with ice",
                Contributor = "Bienz / Vasek Family",
                Description = "Both Earth and Mars have ice but Mars contains very little ice.",
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

        private void UpdateIO()
        {
            // Set exclusive lights if we're using GPIO
            if ((ioManager != null) && (ioManager.IsEnabled) && (selectedBodies != null))
            {
                // Get all bodies that are connected to IO and select the IO pins themselves
                var pins = (from b in selectedBodies
                            where b.IoPin.HasValue
                            select b.IoPin.Value).ToArray();

                // Set exclusive
                ioManager.SetExclusive(pins);
            }
        }
        #endregion // Internal Methods

        #region Overrides / Event Handlers
        private void SelectedBodies_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Update IO
            UpdateIO();
        }

        private void SpeechManager_ResultRecognized(SpeechManager sender, CelestialSpeechResult args)
        {
            // If one or more bodies were recognized, select them.
            if (args.Bodies != null)
            {
                // Must be set on UI thread
                DispatcherHelper.CheckBeginInvokeOnUI(() =>
                {
                    SelectedBodies = new ObservableCollection<CelestialBody>(args.Bodies);
                });
            }
        }
        #endregion // Overrides / Event Handlers


        #region Public Properties
        /// <summary>
        /// Gets or sets the collection of selected bodies.
        /// </summary>
        /// <value>
        /// The selected bodies.
        /// </value>
        public ObservableCollection<CelestialBody> SelectedBodies
        {
            get { return selectedBodies; }
            set
            {
                // Hold onto previous value
                var oldBodies = selectedBodies;

                // Check if changing
                if (Set(ref selectedBodies, value))
                {
                    // Unsubscribe?
                    if (oldBodies != null)
                    {
                        oldBodies.CollectionChanged -= SelectedBodies_CollectionChanged;
                    }

                    // Subscribe?
                    if (value != null)
                    {
                        value.CollectionChanged += SelectedBodies_CollectionChanged;
                    }

                    // Update IO
                    UpdateIO();
                }
            }
        }

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
