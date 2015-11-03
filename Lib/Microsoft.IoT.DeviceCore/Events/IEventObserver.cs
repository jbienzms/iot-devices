// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore
{
    /// <summary>
    /// The interface for a class that observes event subscriptions. 
    /// </summary>
    public interface IEventObserver
    {
        /// <summary>
        /// Called when an event subscription is added.
        /// </summary>
        /// <param name="sender">
        /// The object where the subscription was added.
        /// </param>
        void Added(object sender);

        /// <summary>
        /// Called the first time an event subscription is added.
        /// </summary>
        /// <param name="sender">
        /// The object where the subscription was added.
        /// </param>
        void FirstAdded(object sender);

        /// <summary>
        /// Called when the last event subscription is removed.
        /// </summary>
        /// <param name="sender">
        /// The object where the subscription was removed.
        /// </param>
        void LastRemoved(object sender);

        /// <summary>
        /// Called when an event subscription is removed.
        /// </summary>
        /// <param name="sender">
        /// The object where the subscription was removed.
        /// </param>
        void Removed(object sender);
    }
}
