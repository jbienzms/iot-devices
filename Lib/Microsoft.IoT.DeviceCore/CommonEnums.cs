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
    /// Defines the direction that rotation can occur on a single axis.
    /// </summary>
    public enum RotationDirection
    {
        /// <summary>
        /// Specifies that rotation is in a clockwise (positive-angle) direction.
        /// </summary>
        Clockwise,

        /// <summary>
        /// Specifies that rotation is in a counter clockwise (negative-angle) direction.
        /// </summary>
        Counterclockwise
    }
}
