// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Devices.IoT
{
    static internal class TaskExtensions
    {
        /// <summary>
        /// Schedules a continuation that ignores any exceptions during execution.
        /// </summary>
        /// <param name="task">
        /// The original task.
        /// </param>
        /// <returns>
        /// The continuation task.
        /// </returns>
        public static Task IgnoreExceptions(this Task task)
        {
            // Validate
            if (task == null) throw new ArgumentNullException("task");

            // Continue
            task.ContinueWith(c => { var ignored = c.Exception; },
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        /// <summary>
        /// Schedules a continuation that ignores any exceptions during execution.
        /// </summary>
        /// <param name="task">
        /// The original task.
        /// </param>
        /// <returns>
        /// The continuation task.
        /// </returns>
        public static Task FailFastOnException(this Task task)
        {
            // Validate
            if (task == null) throw new ArgumentNullException("task");

            // Continue
            task.ContinueWith(c => Environment.FailFast("Task failed", c.Exception),
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }
    }
}
