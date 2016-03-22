// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Pwm;
using Windows.Devices.Pwm.Provider;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore.Pwm
{
    /// <summary>
    /// Allows multiple PWM controllers from various sources to be managed as a simple 
    /// disposable collection.
    /// </summary>
    /// <remarks>
    /// All providers should be added to the <see cref="Providers"/> collection 
    /// before calling <see cref="PwmProviderManager.GetControllersAsync">GetControllersAsync</see>.
    /// </remarks>
    public sealed class PwmProviderManager : IPwmProvider, IDisposable
    {
        #region Member Variables
        private List<IPwmProvider> providers;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PwmProviderManager"/>.
        /// </summary>
        public PwmProviderManager()
        {
            providers = new List<IPwmProvider>();
        }
        #endregion // Constructors

        #region IPwmProvider Interface
        IReadOnlyList<IPwmControllerProvider> IPwmProvider.GetControllers()
        {
            var controllers = new List<IPwmControllerProvider>();
            for (int i = 0; i < providers.Count; i++)
            {
                controllers.AddRange(providers[i].GetControllers());
            }
            return controllers;
        }
        #endregion // IPwmProvider Interface

        #region Public Methods
        /// <inheritdoc/>
        public void Dispose()
        {
            // Dispose and remove each provider
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                var provider = providers[i] as IDisposable;
                if (provider != null) { provider.Dispose(); }
                providers.RemoveAt(i);
            }
        }

        /// <summary>
        /// Gets a collection of <see cref="PwmController"/> instances that represent all controllers returned by all providers.
        /// </summary>
        /// <returns>
        /// An IAsyncOperation that yields the controllers.
        /// </returns>
        public IAsyncOperation<IReadOnlyList<PwmController>> GetControllersAsync()
        {
            return PwmController.GetControllersAsync(this);
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets the collection of providers stored in the manager.
        /// </summary>
        /// <value>
        /// The collection of providers stored in the manager.
        /// </value>
        public IList<IPwmProvider> Providers
        {
            get
            {
                return providers;
            }
        }
        #endregion // Public Properties
    }
}
