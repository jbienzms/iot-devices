// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace Microsoft.IoT.DeviceCore.Lights
{
    /// <summary>
    /// The interface for an indicator light.
    /// </summary>
    public interface ILight
    {
        /// <summary>
        /// Gets or sets a value indicating the current brightness level 
        /// of the light, where 0.0 is completely off and 1.0 is maximum 
        /// brightness.
        /// </summary>
        /// <value>
        /// A value indicating the current brightness level of the lamp. The default is <c>1.0</c>.
        /// </value>
        [DefaultValue(1.0f)]
        float BrightnessLevel { get; set; }

        /// <summary>
        /// Gets or sets the color of the light.
        /// </summary>
        /// <value>
        /// The color of the light.
        /// </value>
        /// <remarks>
        /// The alpha channel of the color is ignored by this API.
        /// </remarks>
        Color Color { get; set; }

        /// <summary>
        /// Gets a value indicating whether you can set the 
        /// <see cref="Color"/> property of the light.
        /// </summary>
        /// <value>
        /// <c>true</c> if you can set the <see cref="Color"/> 
        /// property of the light; otherwise false.
        /// </value>
        bool IsColorSettable { get; }
    }
}
