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
    /// An implementation of <see cref="IPwmProvider"/> that allows multiple PWM 
    /// controllers to be registered as a simple collection.
    /// </summary>
    /// <remarks>
    /// All controllers should be added to the <see cref="Providers"/> collection 
    /// before calling <see cref="PwmProviderManager.GetControllersAsync"/>.
    /// </remarks>
    public sealed class PwmProviderManager : IPwmProvider, IDisposable
    {
        #region Member Variables
        private List<IPwmControllerProvider> providers;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="PwmProviderManager"/>.
        /// </summary>
        public PwmProviderManager()
        {
            providers = new List<IPwmControllerProvider>();
        }
        #endregion // Constructors

        #region Public Methods
        public void Dispose()
        {
            // Dispose and remove each provider
            for (int i = Providers.Count - 1; i >= 0; i--)
            {
                var provider = providers[i] as IDisposable;
                if (provider != null) { provider.Dispose(); }
                Providers.RemoveAt(i);
            }
        }

        /// <summary>
        /// Gets the <see cref="PwmController"/> instances for each controller provider.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncOperation"/> that yields the list of controllers.
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
        public IList<IPwmControllerProvider> Providers
        {
            get
            {
                return providers;
            }
        }
        #endregion // Public Properties

        #region IPwmProvider Interface
        IReadOnlyList<IPwmControllerProvider> IPwmProvider.GetControllers()
        {
            return providers;
        }
        #endregion // IPwmProvider Interface
    }
}
