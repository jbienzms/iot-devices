// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.IoT.Devices.Adc
{
    public interface IAdcChannel : IClosable
    {
            int ReadValue();
    }
}
