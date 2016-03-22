// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Adc;
using Windows.Devices.Adc.Provider;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore.Adc
{
    /// <summary>
    /// Allows multiple ADC controllers from various sources to be managed as a simple 
    /// disposable collection.
    /// </summary>
    /// <remarks>
    /// All providers should be added to the <see cref="Providers"/> collection 
    /// before calling <see cref="AdcProviderManager.GetControllersAsync">GetControllersAsync</see>.
    /// </remarks>
    public sealed class AdcProviderManager : IAdcProvider, IDisposable
    {
        #region Member Variables
        private List<IAdcProvider> providers;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="AdcProviderManager"/>.
        /// </summary>
        public AdcProviderManager()
        {
            providers = new List<IAdcProvider>();
        }
        #endregion // Constructors

        #region IAdcProvider Interface
        IReadOnlyList<IAdcControllerProvider> IAdcProvider.GetControllers()
        {
            var controllers = new List<IAdcControllerProvider>();
            for (int i = 0; i < providers.Count; i++)
            {
                controllers.AddRange(providers[i].GetControllers());
            }
            return controllers;
        }
        #endregion // IAdcProvider Interface

        #region Public Methods
        /// <inheritdoc/>
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
        /// Gets the <see cref="AdcController"/> instances for each controller provider.
        /// </summary>
        /// <returns>
        /// An <see cref="IAsyncOperation{TResult}"/> that yields the list of controllers.
        /// </returns>
        public IAsyncOperation<IReadOnlyList<AdcController>> GetControllersAsync()
        {
            return AdcController.GetControllersAsync(this);
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets the collection of providers stored in the manager.
        /// </summary>
        /// <value>
        /// The collection of providers stored in the manager.
        /// </value>
        public IList<IAdcProvider> Providers
        {
            get
            {
                return providers;
            }
        }
        #endregion // Public Properties
    }
}
