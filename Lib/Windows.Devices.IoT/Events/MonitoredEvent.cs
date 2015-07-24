// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// The base class for a monitor of subscribers to an event.
    /// </summary>
    /// <typeparam name="TSender">
    /// The sender type for the event.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of data sent to the event.
    /// </typeparam>
    public class MonitoredEvent<TSender, TResult>
    {
        #region Member Variables
        private TypedEventHandler<TSender, TResult> innerEvent;
        #endregion // Member Variables

        #region Overridables / Event Triggers
        /// <summary>
        /// Occurs when the first subscriber was added to the monitored event.
        /// </summary>
        protected virtual void OnFirstAdded() { }

        /// <summary>
        /// Occurs when the last subscriber was removed from the monitored event.
        /// </summary>
        protected virtual void OnLastRemoved() { }
        #endregion // Overridables / Event Triggers

        /// <summary>
        /// Adds a handler to the monitored event.
        /// </summary>
        /// <param name="added">
        /// The handler to add.
        /// </param>
        public void Add(TypedEventHandler<TSender, TResult> added)
        {
            // Check if we currently have no subscribers
            bool wasNull = (innerEvent == null);

            // Combine
            innerEvent += added;

            // First subscriber?
            if ((wasNull) && (innerEvent != null))
            {
                OnFirstAdded();
            }
        }

        /// <summary>
        /// Raises the monitored event if there is at least one subscriber.
        /// </summary>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="args">
        /// Data for the event.
        /// </param>
        public void Raise(TSender sender, TResult args)
        {
            if (innerEvent != null)
            {
                innerEvent(sender, args);
            }
        }

        /// <summary>
        /// Removes a handler from the monitored event.
        /// </summary>
        /// <param name="removed">
        /// The handler to remove.
        /// </param>
        public void Remove(TypedEventHandler<TSender, TResult> removed)
        {
            // Check if we currently have no subscribers
            bool wasNull = (innerEvent == null);

            // Combine
            innerEvent -= removed;

            // First subscriber?
            if ((!wasNull) && (innerEvent == null))
            {
                OnLastRemoved();
            }
        }
    }
}
