// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.IoT.Devices.Display;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Microsoft.IoT.Devices.Controls
{
    public sealed class GraphicsDisplayPanel : ContentControl
    {
        /// <summary>
        /// Identifies the <see cref="DisplayHeight"/> dependency property.
        /// </summary>
        static private readonly DependencyProperty displayHeightPropertyField = DependencyProperty.Register("DisplayHeight", typeof(double), typeof(GraphicsDisplayPanel), new PropertyMetadata(64d));
        static internal DependencyProperty DisplayHeightProperty
        {
            get
            {
                return displayHeightPropertyField;
            }
        }

        /// <summary>
        /// Identifies the <see cref="DisplayWidth"/> dependency property.
        /// </summary>
        static private readonly DependencyProperty displayWidthPropertyField = DependencyProperty.Register("DisplayWidth", typeof(double), typeof(GraphicsDisplayPanel), new PropertyMetadata(128d));

        static internal DependencyProperty DisplayWidthProperty
        {
            get
            {
                return displayWidthPropertyField;
            }
        }

        private Panel contentPanel;

        public GraphicsDisplayPanel()
        {
            this.DefaultStyleKey = typeof(GraphicsDisplayPanel);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            contentPanel = FindName("ContentPanel") as Panel;
        }

        public IAsyncAction RenderToAsync(IGraphicsDisplay display)
        {
            // Validate
            if (display == null) throw new ArgumentNullException("display");

            return Task.Run(async () =>
            {
                RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(contentPanel);
                var pixelBuffer = (await renderTargetBitmap.GetPixelsAsync()).AsStream();

                byte[] pixel = new byte[3];

                // Clear the display
                display.Clear();

                using (pixelBuffer)
                {
                    for (int x = 0; x < renderTargetBitmap.PixelWidth; x++)
                    {
                        for (int y = 0; y < renderTargetBitmap.PixelHeight; y++)
                        {
                            // Read three bytes
                            pixelBuffer.Read(pixel, 0, 3);

                            // Convert to color
                            var color = Color.FromArgb(0xFF, pixel[0], pixel[1], pixel[2]);

                            // Write out pixels
                            display.DrawPixel(x, y, color);
                        }
                    }
                }

                // Update the display
                display.Update();

            }).AsAsyncAction();
        }

        /// <summary>
        /// Gets or sets the DisplayHeight of the <see cref="GraphicsDisplayPanel"/>. This is a dependency property.
        /// </summary>
        /// <value>
        /// The DisplayHeight of the <see cref="GraphicsDisplayPanel"/>. The default is 64.
        /// </value>
        [DefaultValue(64d)]
        public double DisplayHeight
        {
            get
            {
                return (double)GetValue(DisplayHeightProperty);
            }
            set
            {
                SetValue(DisplayHeightProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the DisplayWidth of the <see cref="GraphicsDisplayPanel"/>. This is a dependency property.
        /// </summary>
        /// <value>
        /// The DisplayWidth of the <see cref="GraphicsDisplayPanel"/>. The default is 128.
        /// </value>
        [DefaultValue(128d)]
        public double DisplayWidth
        {
            get
            {
                return (double)GetValue(DisplayWidthProperty);
            }
            set
            {
                SetValue(DisplayWidthProperty, value);
            }
        }
    }
}
