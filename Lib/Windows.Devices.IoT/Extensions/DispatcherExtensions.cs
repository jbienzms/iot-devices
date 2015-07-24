// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.Devices.IoT
{
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

        static public void RunIdle(this CoreDispatcher dispatcher, Action action)
        {
            // Run
            var t = RunIdleAsync(dispatcher, action);
        }

        static public IAsyncAction RunIdleAsync(this CoreDispatcher dispatcher, Action action)
        {
            // Validate
            if (dispatcher == null) throw new ArgumentNullException("dispatcher");

            // Run
            return dispatcher.RunIdleAsync((e) => { action(); });
        }
    }
}
