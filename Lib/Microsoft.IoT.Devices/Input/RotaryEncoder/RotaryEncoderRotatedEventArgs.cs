// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices.Input
{
    /// <summary>
    /// Provides data for the <see cref="RotaryEncoder.Rotated"/> event.
    /// </summary>
    public sealed class RotaryEncoderRotatedEventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="RotaryEncoderRotatedEventArgs"/> instance.
        /// </summary>
        /// <param name="direction">
        /// The direction of rotation.
        /// </param>
        public RotaryEncoderRotatedEventArgs(RotationDirection direction)
        {
            this.Direction = direction;
        }

        /// <summary>
        /// Gets a value that indicates direction of rotation.
        /// </summary>
        /// <value>
        /// A <see cref="RotationDirection"/> that indicates direction of rotation.
        /// </value>
        public RotationDirection Direction { get; }
    }
}
