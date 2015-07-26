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
    /// Thrown when a required IO setting has not been supplied.
    /// </summary>
    internal class MissingIoException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="IoChangeException"/> with a custom message.
        /// </summary>
        public MissingIoException(string message) : base(string.Format(Strings.MissingRequiredIO, message)) { }
    }
}
