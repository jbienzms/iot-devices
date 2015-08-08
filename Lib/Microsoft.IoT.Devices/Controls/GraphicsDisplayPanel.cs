// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.IoT.Devices.Display;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Microsoft.IoT.Devices.Controls
{
    public sealed class GraphicsDisplayPanel : ContentControl
    {
        #region Static Version
        #region Constants
        private const string ContentPanelName = "ContentPanel";
        #endregion // Constants
        #endregion // Static Version

        #region Instance Version
        #region Member Variables
        private bool autoUpdate;
        private Panel contentPanel;
        private IGraphicsDisplay display;
        private Random rand;
        private TimeSpan updateInterval;
        private DispatcherTimer updateTimer;
        #endregion // Member Variables

        #region Constructors
        public GraphicsDisplayPanel()
        {
            // Theme
            this.DefaultStyleKey = typeof(GraphicsDisplayPanel);

            // Defaults
            rand = new Random();
            updateInterval = TimeSpan.FromSeconds(1);
        }
        #endregion // Constructors

        #region Overrides / Event Handlers
        protected override void OnApplyTemplate()
        {
            // Apply the template
            base.OnApplyTemplate();

            // Find the container for our display content
            contentPanel = GetTemplateChild(ContentPanelName) as Panel;

            // If it's missing, major error
            if (contentPanel == null) { throw new MissingTemplateElementException(ContentPanelName, nameof(GraphicsDisplayPanel)); }
        }

        private async void UpdateTimer_Tick(object sender, object e)
        {
            if (display != null)
            {
                await RenderAsync();
            }
        }
        #endregion // Overrides / Event Handlers

        #region Internal Methods
        // public Image PreviewImage { get; set; }
        private async Task RenderAsync()
        {
            // Make sure we have a display
            if (display == null) { return; }

            // Make sure we have a content panel and we're visible
            if ((contentPanel == null) || (Visibility == Visibility.Collapsed))
            {
                // Warn developer
                var msg = string.Format(Strings.ElementNotRendered, nameof(GraphicsDisplayPanel), Name);
                Debug.WriteLine(msg);

                // Bail
                return;
            }
            
            // Create render target
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();

            // Capture content panel (should be at display scale)
            await renderTargetBitmap.RenderAsync(contentPanel);

            // if (PreviewImage != null) { PreviewImage.Source = renderTargetBitmap; }

            // Get pixel reader
            var reader = DataReader.FromBuffer(await renderTargetBitmap.GetPixelsAsync());

            // Make sure we still have a display
            if (display == null) { return; }

            // Make sure nobody else is attempting to use the display at the same time
            lock (display)
            {
                // Was display auto updating?
                bool wasDisplayAutoUpdating = display.AutoUpdate;

                // Stop display auto updates
                display.AutoUpdate = false;

                /*
                var x = rand.Next(display.Width);
                var y = rand.Next(display.Height);
                var r = (byte)rand.Next(255);
                var g = (byte)rand.Next(255);
                var b = (byte)rand.Next(255);
                display.DrawPixel(x, y, r, g, b);
                */

                // Clear the display
                display.Clear();

                // Get dimensions of bitmap
                var rHeight = renderTargetBitmap.PixelHeight;
                var rWidth = renderTargetBitmap.PixelWidth;

                // Placeholder for reading pixels
                byte[] pixel = new byte[4]; // RGBA8

                // Write out pixels
                using (reader)
                {
                    for (int y = 0; y < rHeight; y++)
                    {
                        for (int x = 0; x < rWidth; x++)
                        {
                            // Read raw pixel bytes
                            reader.ReadBytes(pixel);

                            /*
                            [0] = B
                            [1] = G
                            [2] = R
                            [3] = A
                            */

                            // Write out pixels
                            display.DrawPixel(x, y, pixel[2], pixel[1], pixel[0]);
                        }
                    }
                }

                // Update the display
                display.Update();

                // Resume display auto updates if previously running
                display.AutoUpdate = wasDisplayAutoUpdating;
            }
        }

        private void TryStartUpdates()
        {
            // If we don't have a display or auto updates are false, ignore
            if ((display == null) || (autoUpdate == false)) { return; }

            // If timer hasn't been created yet, create it
            if (updateTimer == null)
            {
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = updateInterval;
                updateTimer.Tick += UpdateTimer_Tick;
            }

            // Start timer if not already running
            if (!updateTimer.IsEnabled) { updateTimer.Start(); }
        }

        private void StopUpdates()
        {
            // If no timer, ignore
            if (updateTimer == null) { return; }

            // Stop timer if running
            if (updateTimer.IsEnabled) { updateTimer.Stop(); }
        }
        #endregion // Internal Methods

        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the panel should automatically update the display.
        /// </summary>
        /// <value>
        /// <c>true</c> if the panel should automatically update the display; otherwise <c>false</c>. The default is <c>false</c>.
        /// </value>
        /// <remarks>
        /// The display will be updated as frequently as <see cref="UpdateInterval"/>
        /// </remarks>
        [DefaultValue(false)]
        public bool AutoUpdate
        {
            get
            {
                return autoUpdate;
            }
            set
            {
                if (value != autoUpdate)
                {
                    autoUpdate = value;
                    if (autoUpdate)
                    {
                        TryStartUpdates();
                    }
                    else
                    {
                        StopUpdates();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the display that the panel will render to.
        /// </summary>
        /// <value>
        /// The display that the panel will render to.
        /// </value>
        public IGraphicsDisplay Display
        {
            get
            {
                return display;
            }
            set
            {
                // Make sure changing
                if (value != display)
                {
                    // Change in thread safe way
                    if (display != null)
                    {
                        lock(display)
                        {
                            display = value;
                        }
                    }
                    else
                    {
                        display = value;
                    }

                    // Start or stop?
                    if (display != null)
                    {
                        TryStartUpdates();
                    }
                    else
                    {
                        StopUpdates();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates how often the display will be updated.
        /// </summary>
        /// <value>
        /// A value that indicates how often the display will be updated. The default is 1 second.
        /// </value>
        public TimeSpan UpdateInterval
        {
            get
            {
                return updateInterval;
            }
            set
            {
                if (value != updateInterval)
                {
                    // Store
                    updateInterval = value;

                    // If timer running, update timer
                    if (updateTimer != null) { updateTimer.Interval = value; }
                }
            }
        }
        #endregion // Public Properties
        #endregion // Instance Version
    }
}
