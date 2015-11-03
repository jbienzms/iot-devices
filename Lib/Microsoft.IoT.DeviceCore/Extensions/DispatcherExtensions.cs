// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Microsoft.IoT.DeviceCore
{
    /// <summary>
    /// Provides extension methods that make it easier to schedule actions on a dispatcher.
    /// </summary>
    /// <remarks>
    /// </remarks>
    static public class DispatcherExtensions
    {
        /// <summary>
        /// Runs the handler in parallel at normal priority without capturing the task.
        /// </summary>
        /// <param name="dispatcher">
        /// The <see cref="CoreDispatcher"/> that will run the handler.
        /// </param>
        /// <param name="handler">
        /// The handler to run.
        /// </param>
        static public void Run(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Run
            var t = RunAsync(dispatcher, handler);
        }

        /// <summary>
        /// Runs the handler at normal priority.
        /// </summary>
        /// <param name="dispatcher">
        /// The <see cref="CoreDispatcher"/> that will run the handler.
        /// </param>
        /// <param name="handler">
        /// The handler to run.
        /// </param>
        /// <returns>
        /// The <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        static public IAsyncAction RunAsync(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Validate
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            // Run
            return dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);
        }

        /// <summary>
        /// Runs the handler in parallel at idle priority without capturing the task.
        /// </summary>
        /// <param name="dispatcher">
        /// The <see cref="CoreDispatcher"/> that will run the handler.
        /// </param>
        /// <param name="handler">
        /// The handler to run.
        /// </param>
        static public void RunIdle(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Run
            var t = RunIdleAsync(dispatcher, handler);
        }

        /// <summary>
        /// Runs the handler at idle priority.
        /// </summary>
        /// <param name="dispatcher">
        /// The <see cref="CoreDispatcher"/> that will run the handler.
        /// </param>
        /// <param name="handler">
        /// The handler to run.
        /// </param>
        /// <returns>
        /// The <see cref="IAsyncAction"/> that represents the operation.
        /// </returns>
        static public IAsyncAction RunIdleAsync(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Validate
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            // Run
            return dispatcher.RunIdleAsync((e) => { handler(); });
        }
    }
}
