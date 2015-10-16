// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Microsoft.IoT.DeviceCore.Display;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Microsoft.IoT.DeviceCore.Controls
{
    /// <summary>
    /// A container control that can mirror its contents out to a hardware graphics display.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <see cref="GraphicsDisplayPanel"/> works with any display that implements the <see cref="IGraphicsDisplay"/> interface. 
    /// The display must be initialized and assigned to the <see cref="Display"/> property of this control. After the display 
    /// has been assigned set the <see cref="AutoUpdate"/> property to <c>true</c> to begin rendering to the display.
    /// </p>
    /// <p>
    /// Child content inside of this control should have the same pixel dimensions as the attached graphics display. 
    /// The default template for this control wraps child content with a <see cref="Viewbox"/> so that the content will fill 
    /// available screen space regardless of the native pixel dimensions.
    /// </p>
    /// <p>
    /// This control must be present in the XAML tree in order for it to work, however it does not have to be opaque. The  
    /// <see cref="UIElement.Opacity">Opacity</see> property can be set to <c>0</c> to keep the control from rendering on an 
    /// HDMI display while still allowing it to render on the attached graphics display. Importantly, do not set the 
    /// <see cref="UIElement.Visibility">Visibility</see> property to <c>Collapsed</c> or the XAML framework will not allow the 
    /// control to render on either display.
    /// </p>
    /// </remarks>
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
        private ScheduledUpdater updater;
        #endregion // Member Variables

        #region Constructors
        public GraphicsDisplayPanel()
        {
            // Theme
            this.DefaultStyleKey = typeof(GraphicsDisplayPanel);

            // Create the updater. Default to 1 second between updates.
            // IMPORTANT: Do not use Scheduler.Default, create a new Scheduler.
            // This puts us in parallel priority with other sensors and allows 
            // us to run on a separate core if available.
            updater = new ScheduledUpdater(scheduleOptions: new ScheduleOptions(1000), scheduler: new Scheduler());
            updater.SetAsyncUpdateAction(RenderAsyncAction);
        }
        #endregion // Constructors

        #region Overrides / Event Handlers
        /// <inheritdoc/>
        protected override void OnApplyTemplate()
        {
            // Apply the template
            base.OnApplyTemplate();

            // Find the container for our display content
            contentPanel = GetTemplateChild(ContentPanelName) as Panel;

            // If it's missing, major error
            if (contentPanel == null) { throw new MissingTemplateElementException(ContentPanelName, nameof(GraphicsDisplayPanel)); }
        }
        #endregion // Overrides / Event Handlers

        #region Internal Methods
        // public Image PreviewImage { get; set; }
        private IAsyncAction RenderAsyncAction()
        {
            return RenderAsync().AsAsyncAction();
        }
        private async Task RenderAsync()
        {
            // Make sure we have a display
            if (display == null) { return; }

            // Placeholders
            RenderTargetBitmap renderTargetBitmap = null;
            DataReader reader = null;
            int rHeight=0;
            int rWidth=0;

            // Task to wait for COMPLETION of the UI action
            var uiComplete = new TaskCompletionSource<object>();

            // The following steps must be done on the UI thread
            // The task returned here completes when it the work 
            // is SCHEDULED, not complete.
            var t = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
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
                renderTargetBitmap = new RenderTargetBitmap();

                // Capture content panel (should be at display scale)
                await renderTargetBitmap.RenderAsync(contentPanel);

                // if (PreviewImage != null) { PreviewImage.Source = renderTargetBitmap; }

                // Get dimensions of bitmap
                rHeight = renderTargetBitmap.PixelHeight;
                rWidth = renderTargetBitmap.PixelWidth;

                // Get pixel reader
                reader = DataReader.FromBuffer(await renderTargetBitmap.GetPixelsAsync());

                // Signal completion
                uiComplete.SetResult(null);
            });

            // Wait for UI actions to complete
            await uiComplete.Task;

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

            // Start the updater if not already started
            if (!updater.IsStarted) { updater.Start(); }
        }

        private void StopUpdates()
        {
            // Stop updater if started
            if (updater.IsStarted) { updater.Stop(); }
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
        /// Gets or sets a value that indicates how often the display will be updated in milliseconds.
        /// </summary>
        /// <value>
        /// A value that indicates how often the display will be updated in milliseconds. The default is 1000.
        /// </value>
        /// <remarks>
        /// <see cref="GraphicsDisplayPanel"/> will attempt to achieve the target rate but 
        /// the highest possible rate is bound to the CPU and transfer speed of the display. 
        /// </remarks>
        [DefaultValue(1000)]
        public uint UpdateInterval
        {
            get
            {
                return updater.UpdateInterval;
            }
            set
            {
                updater.UpdateInterval = value;
            }
        }
        #endregion // Public Properties
        #endregion // Instance Version
    }
}
