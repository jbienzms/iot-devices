// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Devices.IoT
{
    public abstract class ScheduledBase : IDisposable, IEventObserver
    {
        #region Member Variables
        private uint eventsSubscribed;
        private bool scheduled;
        private IScheduler scheduler;
        private ScheduleOptions scheduleOptions;
        private Delegate updateAction;
        #endregion // Member Variables


        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="ScheduledBase"/> instance.
        /// </summary>
        /// <param name="scheduler">
        /// The scheduler that will be used to provide updates.
        /// </param>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public ScheduledBase(IScheduler scheduler, ScheduleOptions scheduleOptions)
        {
            // Validate
            if (scheduler == null) throw new ArgumentNullException("scheduler");
            if (scheduleOptions == null) throw new ArgumentNullException("options");

            // Store
            this.scheduler = scheduler;
            this.scheduleOptions = scheduleOptions;

            // Defaults
            StartUpdatesWithEvents = true;
            StopUpdatesWithEvents = true;
        }
        #endregion // Constructors


        #region Internal Methods
        /// <summary>
        /// Sets the update action to be called by the scheduler.
        /// </summary>
        /// <param name="updateAction">
        /// The update action.
        /// </param>
        internal void SetUpdateAction(Delegate updateAction)
        {
            // Validate
            if (updateAction == null) throw new ArgumentNullException("updateAction");

            // Store
            this.updateAction = updateAction;
        }

        /// <summary>
        /// Starts being executed by the scheduler.
        /// </summary>
        protected void StartUpdates()
        {
            lock (updateAction)
            {
                if (scheduled)
                {
                    scheduler.Resume(updateAction);
                }
                else
                {
                    scheduler.Schedule(updateAction, scheduleOptions);
                    scheduled = true;
                }
            }
        }

        /// <summary>
        /// Stops updates from being executed by the scheduler.
        /// </summary>
        protected void StopUpdates()
        {
            lock (updateAction)
            {
                if (scheduled)
                {
                    // Suspend instead of unschedule to maintain registration sequence.
                    // This is important in case the synchronous update order matters.
                    scheduler.Suspend(updateAction);
                }
            }
        }

        internal void UpdateScheduleOptions(ScheduleOptions options)
        {
            // Validate
            if (options == null) throw new ArgumentNullException("options");

            // Ensure changing
            if (options == scheduleOptions) { return; }

            // Update variable first
            scheduleOptions = options;

            // If current scheduled, update the schedule
            if (scheduled)
            {
                scheduler.UpdateSchedule(updateAction, options);
            }
        }
        #endregion // Internal Methods


        #region Public Methods
        public virtual void Dispose()
        {
            lock (updateAction)
            {
                if (scheduled)
                {
                    scheduler.Unschedule(updateAction);
                }
            }

            scheduler = null;
        }

        void IEventObserver.FirstHandlerAdded(object sender)
        {
            eventsSubscribed++;
            if ((eventsSubscribed == 1) && (StartUpdatesWithEvents))
            {
                StartUpdates();
            }
        }

        void IEventObserver.HandlerAdded(object sender)
        {
            
        }

        void IEventObserver.HandlerRemoved(object sender)
        {
            
        }

        void IEventObserver.LastHandlerRemoved(object sender)
        {
            eventsSubscribed--;
            if ((eventsSubscribed == 0) && (StopUpdatesWithEvents))
            {
                StopUpdates();
            }
        }
        #endregion // Public Methods

        #region Internal Properties
        /// <summary>
        /// Gets or sets a value that indicates if <see cref="StartUpdates"/> will 
        /// get called when the first event is subscribed to.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="StartUpdates"/> will get called when the 
        /// first event is subscribed to; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="SchedulingEvent"/> 
        /// participate in auto start and stop.
        /// </remarks>
        protected bool StartUpdatesWithEvents { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if <see cref="StopUpdates"/> will 
        /// get called when the last event is unsubscribed.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="StopUpdates"/> will get called when the 
        /// last event is unsubscribed; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="SchedulingEvent"/> 
        /// participate in auto start and stop.
        /// </remarks>
        protected bool StopUpdatesWithEvents { get; set; }

        /// <summary>
        /// Gets the scheduler that is providing updates.
        /// </summary>
        /// <value>
        /// The scheduler that is providing updates.
        /// </value>
        protected IScheduler Scheduler { get; }

        /// <summary>
        /// Gets the schedule options currently being used by the scheduler.
        /// </summary>
        protected ScheduleOptions ScheduleOptions { get; }
        #endregion // Internal Properties
    }
}
