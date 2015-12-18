using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Threading;
using Newtonsoft.Json;
using SolarSystem.Data;
using SolarSystem.IO;
using SolarSystem.Model;
using SolarSystem.Speech;

namespace SolarSystem.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Member Variables
        private DataManager dataManager;
        private IoManager ioManager;
        private ObservableCollection<CelestialBody> selectedBodies;
        private SpeechManager speechManager;
        private CelestialSystem system;
        #endregion // Member Variables

        #region Constructors
        public MainViewModel()
        {
            Initialize();
        }
        #endregion // Constructors

        #region Internal Methods
        private async void Initialize()
        {
            SelectedBodies = new ObservableCollection<CelestialBody>();
            dataManager = new DataManager();
            if (this.IsInDesignMode)
            {
                system = dataManager.LoadSampleData();
            }
            else
            {
                system = await dataManager.LoadDataAsync();
                await InitSpeechAsync();
                InitIO();
            }
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
