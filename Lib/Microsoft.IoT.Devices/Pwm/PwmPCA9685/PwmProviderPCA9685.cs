using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Pwm.Provider;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Pwm.PwmPCA9685
{
    /// <summary>
    /// Initializes a new <see cref="PwmProviderPCA9685"/> instance.
    /// </summary>
    /// <remarks>
    /// Usage:<br/>
    /// <code>
    /// var controllers = await Windows.Devices.Pwm.PwmController.GetControllersAsync(
    ///     new Microsoft.IoT.Devices.Pwm.PCA9685PwmProvider(0x40));
    /// var pwm = controllers[0];
    /// pwm.SetDesiredFrequency(1200);
    /// using (var pin = pwm.OpenPin(0))
    /// {
    ///     pin.SetActiveDutyCyclePercentage(100.0);
    ///     pin.Start();
    /// } // end using will disppose the pin and return it to its power on state
    /// </code>
    /// </remarks>
    public sealed class PwmProviderPCA9685 : IPwmProvider, IDisposable
    {
        private List<IPwmControllerProvider> providers;
        private byte i2caddress;

        /// <summary>
        /// Initializes a new <see cref="PwmProviderPCA9685"/> instance.
        /// </summary>
        /// <param name="address">The I2C address of this PWM controller.</param>
        public PwmProviderPCA9685(byte address)
        {
            providers = new List<IPwmControllerProvider>();
            i2caddress = address;
        }

        /// <summary>
        /// Initializes a new <see cref="PwmProviderPCA9685"/> instance at the default address 0x40.
        /// </summary>
        public PwmProviderPCA9685() : this(0x40) { }

        /// <summary>
        /// Gets the controllers available on the system.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IPwmControllerProvider> IPwmProvider.GetControllers()
        {
            if (providers.Count == 0)
            {
                providers.Add(new PwmControllerProviderPCA9685(i2caddress));
            }

            return providers;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    for (int i = providers.Count - 1; i >= 0; i--)
                    {
                        var provider = providers[i] as IDisposable;
                        if (provider != null) { provider.Dispose(); }
                        providers.RemoveAt(i);
                    }
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
