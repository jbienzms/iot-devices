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
    /// Thrown when an element is missing from a template (usually a xaml template).
    /// </summary>
    internal class MissingTemplateElementException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="MissingTemplateElementException"/> with the default message.
        /// </summary>
        /// <param name="elementName">
        /// The name of the element that is missing.
        /// </param>
        /// <param name="templateName">
        /// The name of the template the element is missing from.
        /// </param>
        public MissingTemplateElementException(string elementName, string templateName) : base(string.Format(Strings.MissingTemplateElement, elementName, templateName)) { }
    }
}
