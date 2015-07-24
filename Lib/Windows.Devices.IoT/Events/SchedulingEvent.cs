// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// An event that can schedule its owner based on subscriptions.
    /// </summary>
    /// <typeparam name="TOwner">
    /// The sender type for the event.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The type of data sent to the event.
    /// </typeparam>
    public class SchedulingEvent<TOwner, TResult> : MonitoredEvent<TOwner, TResult> where TOwner:ScheduledBase
    {
        #region Member Variables
        private ScheduledBase owner;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="SchedulingEvent"/>.
        /// </summary>
        /// <param name="owner">
        /// The owner of the event.
        /// </param>
        public SchedulingEvent(TOwner owner)
        {
            // Validate
            if (owner == null) throw new ArgumentNullException("owner");

            // Store
            this.owner = owner;
        }
        #endregion // Constructors

        #region Overrides / Event Handlers
        protected override void OnFirstAdded()
        {
            owner.EventSubscribed();
            base.OnFirstAdded();
        }

        protected override void OnLastRemoved()
        {
            owner.EventUnsubscribed();
            base.OnLastRemoved();
        }
        #endregion // Overrides / Event Handlers
    }
}
