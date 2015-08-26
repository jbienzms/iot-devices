// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.DeviceCore.Input
{
    public interface IThumbstickReadingChangedEventArgs
    {
        /// <summary>
        /// Gets the current reading of the thumbstick.
        /// </summary>
        ThumbstickReading Reading { get; }
    }

    public sealed class ThumbstickReadingChangedEventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="ThumbstickReadingChangedEventArgs"/>
        /// </summary>
        /// <param name="reading"></param>
        public ThumbstickReadingChangedEventArgs(ThumbstickReading reading)
        {
            if (reading == null) throw new ArgumentNullException("reading");
            this.Reading = reading;
        }

        /// <summary>
        /// Gets the current reading of the thumbstick.
        /// </summary>
        public ThumbstickReading Reading { get; }
    }
}
