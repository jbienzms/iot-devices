// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceHelpers
{
    /// <summary>
    /// The base class for a monitor of subscribers to an event.
    /// </summary>
    /// <typeparam name="TSender">
    /// The sender type for the event.
    /// </typeparam>
    public class ObservableEvent<TSender, TResult> : IObservableEvent<TSender, TResult>
    {
        #region Member Variables
        private EventRegistrationTokenTable<TypedEventHandler<TSender, TResult>> eventTable;
        private ScheduledAction firstAdded;
        private IEventObserver observer;
        #endregion // Member Variables

        #region Constants
        private ObservableEvent()
        {
            // Create
            eventTable = new EventRegistrationTokenTable<TypedEventHandler<TSender, TResult>>();
        }

        /// <summary>
        /// Initializes a new <see cref="ObservableEvent"/>.
        /// </summary>
        /// <param name="observer">
        /// An <see cref="IEventObserver"/> that monitors the event.
        /// </param>
        public ObservableEvent(IEventObserver observer) : this()
        {
            // Validate
            if (observer == null) throw new ArgumentNullException("observer");

            // Store
            this.observer = observer;
        }

        /// <summary>
        /// Initializes a new <see cref="ObservableEvent"/>.
        /// </summary>
        /// <param name="observer">
        /// An <see cref="IEventObserver"/> that monitors the event.
        /// </param>
        public ObservableEvent(ScheduledAction firstAdded) : this()
        {
            // Validate
            if (firstAdded == null) throw new ArgumentNullException("firstAddedAction");

            // Store
            this.firstAdded = firstAdded;
        }
        #endregion // Constants

        private void InnerRemove(TypedEventHandler<TSender, TResult> handler, Func<EventRegistrationToken> token)
        {
            // Check if we handlers
            bool wasNull = (eventTable.InvocationList == null);

            // Remove
            if (handler != null)
            {
                eventTable.RemoveEventHandler(handler);
            }
            else
            {
                eventTable.RemoveEventHandler(token());
            }

            // Notify of remove
            if (observer != null) { observer.Removed(this); }

            // If last handler, notify
            if ((!wasNull) && (eventTable.InvocationList == null))
            {
                if (observer != null) { observer.LastRemoved(this); }
            }
        }

        #region Public Methods
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
        public EventRegistrationToken Add(TypedEventHandler<TSender, TResult> handler)
        {
            // Check if we currently have no subscribers
            bool wasNull = (eventTable.InvocationList == null);

            // Add
            var reg = eventTable.AddEventHandler(handler);

            // Notify of add
            if (observer != null) { observer.Added(this); }

            // If first handler, notify
            if ((wasNull) && (eventTable.InvocationList != null))
            {
                if (firstAdded != null) { firstAdded(); }
                if (observer != null) { observer.FirstAdded(this); }
            }

            // Return the registration
            return reg;
        }

        /// <summary>
        /// Raises the observed event if there is at least one subscriber.
        /// </summary>
        /// <param name="sender">
        /// The sender of the event.
        /// </param>
        /// <param name="args">
        /// Data for the event.
        /// </param>
        public void Raise(TSender sender, TResult args)
        {
            // Get innner event and call if there are subscribers
            var innerEvent = eventTable.InvocationList;
            if (innerEvent != null)
            {
                innerEvent(sender, args);
            }
        }

        /// <summary>
        /// Removes a handler from the observed event.
        /// </summary>
        /// <param name="removed">
        /// The handler to remove.
        /// </param>
        public void Remove(TypedEventHandler<TSender, TResult> handler)
        {
            InnerRemove(handler, null);
        }

        /// <summary>
        /// Removes a handler from the observed event.
        /// </summary>
        /// <param name="token">
        /// The token to remove.
        /// </param>
        public void Remove(EventRegistrationToken token)
        {
            InnerRemove(null, ()=>token);
        }
        #endregion // Public Methods
    }
}
