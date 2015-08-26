// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceCore
{
    /// <summary>
    /// Indicates the requested priority level that a subscriber is scheduled.
    /// </summary>
    public enum SchedulerPriority
    {
        /// <summary>
        /// The subscriber will be scheduled at the same priority as other subscribers.
        /// </summary>
        Default,

        /// <summary>
        /// The subscriber will be scheduled at a higher priority than other subscribers.
        /// </summary>
        High
    };

    /// <summary>
    /// Represents the options for a subscription with a scheduler.
    /// </summary>
    public sealed class ScheduleOptions
    {
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="ScheduleOptions"/> instance.
        /// </summary>
        /// <param name="updateInterval">
        /// The requested update interval for the subscriber.
        /// </param>
        /// <param name="priority">
        /// The requested update priority for the subscriber.
        /// </param>
        /// <remarks>
        /// The report interval is specified in milliseconds.
        /// </remarks>
        public ScheduleOptions(uint updateInterval, SchedulerPriority priority)
        {
            this.UpdateInterval = updateInterval;
            this.Priority = priority;
        }

        /// <summary>
        /// Initializes a new <see cref="ScheduleOptions"/> instance with a default priority.
        /// </summary>
        /// <param name="reportInterval">
        /// The requested update interval for the subscriber.
        /// </param>
        /// <remarks>
        /// The report interval is specified in milliseconds.
        /// </remarks>
        public ScheduleOptions(uint reportInterval) : this(reportInterval, SchedulerPriority.Default) { }
        #endregion // Constructors

        #region Public Methods
        /// <summary>
        /// Returns a schedule options with a new update interval.
        /// </summary>
        /// <param name="updateInterval"></param>
        /// <returns>
        /// The new options.
        /// </returns>
        public ScheduleOptions WithNewUpdateInterval(uint updateInterval)
        {
            return new ScheduleOptions(updateInterval, this.Priority);
        }

        /// <summary>
        /// Returns new schedule options with an updated priority.
        /// </summary>
        /// <param name="priority">
        /// The new priority.
        /// </param>
        /// <returns>
        /// The new options.
        /// </returns>
        public ScheduleOptions WithNewPriority(SchedulerPriority priority)
        {
            return new ScheduleOptions(this.UpdateInterval, priority);
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets the requested update priority for the subscriber.
        /// </summary>
        public SchedulerPriority Priority { get; private set; }

        /// <summary>
        /// Gets the requested update interval for the subscriber.
        /// </summary>
        /// <value>
        /// The requested update interval for the subscriber.
        /// </value>
        /// <remarks>
        /// The report interval is specified in milliseconds. 
        /// </remarks>
        public uint UpdateInterval { get; private set; }
        #endregion // Public Properties
    }
}
