// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using IoTScheduler = Windows.Devices.IoT.Scheduler;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// The internal base class for a device that is updated by a scheduler.
    /// </summary>
    public abstract class ScheduledDeviceBase : ScheduledBase
    {
        #region Member Variables
        private ScheduleOptions defaultScheduleOptions;
        #endregion // Member Variables


        #region Constructors
        public ScheduledDeviceBase(IScheduler scheduler, ScheduleOptions scheduleOptions) : base(scheduler, scheduleOptions)
        {
            // Store
            this.defaultScheduleOptions = scheduleOptions;
        }
        #endregion // Constructors


        #region Internal Methods
        private void UpdateReportInterval(uint newInterval)
        {
            // Create new options
            var options = ScheduleOptions.WithNewReportInterval(newInterval);

            // Call base to update
            base.UpdateScheduleOptions(options);
        }
        #endregion // Internal Methods


        #region Public Properties
        /// <summary>
        /// Gets or sets the current report interval.
        /// </summary>
        /// <value>
        /// The current report interval. 
        /// </value>
        /// <remarks>
        /// The report interval will be set to a default value that will vary 
        /// based on the sensor driver’s implementation. If your app does not 
        /// want to use this default value, you should set the report interval 
        /// to a non-zero value prior to registering any event handlers.
        /// </remarks>
        public uint ReportInterval
        {
            get
            {
                return ScheduleOptions.ReportInterval;
            }
            set
            {
                // Changing?
                if (value != ScheduleOptions.ReportInterval)
                {
                    // New value or default?
                    if (value == 0)
                    {
                        UpdateReportInterval(defaultScheduleOptions.ReportInterval);
                    }
                    else
                    {
                        UpdateReportInterval(value);
                    }
                }
            }
        }
        #endregion // Public Properties
    }

    /// <summary>
    /// A base class for a device that is synchronously updated by a scheduler.
    /// </summary>
    public abstract class ScheduledDevice : ScheduledDeviceBase
    {
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="ScheduledDevice"/>.
        /// </summary>
        /// <param name="scheduler">
        /// The scheduler that will be used to provide updates.
        /// </param>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public ScheduledDevice(IScheduler scheduler, ScheduleOptions scheduleOptions) : base(scheduler, scheduleOptions)
        {
            SetUpdateAction((Action)Update);
        }

        /// <summary>
        /// Initializes a new <see cref="ScheduledDevice"/> using the default scheduler.
        /// </summary>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public ScheduledDevice(ScheduleOptions scheduleOptions) : this(IoTScheduler.Default, scheduleOptions) { }
        #endregion // Constructors


        #region Internal Methods
        /// <summary>
        /// Called by the scheduler to update the device.
        /// </summary>
        protected abstract void Update();
        #endregion // Internal Methods
    }

    /*
    /// <summary>
    /// A base class for a device that is asynchronously updated by a scheduler.
    /// </summary>
    public abstract class AsyncScheduledDevice : ScheduledDeviceBase
    {
        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="AsyncScheduledDevice"/>.
        /// </summary>
        /// <param name="scheduler">
        /// The scheduler that will be used to provide updates.
        /// </param>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public AsyncScheduledDevice(IScheduler scheduler, ScheduleOptions scheduleOptions) : base(scheduler, scheduleOptions)
        {
            SetUpdateAction((IAsyncAction)UpdateAsync.AsAsyncAction());
        }

        /// <summary>
        /// Initializes a new <see cref="AsyncScheduledDevice"/> using the default scheduler.
        /// </summary>
        /// <param name="scheduleOptions">
        /// The options used for scheduling.
        /// </param>
        public AsyncScheduledDevice(ScheduleOptions scheduleOptions) : this(IoTScheduler.Default, scheduleOptions) { }
        #endregion // Constructors


        #region Internal Methods
        /// <summary>
        /// Called by the scheduler to update the device.
        /// </summary>
        protected abstract Task UpdateAsync(CancellationToken cancellationToken);
        #endregion // Internal Methods
    }
        */
}