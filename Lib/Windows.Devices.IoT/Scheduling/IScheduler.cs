// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// A delegate for a cancellable asynchronous method. 
    /// </summary>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the operation.
    /// </returns>
    public delegate Task AsyncAction(CancellationToken token);

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
        void Resume(AsyncAction subscriber);

        /// <summary>
        /// Resumes execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to resume.
        /// </param>
        void Resume(Action subscriber);

        /// <summary>
        /// Schedules execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to schedule.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides options for the schedule.
        /// </param>
        void Schedule(AsyncAction subscriber, ScheduleOptions options);

        /// <summary>
        /// Schedules execution of a syncrhonous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to schedule.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides options for the schedule.
        /// </param>
        void Schedule(Action subscriber, ScheduleOptions options);

        /// <summary>
        /// Suspends execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to suspend.
        /// </param>
        void Suspend(AsyncAction subscriber);

        /// <summary>
        /// Suspends execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to suspend.
        /// </param>
        void Suspend(Action subscriber);

        /// <summary>
        /// Unschedules execution of an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to unschedule.
        /// </param>
        void Unschedule(AsyncAction subscriber);

        /// <summary>
        /// Unschedules execution of a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to unschedule.
        /// </param>
        void Unschedule(Action subscriber);

        /// <summary>
        /// Updates the schedule for an asynchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to update.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides the updated options.
        /// </param>
        void UpdateSchedule(AsyncAction subscriber, ScheduleOptions options);

        /// <summary>
        /// Updates the schedule for a synchronous subscriber.
        /// </summary>
        /// <param name="subscriber">
        /// The subscriber to update.
        /// </param>
        /// <param name="options">
        /// A <see cref="ScheduleOptions"/> that provides the updated options.
        /// </param>
        void UpdateSchedule(Action subscriber, ScheduleOptions options);
    }
}
