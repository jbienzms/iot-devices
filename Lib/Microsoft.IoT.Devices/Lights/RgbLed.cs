// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore.Lights;
using Windows.Devices.Pwm;
using Windows.UI;
using Microsoft.IoT.DeviceHelpers;

namespace Microsoft.IoT.Devices.Lights
{
    /// <summary>
    /// A light capable of producing multiple colors by adjusting red, green and blue values. This 
    /// class implements the <see cref="ILight"/> interface.
    /// </summary>
    /// <remarks>
    /// Each color component is represented by a <see cref="PwmPin"/>. All three color components are 
    /// <i>not</i> required but at least one <see cref="PwmPin"/> must be specified before setting the 
    /// <see cref="Color"/> property. Setting the color to a value that cannot be displayed by the 
    /// hardware configuration will not raise an error. For example, setting <see cref="Color"/> to 
    /// <see cref="Colors.Blue"/> when only the <see cref="GreenPin"/> is connected will not result 
    /// in an exception (though obviously no color will be shown).
    /// </remarks>
    sealed public class RgbLed : ILight, IDisposable
    {
        #region Member Variables
        private PwmPin bluePin;
        private float brightnessLevel = 1.0f;
        private Color color = Colors.Black;
        private PwmPin greenPin;
        private bool isInitialized;
        private PwmPin redPin;
        #endregion // Member Variables

        #region Internal Methods
        private void EnsureInitialized()
        {
            if (isInitialized) { return; }

            // Validate that the pin has been set
            if ((bluePin == null) && (greenPin == null) && (redPin == null)) { throw new MissingIoException(string.Format("{0}, {1} or {2}", nameof(BluePin), nameof(GreenPin), nameof(RedPin))); }
            /*
            if (bluePin == null) { throw new MissingIoException(nameof(BluePin)); }
            if (greenPin == null) { throw new MissingIoException(nameof(GreenPin)); }
            if (redPin == null) { throw new MissingIoException(nameof(RedPin)); }
            */

            // Make sure all pins are started
            if ((bluePin != null) && (!bluePin.IsStarted)) { bluePin.Start(); }
            if ((greenPin != null) && (!greenPin.IsStarted)) { greenPin.Start(); }
            if ((redPin != null) && (!redPin.IsStarted)) { redPin.Start(); }

            // Consider ourselves initialized now
            isInitialized = true;

            // Go to default color
            UpdateLed();
        }

        private void UpdateLed()
        {
            // Make sure we're initialized
            EnsureInitialized();

            // Figure out individual color duty percentages
            double redPercent = (((double)color.R) / 255d) * brightnessLevel;
            double greenPercent = (((double)color.G) / 255d) * brightnessLevel;
            double bluePercent = (((double)color.B) / 255d) * brightnessLevel;

            // Update pins
            if (redPin != null) { redPin.SetActiveDutyCyclePercentage(redPercent); }
            if (greenPin != null) { greenPin.SetActiveDutyCyclePercentage(greenPercent); }
            if (bluePin != null) { bluePin.SetActiveDutyCyclePercentage(bluePercent); }
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public void Dispose()
        {
            if (bluePin != null)
            {
                if (bluePin.IsStarted) { bluePin.Stop(); }
                bluePin.Dispose();
                bluePin = null;
            }
            if (greenPin != null)
            {
                if (greenPin.IsStarted) { greenPin.Stop(); }
                greenPin.Dispose();
                greenPin = null;
            }
            if (redPin != null)
            {
                if (redPin.IsStarted) { redPin.Stop(); }
                redPin.Dispose();
                redPin = null;
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets the pin that the blue component is connected to.
        /// </summary>
        public PwmPin BluePin
        {
            get
            {
                return bluePin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                bluePin = value;
            }
        }

        /// <inheritdoc/>
        [DefaultValue(1.0f)]
        public float BrightnessLevel
        {
            get
            {
                return brightnessLevel;
            }
            set
            {
                if (value != brightnessLevel)
                {
                    if ((value < 0) || (value > 1)) throw new ArgumentOutOfRangeException("value");
                    brightnessLevel = value;
                    UpdateLed();
                }
            }
        }

        /// <inheritdoc/>
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                if (value != color)
                {
                    color = value;
                    UpdateLed();
                }
            }
        }

        /// <summary>
        /// Gets or sets the pin that the green component is connected to.
        /// </summary>
        public PwmPin GreenPin
        {
            get
            {
                return greenPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                greenPin = value;
            }
        }

        /// <inheritdoc/>
        public bool IsColorSettable
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets the pin that the red component is connected to.
        /// </summary>
        public PwmPin RedPin
        {
            get
            {
                return redPin;
            }
            set
            {
                if (isInitialized) { throw new IoChangeException(); }
                redPin = value;
            }
        }
        #endregion // Public Properties
    }
}
