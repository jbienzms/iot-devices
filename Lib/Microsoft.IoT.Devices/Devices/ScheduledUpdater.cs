// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using IoTScheduler = Microsoft.IoT.Devices.Scheduler;

namespace Microsoft.IoT.Devices
{
    public sealed class ScheduledUpdater : IDisposable, IEventObserver
    {
        #region Member Variables
        private IAsyncAction asyncUpdateAction;
        private ScheduleOptions defaultScheduleOptions;
        private uint eventsSubscribed;
        private bool scheduled;
        private IScheduler scheduler;
        private ScheduleOptions scheduleOptions;
        private Delegate updateAction;
        #endregion // Member Variables


        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="ScheduledUpdater"/> instance.
        /// </summary>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        /// <param name="scheduler">
        /// The scheduler that will be used to schedule updates.
        /// </param>
        public ScheduledUpdater(ScheduleOptions scheduleOptions, IScheduler scheduler)
        {
            // Validate
            if (scheduleOptions == null) throw new ArgumentNullException("scheduleOptions");
            if (scheduler == null) throw new ArgumentNullException("scheduler");

            // Store
            this.scheduler = scheduler;
            this.scheduleOptions = scheduleOptions;
            this.defaultScheduleOptions = scheduleOptions;

            // Defaults
            StartUpdatesWithEvents = true;
            StopUpdatesWithEvents = true;
        }

        /// <summary>
        /// Initializes a new <see cref="ScheduledUpdater"/> using the default scheduler.
        /// </summary>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public ScheduledUpdater(ScheduleOptions scheduleOptions) : this(scheduleOptions, IoTScheduler.Default) { }

        #endregion // Constructors


        #region Internal Methods
        private void SetUpdateInterval(uint newInterval)
        {
            // Create new options
            var options = ScheduleOptions.WithNewUpdateInterval(newInterval);

            // Update
            UpdateScheduleOptions(options);
        }

        private void UpdateScheduleOptions(ScheduleOptions options)
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
                if (asyncUpdateAction != null)
                {
                    scheduler.UpdateSchedule(asyncUpdateAction, options);
                }
                else
                {
                    scheduler.UpdateSchedule(updateAction, options);
                }
            }
        }

        private void ValidateUpdateAction()
        {
            if ((asyncUpdateAction == null) && (updateAction == null))
            {
                throw new InvalidOperationException(Strings.NoUpdateAction);
            }
        }
        #endregion // Internal Methods


        #region Public Methods
        public void Dispose()
        {
            if (scheduled)
            {
                if (asyncUpdateAction != null)
                {
                    lock (asyncUpdateAction)
                    {
                        scheduler.Unschedule(asyncUpdateAction);
                    }
                }

                if (updateAction != null)
                {
                    lock (updateAction)
                    {
                        scheduler.Unschedule(updateAction);
                    }
                }

                scheduled = false;
            }

            scheduler = null;
        }

        /// <summary>
        /// Sets an asynchronous update action to be called by the scheduler.
        /// </summary>
        /// <param name="asyncUpdateAction">
        /// The asynchronous update action.
        /// </param>
        public void SetAsyncUpdateAction(IAsyncAction asyncUpdateAction)
        {
            // Validate
            if (asyncUpdateAction == null) throw new ArgumentNullException("asyncUpdateAction");
            if (scheduled) { throw new InvalidOperationException(Strings.ExistingUpdateAction); }

            // Store
            this.updateAction = null;
            this.asyncUpdateAction = asyncUpdateAction;
        }

        /// <summary>
        /// Sets a synchronous update action to be called by the scheduler.
        /// </summary>
        /// <param name="updateAction">
        /// The synchronous update action.
        /// </param>
        public void SetUpdateAction(ScheduledAction updateAction)
        {
            // Validate
            if (updateAction == null) throw new ArgumentNullException("updateAction");
            if (scheduled) { throw new InvalidOperationException(Strings.ExistingUpdateAction); }

            // Store
            this.asyncUpdateAction = null;
            this.updateAction = updateAction;
        }

        /// <summary>
        /// Starts executing updates with the scheduler.
        /// </summary>
        public void StartUpdates()
        {
            ValidateUpdateAction();
            if (asyncUpdateAction != null)
            {
                lock (asyncUpdateAction)
                {
                    if (scheduled)
                    {
                        scheduler.Resume(asyncUpdateAction);
                    }
                    else
                    {
                        scheduler.Schedule(asyncUpdateAction, scheduleOptions);
                        scheduled = true;
                    }
                }
            }
            else
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
        }

        /// <summary>
        /// Stops updates from being executed by the scheduler.
        /// </summary>
        public void StopUpdates()
        {
            if (scheduled)
            {
                if (asyncUpdateAction != null)
                {
                    lock (asyncUpdateAction)
                    {
                        // Suspend instead of unschedule to maintain registration sequence.
                        // This is important in case the synchronous update order matters.
                        scheduler.Suspend(asyncUpdateAction);
                    }
                }
                else
                {
                    lock (updateAction)
                    {
                        // Suspend instead of unschedule to maintain registration sequence.
                        // This is important in case the synchronous update order matters.
                        scheduler.Suspend(updateAction);
                    }
                }
            }
        }
        #endregion // Public Methods

        #region Public Properties
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
        public bool StartUpdatesWithEvents { get; set; }

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
        public bool StopUpdatesWithEvents { get; set; }

        /// <summary>
        /// Gets the scheduler that is providing updates.
        /// </summary>
        /// <value>
        /// The scheduler that is providing updates.
        /// </value>
        public IScheduler Scheduler { get; }

        /// <summary>
        /// Gets the schedule options currently being used by the scheduler.
        /// </summary>
        public ScheduleOptions ScheduleOptions { get; }

        /// <summary>
        /// Gets or sets the update interval.
        /// </summary>
        /// <value>
        /// The current update interval. 
        /// </value>
        /// <remarks>
        /// The update interval will be set to a default value that will vary 
        /// based on the devices implementation. If your app does not 
        /// want to use this default value, you should set the update interval 
        /// to a non-zero value prior to registering any event handlers.
        /// </remarks>
        public uint UpdateInterval
        {
            get
            {
                return ScheduleOptions.UpdateInterval;
            }
            set
            {
                // Changing?
                if (value != ScheduleOptions.UpdateInterval)
                {
                    // New value or default?
                    if (value == 0)
                    {
                        SetUpdateInterval(defaultScheduleOptions.UpdateInterval);
                    }
                    else
                    {
                        SetUpdateInterval(value);
                    }
                }
            }
        }
        #endregion // Public Properties

        #region IEventObserver Interface
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
        #endregion // IEventObserver Interface
    }
}
