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
        static public void Run(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Run
            var t = RunAsync(dispatcher, handler);
        }

        static public IAsyncAction RunAsync(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Validate
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            // Run
            return dispatcher.RunAsync(CoreDispatcherPriority.Normal, handler);
        }

        static public void RunIdle(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Run
            var t = RunIdleAsync(dispatcher, handler);
        }

        static public IAsyncAction RunIdleAsync(this CoreDispatcher dispatcher, DispatchedHandler handler)
        {
            // Validate
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            // Run
            return dispatcher.RunIdleAsync((e) => { handler(); });
        }
    }
}
