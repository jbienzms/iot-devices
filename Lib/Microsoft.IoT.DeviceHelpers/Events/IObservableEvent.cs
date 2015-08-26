// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.IoT.DeviceHelpers
{
    public interface IObservableEvent<TSender, TResult>
    {
        /// <summary>
        /// Adds a handler to the observed event.
        /// </summary>
        /// <param name="handler">
        /// The handler to add.
        /// </param>
        /// <returns>
        /// A token that can be used to remove the event handler from the 
        /// invocation list.
        /// </returns>
        EventRegistrationToken Add(TypedEventHandler<TSender, TResult> handler);

        /// <summary>
        /// Raises the observed event if there is at least one subscriber.
        /// </summary>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="args">
        /// Data for the event.
        /// </param>
        void Raise(TSender sender, TResult args);

        /// <summary>
        /// Removes a handler from the observed event.
        /// </summary>
        /// <param name="handler">
        /// The handler to remove.
        /// </param>
        [DefaultOverload]
        void Remove(TypedEventHandler<TSender, TResult> handler);

        /// <summary>
        /// Removes a handler from the observed event.
        /// </summary>
        /// <param name="token">
        /// The token to remove.
        /// </param>
        void Remove(EventRegistrationToken token);
    }
}
