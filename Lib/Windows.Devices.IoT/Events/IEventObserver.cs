// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Devices.IoT
{
    public interface IEventObserver
    {
        void FirstHandlerAdded(object sender);
        void HandlerAdded(object sender);
        void HandlerRemoved(object sender);
        void LastHandlerRemoved(object sender);
    }
}
