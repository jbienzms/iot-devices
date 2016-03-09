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
    /// A class that provides an observable event.
    /// </summary>
    /// <typeparam name="TSender">
    /// The type of object that raises the event.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of result (or args) passed to event handlers.
    /// </typeparam>
    /// <remarks>
    /// <see cref="ObservableEvent{TSender, TResult}"/> is a helper class that creates Windows Runtime 
    /// compatible events whose subscriptions can be monitored. Subscriptions can be monitored by 
    /// passing a class that implements <see cref="IEventObserver"/> into the constructor.
    /// </remarks>
    public class ObservableEvent<TSender, TResult> : IObservableEvent<TSender, TResult>
    {
        #region Member Variables
        private EventRegistrationTokenTable<TypedEventHandler<TSender, TResult>> eventTable;
        private ScheduledAction firstAdded;
        private ScheduledAction lastRemoved;
        private IEventObserver observer;
        #endregion // Member Variables

        #region Constructors
        private ObservableEvent()
        {
            // Create
            eventTable = new EventRegistrationTokenTable<TypedEventHandler<TSender, TResult>>();
        }

        /// <summary>
        /// Initializes a new <see cref="ObservableEvent{TSender, TResult}"/>.
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
        /// Initializes a new <see cref="ObservableEvent{TSender, TResult}"/>.
        /// </summary>
        /// <param name="firstAdded">
        /// A <see cref="ScheduledAction"/> that will be called when the first subscriber is added.
        /// </param>
        /// <param name="lastRemoved">
        /// A <see cref="ScheduledAction"/> that will be called when the last subscriber is removed.
        /// </param>
        public ObservableEvent(ScheduledAction firstAdded, ScheduledAction lastRemoved = null) : this()
        {
            // Validate
            if ((firstAdded == null) && (lastRemoved == null)) throw new ArgumentNullException($"{nameof(firstAdded)} and {nameof(lastRemoved)} cannot both be null");

            // Store
            this.firstAdded = firstAdded;
            this.lastRemoved = lastRemoved;
        }
        #endregion // Constructors

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
                if (lastRemoved != null) { lastRemoved(); }
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
            // Get inner event and call if there are subscribers
            var innerEvent = eventTable.InvocationList;
            if (innerEvent != null)
            {
                innerEvent(sender, args);
            }
        }

        /// <summary>
        /// Removes a handler from the observed event.
        /// </summary>
        /// <param name="handler">
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
