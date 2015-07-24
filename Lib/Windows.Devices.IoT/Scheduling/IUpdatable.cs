// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Devices.IoT
{
    /// <summary>
    /// The interface for an entity that can be updated.
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// Causes the entity to update.
        /// </summary>
        void Update();
    }
}
