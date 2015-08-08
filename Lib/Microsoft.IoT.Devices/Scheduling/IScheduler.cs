// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.IoT.Devices
{
    /// <summary>
    /// A delegate for scheduled action. 
    /// </summary>
    public delegate void ScheduledAction();

    /// <summary>
    /// A delegate for scheduled asynchronous action. 
    /// </summary>
    public delegate IAsyncAction ScheduledAsyncAction();

    /// <summary>
    /// The interface for a class that can schedule updates for other entities.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Resumes execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The asynchronous subscriber to resume.
        /// </param>
        void Resume(ScheduledAsyncAction subscriber);

        /// <summary>
        /// Resumes execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to resume.
        /// </param>
        [DefaultOverload]
        void Resume(ScheduledAction subscriber);

        /// <summary>
        /// Schedules execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to schedule.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides options for the schedule.
        /// </param>
        void Schedule(ScheduledAsyncAction subscriber, ScheduleOptions options);

        /// <summary>
        /// Schedules execution of a syncrhonous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to schedule.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides options for the schedule.
        /// </param>
        [DefaultOverload]
        void Schedule(ScheduledAction subscriber, ScheduleOptions options);

        /// <summary>
        /// Suspends execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to suspend.
        /// </param>
        void Suspend(ScheduledAsyncAction subscriber);

        /// <summary>
        /// Suspends execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to suspend.
        /// </param>
        [DefaultOverload]
        void Suspend(ScheduledAction subscriber);

        /// <summary>
        /// Unschedules execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to unschedule.
        /// </param>
        void Unschedule(ScheduledAsyncAction subscriber);

        /// <summary>
        /// Unschedules execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to unschedule.
        /// </param>
        [DefaultOverload]
        void Unschedule(ScheduledAction subscriber);

        /// <summary>
        /// Updates the schedule for an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to update.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides the updated options.
        /// </param>
        void UpdateSchedule(ScheduledAsyncAction subscriber, ScheduleOptions options);

        /// <summary>
        /// Updates the schedule for a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to update.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides the updated options.
        /// </param>
        [DefaultOverload]
        void UpdateSchedule(ScheduledAction subscriber, ScheduleOptions options);
    }
}
