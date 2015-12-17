using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation.Metadata;

namespace SolarSystem.IO
{
    /// <summary>
    /// Manages IO Devices, e.g. LEDs connected to GPIO pins.
    /// </summary>
    public class IoManager : IDisposable
    {
        #region Member Variables
        private GpioController controller;
        private bool isEnabled;
        private Dictionary<int, GpioPin> pinCache = new Dictionary<int, GpioPin>();
        #endregion // Member Variables


        #region Internal Methods
        private GpioPin GetPin(int pin)
        {
            // If not already opened, open it
            if (!pinCache.ContainsKey(pin))
            {
                var newPin = controller.OpenPin(pin);
                newPin.SetDriveMode(GpioPinDriveMode.Output);
                pinCache[pin] = newPin;
            }

            // Return from cache
            return pinCache[pin];
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public void Dispose()
        {
            if (pinCache != null)
            {
                foreach (var pin in pinCache.Values)
                {
                    pin.Dispose();
                }
                pinCache = null;
            }
        }

        /// <summary>
        /// Initializes the IO Manager.
        /// </summary>
        public void Initialize()
        {
            // Check for GPIO support
            if (ApiInformation.IsTypePresent("Windows.Devices.Gpio.GpioController"))
            {
                // Get the default GPIO controller
                controller = GpioController.GetDefault();

                // Only enabled if controller was found
                isEnabled = (controller != null);
            }
        }

        /// <summary>
        /// Turns only the pins specified on and turns all other previous pins off.
        /// </summary>
        /// <param name="pins">
        /// The pins to turn on.
        /// </param>
        /// <remarks>
        /// If the array has no elements, existing pins will be turned off but none will be turned on.
        /// </remarks>
        public void SetExclusive(params int[] pins)
        {
            //  If not enabled just ignore
            if (!isEnabled) { return; }

            // Off phase
            foreach (var existingPin in pinCache)
            {
                if ((pins == null) || (!pins.Contains(existingPin.Key)))
                {
                    // Turn off
                    existingPin.Value.Write(GpioPinValue.Low);
                }
            }

            // On phase
            if (pins != null)
            {
                foreach (var pin in pins)
                {
                    // Turn on
                    GetPin(pin).Write(GpioPinValue.High);
                }
            }
        }

        /// <summary>
        /// Turns the specified pins off.
        /// </summary>
        /// <param name="pins">
        /// The pins to turn off.
        /// </param>
        /// <remarks>
        /// If the array has no elements this method has no effect.
        /// </remarks>
        public void SetOff(params int[] pins)
        {
            //  If not enabled just ignore
            if (!isEnabled) { return; }

            // Off phase
            if (pins != null)
            {
                foreach (var pin in pins)
                {
                    // Turn off
                    GetPin(pin).Write(GpioPinValue.Low);
                }
            }
        }

        /// <summary>
        /// Turns the specified pins on.
        /// </summary>
        /// <param name="pins">
        /// The pins to turn on.
        /// </param>
        /// <remarks>
        /// If the array has no elements this method has no effect.
        /// </remarks>
        public void SetOn(params int[] pins)
        {
            //  If not enabled just ignore
            if (!isEnabled) { return; }

            // On phase
            if (pins != null)
            {
                foreach (var pin in pins)
                {
                    // Turn on
                    GetPin(pin).Write(GpioPinValue.High);
                }
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the IO Manager is available and connected.
        /// </summary>
        /// <c>true</c> if the IO Manager is available and connected; otherwise <c>false</c>.
        public bool IsEnabled => isEnabled;
        #endregion // Public Properties
    }
}
