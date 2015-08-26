// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.DeviceCore
{
    public interface IEventObserver
    {
        void Added(object sender);
        void FirstAdded(object sender);
        void LastRemoved(object sender);
        void Removed(object sender);
    }
}
