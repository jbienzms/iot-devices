// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices
{
    // TODO: Export
    /// <summary>
    /// Thrown when an IO setting has been changed at an unsupported time.
    /// </summary>
    internal class IoChangeException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="IoChangeException"/> with the default message.
        /// </summary>
        public IoChangeException() : base(Strings.IOChangeState) { }

        /// <summary>
        /// Initializes a new <see cref="IoChangeException"/> with a custom message.
        /// </summary>
        public IoChangeException(string message) : base(message) { }
    }
}
