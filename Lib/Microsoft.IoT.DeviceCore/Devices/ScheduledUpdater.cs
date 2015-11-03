// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using IoTScheduler = Microsoft.IoT.DeviceCore.Scheduler;

namespace Microsoft.IoT.DeviceCore
{
    /// <summary>
    /// <see cref="ScheduledUpdater"/> is a helper class handles scheduling and unscheduling of 
    /// delegates for execution by an <see cref="IScheduler"/>.
    /// </summary>
    /// <remarks>
    /// <p>
    /// <see cref="ScheduledUpdater"/> is capable of delivering both synchronous and asynchronous 
    /// through the <see cref="SetUpdateAction"/> and <see cref="SetAsyncUpdateAction"/> methods 
    /// respectively. 
    /// </p>
    /// <p>
    /// <see cref="ScheduledUpdater"/> is commonly used in device classes to schedule polling of 
    /// device data. Devices that share the same <see cref="IScheduler"/> automatically share 
    /// time slices on a CPU. Whenever a <see cref="ScheduledUpdater"/> is created using a 
    /// constructor that doesn't explicitly receive an <see cref="IScheduler"/> instance, 
    /// <see cref="IoTScheduler.Default"/> is automatically used.
    /// </p>
    /// <p>
    /// Because <see cref="ScheduledUpdater"/> is commonly used by devices (especially sensors) 
    /// there is a common need is to start updates when one or more device events are subscribed to 
    /// then stop updates when the last subscription is removed. To help with this common scenario, 
    /// <see cref="ScheduledUpdater"/> implements the <see cref="IEventObserver"/> interface which 
    /// means it can be passed to the constructor of any ObservableEvent (included in the 
    /// Microsoft.IoT.DeviceHelpers library).
    /// </p>
    /// </remarks>
    public sealed class ScheduledUpdater : IDisposable, IEventObserver
    {
        #region Member Variables
        private ScheduledAsyncAction asyncUpdateAction;
        private ScheduleOptions defaultScheduleOptions;
        private uint eventsSubscribed;
        private bool isStarted;
        private bool scheduled;
        private IScheduler scheduler;
        private ScheduleOptions scheduleOptions;
        private ScheduledAction updateAction;
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
            StartWithEvents = true;
            StopWithEvents = true;
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
        /// <inheritdoc/>
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
        public void SetAsyncUpdateAction(ScheduledAsyncAction asyncUpdateAction)
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
        public void Start()
        {
            // Validate
            ValidateUpdateAction();

            // Notify starting
            if (Starting != null) { Starting(this, EmptyEventArgs.Instance); }

            // Actually start
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

            // Notify started
            isStarted = true;
            if (Started != null) { Started(this, EmptyEventArgs.Instance); }
        }

        /// <summary>
        /// Stops updates from being executed by the scheduler.
        /// </summary>
        public void Stop()
        {
            // If not scheduled, ignore
            if (!scheduled) { return; }

            // Notify stopping
            if (Stopping != null) { Stopping(this, EmptyEventArgs.Instance);  }

            // Actually stop
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

            // Notify stopped
            isStarted = false;
            if (Stopped != null) { Stopped(this, EmptyEventArgs.Instance); }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the updater is currently providing updates.
        /// </summary>
        /// <value>
        /// <c>true</c> if the updater is currently providing updates; otherwise false.
        /// </value>
        public bool IsStarted { get { return isStarted; } }

        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Start"/> will 
        /// get called when the first event is subscribed to.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Start"/> will get called when the 
        /// first event is subscribed to; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="ScheduledUpdater"/> 
        /// participate in auto start and stop.
        /// </remarks>
        public bool StartWithEvents { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Stop"/> will 
        /// get called when the last event is unsubscribed.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Stop"/> will get called when the 
        /// last event is unsubscribed; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="ScheduledUpdater"/> 
        /// participate in auto start and stop.
        /// </remarks>
        public bool StopWithEvents { get; set; }

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
        public ScheduleOptions ScheduleOptions { get { return scheduleOptions; } }

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
                return scheduleOptions.UpdateInterval;
            }
            set
            {
                // Changing?
                if (value != scheduleOptions.UpdateInterval)
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


        #region Public Events
        /// <summary>
        /// Occurs right before updates are started with the scheduler.
        /// </summary>
        public event TypedEventHandler<ScheduledUpdater, EmptyEventArgs> Starting;

        /// <summary>
        /// Occurs right after updates have been started with the scheduler.
        /// </summary>
        public event TypedEventHandler<ScheduledUpdater, EmptyEventArgs> Started;

        /// <summary>
        /// Occurs right before updates are stopped with the scheduler.
        /// </summary>
        public event TypedEventHandler<ScheduledUpdater, EmptyEventArgs> Stopping;

        /// <summary>
        /// Occurs right after updates are stopped with the scheduler.
        /// </summary>
        public event TypedEventHandler<ScheduledUpdater, EmptyEventArgs> Stopped;
        #endregion // Public Events

        #region IEventObserver Interface
        void IEventObserver.FirstAdded(object sender)
        {
            eventsSubscribed++;
            if ((eventsSubscribed == 1) && (StartWithEvents))
            {
                Start();
            }
        }

        void IEventObserver.Added(object sender)
        {

        }

        void IEventObserver.Removed(object sender)
        {

        }

        void IEventObserver.LastRemoved(object sender)
        {
            eventsSubscribed--;
            if ((eventsSubscribed == 0) && (StopWithEvents))
            {
                Stop();
            }
        }
        #endregion // IEventObserver Interface
    }
}
