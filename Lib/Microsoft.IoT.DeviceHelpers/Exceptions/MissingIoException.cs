// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceHelpers
{
    /// <summary>
    /// Thrown when a required IO setting has not been supplied.
    /// </summary>
    public class MissingIoException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="IoChangeException"/> with a property name.
        /// </summary>
        public MissingIoException(string property) : base(string.Format(Strings.MissingRequiredIO, property)) { }
    }
}
