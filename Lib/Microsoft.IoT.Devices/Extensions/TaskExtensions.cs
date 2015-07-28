// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices
{
    static internal class TaskExtensions
    {
        #region Member Variables
        static private Task completedTask;
        #endregion // Member Variables

        /// <summary>
        /// Schedules a continuation that ignores any exceptions during execution.
        /// </summary>
        /// <param name="task">
        /// The original task.
        /// </param>
        /// <returns>
        /// The continuation task.
        /// </returns>
        static public Task IgnoreExceptions(this Task task)
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
        static public Task FailFastOnException(this Task task)
        {
            // Validate
            if (task == null) throw new ArgumentNullException("task");

            // Continue
            task.ContinueWith(c => Environment.FailFast("Task failed", c.Exception),
                TaskContinuationOptions.OnlyOnFaulted |
                TaskContinuationOptions.ExecuteSynchronously);
            return task;
        }

        /// <summary>
        /// Gets a <see cref="Task"/> that has already completed.
        /// </summary>
        static public Task CompletedTask
        {
            get
            {
                if (completedTask == null)
                {
                    completedTask = Task.FromResult<bool>(true);
                }
                return completedTask;
            }
        }
    }
}
