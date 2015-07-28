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
    public sealed class EventAggregator : IEventObserver
    {
        #region Member Variables
        private uint eventsSubscribed;
        private bool isStarted;
        private ScheduledAction startAction;
        private ScheduledAction stopAction;
        #endregion // Member Variables


        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="EventAggregator"/> instance.
        /// </summary>
        public EventAggregator()
        {
            // Defaults
            StartWithEvents = true;
            StopWithEvents = true;
        }
        #endregion // Constructors


        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the updater is currently providing updates.
        /// </summary>
        /// <value>
        /// <c>true</c> if the updater is currently providing updates; otherwise false.
        /// </value>
        public bool IsStarted { get { return isStarted; } }

        /// <summary>
        /// Gets the action that will be called when the first event is subscribed.
        /// </summary>
        public ScheduledAction StartAction { get { return startAction; } set { startAction = value; } }

        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Start"/> will 
        /// get called when the first event is subscribed to.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Start"/> will get called when the 
        /// first event is subscribed to; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="SchedulingEvent"/> 
        /// participate in auto start and stop.
        /// </remarks>
        public bool StartWithEvents { get; set; }

        /// <summary>
        /// Gets the action that will be called when the last event is unsubscribed.
        /// </summary>
        public ScheduledAction StopAction { get { return stopAction; } set { stopAction = value; } }

        /// <summary>
        /// Gets or sets a value that indicates if <see cref="Stop"/> will 
        /// get called when the last event is unsubscribed.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="Stop"/> will get called when the 
        /// last event is unsubscribed; otherwise false. The default is <c>true</c>.
        /// </value>
        /// <remarks>
        /// Only events that are internally implemented using <see cref="SchedulingEvent"/> 
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


        #region Public Events
        /// <summary>
        /// Occurs right before updates are started with the scheduler.
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Occurs right after updates have been started with the scheduler.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Occurs right before updates are stopped with the scheduler.
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Occurs right after updates are stopped with the scheduler.
        /// </summary>
        public event EventHandler Stopped;
        #endregion // Public Events

        #region IEventObserver Interface
        void IEventObserver.FirstHandlerAdded(object sender)
        {
            eventsSubscribed++;
            if ((eventsSubscribed == 1) && (StartWithEvents))
            {
                Start();
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
            if ((eventsSubscribed == 0) && (StopWithEvents))
            {
                Stop();
            }
        }
        #endregion // IEventObserver Interface
    }
}
