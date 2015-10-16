// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceCore
{

    /// <summary>
    /// Represents an argument for events that do not provide custom data.
    /// </summary>
    /// <remarks>
    /// This type is typically used with <see cref="Windows.Foundation.TypedEventHandler{TSender, TResult}">TypedEventHandler</see>
    /// </remarks>
    public sealed class EmptyEventArgs
    {
        static private EmptyEventArgs instance;

        /// <summary>
        /// Returns the singleton instance of <see cref="EmptyEventArgs"/>.
        /// </summary>
        static public EmptyEventArgs Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new EmptyEventArgs();
                }
                return instance;
            }
        }
    }
}
