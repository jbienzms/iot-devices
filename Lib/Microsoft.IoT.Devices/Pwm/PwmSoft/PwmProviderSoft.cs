using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Pwm.Provider;
using Windows.Foundation;

namespace Microsoft.IoT.Devices.Pwm.PwmSoft
{
    /// <summary>
    /// Initializes a new <see cref="PwmProviderSoft"/> instance.
    /// </summary>
    public sealed class PwmProviderSoft : IPwmProvider, IDisposable
    {
        private List<IPwmControllerProvider> providers;

        /// <summary>
        /// Initializes a new <see cref="PwmProviderSoft"/> instance.
        /// </summary>
        public PwmProviderSoft()
        {
            providers = new List<IPwmControllerProvider>();
        }

        /// <summary>
        /// Gets the controllers available on the system.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IPwmControllerProvider> IPwmProvider.GetControllers()
        {
            if (providers.Count == 0)
            {
                providers.Add(new PwmControllerProviderSoft());
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
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
