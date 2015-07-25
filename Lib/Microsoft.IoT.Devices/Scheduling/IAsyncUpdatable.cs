// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// The interface for a class that can be updated asynchronously.
    /// </summary>
    public interface IAsyncUpdatable
    {
        /// <summary>
        /// Causes the entity to update.
        /// </summary>
        /// <param name="token">
        /// The <see cref="CancellationToken"/> that can be used to stop the update before it completes.
        /// </param>
        Task UpdateAsync(CancellationToken token);
    }
}
