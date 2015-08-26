// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if DEVICE_CORE
namespace Microsoft.IoT.DeviceCore
{
    static internal class TaskExtensions
#else
namespace Microsoft.IoT.DeviceHelpers
{
    static public class TaskExtensions
#endif
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
        /// Blocks and waits for a task to complete in a way that will not deadlock the UI thread.
        /// </summary>
        /// <param name="taskFunction">
        /// A function that returns the task to wait on.
        /// </param>
        static public void UISafeWait(Func<Task> taskFunction)
        {
            Task.Run(async () =>
            {
                await taskFunction().ConfigureAwait(continueOnCapturedContext: false);
            }).Wait();
        }

        /// <summary>
        /// Blocks and waits for a task to complete in a way that will not deadlock the UI thread.
        /// </summary>
        /// <typeparam name="TParam">
        /// The type of parameter passed to the task.
        /// </typeparam>
        /// <param name="taskFunction">
        /// A function that returns the task to wait on.
        /// </param>
        /// <param name="param">
        /// The parameter to pass to the function.
        /// </param>
        static public void UISafeWait<TParam>(Func<TParam, Task> taskFunction, TParam param)
        {
            Task.Run(async () =>
            {
                await taskFunction(param).ConfigureAwait(continueOnCapturedContext: false);
            }).Wait();
        }

        /// <summary>
        /// Blocks and waits for a task to complete in a way that will not deadlock the UI thread.
        /// </summary>
        /// <typeparam name="T">
        /// The type of value returned by the task.
        /// </typeparam>
        /// <param name="taskFunction">
        /// A function that returns the task to wait on.
        /// </param>
        static public T UISafeWait<T>(Func<Task<T>> taskFunction)
        {
            return Task.Run<T>(async () =>
            {
                return await taskFunction().ConfigureAwait(continueOnCapturedContext: false);
            }).Result;
        }
    }
}
