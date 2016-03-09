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
    /// A class for working with temperature conversions.
    /// </summary>
    /// <remarks>
    /// This class borrows <i>heavily</i> from the 
    /// <see href="https://raw.githubusercontent.com/anjdreas/UnitsNet/master/UnitsNet/GeneratedCode/UnitClasses/Temperature.g.cs">Temperature class in UnitsNet</see>. 
    /// In fact if UnitsNet is updated to be Windows Runtime compatible we will likely take a dependency on that library. 
    /// All credit for the code in this class should go to Initial Force AS and the 
    /// <see href="https://github.com/anjdreas/UnitsNet">UnitsNet</see> project.
    /// </remarks>
    public sealed class Temperature
    {
        #region Static Version
        #region Member Variables
        static private Temperature zero;
        #endregion // Member Variables

        #region Public Methods
        /// <summary>
        ///     Get Temperature from DegreesCelsius.
        /// </summary>
        public static Temperature FromDegreesCelsius(double degreescelsius)
        {
            return new Temperature(degreescelsius + 273.15);
        }

        /// <summary>
        ///     Get Temperature from DegreesFahrenheit.
        /// </summary>
        public static Temperature FromDegreesFahrenheit(double degreesfahrenheit)
        {
            return new Temperature(degreesfahrenheit * 5 / 9 + 459.67 * 5 / 9);
        }

        /// <summary>
        ///     Get Temperature from Kelvins.
        /// </summary>
        public static Temperature FromKelvins(double kelvins)
        {
            return new Temperature(kelvins);
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets the singleton for zero degrees Kelvin.
        /// </summary>
        public static Temperature Zero
        {
            get
            {
                if (zero == null)
                {
                    zero = new Temperature(0);
                }
                return zero;
            }
        }
        #endregion // Public Properties
        #endregion  // Static Version


        #region Instance Version
        #region Member Variables
        private readonly double kelvins;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="Temperature"/> instance.
        /// </summary>
        /// <param name="kelvins">
        /// The temperature in kelvins
        /// </param>
        public Temperature(double kelvins)
        {
            this.kelvins = kelvins;
        }
        #endregion // Constructors

        #region Public Properties
        /// <summary>
        ///     Get Temperature in DegreesCelsius.
        /// </summary>
        public double DegreesCelsius
        {
            get { return kelvins - 273.15; }
        }

        /// <summary>
        ///     Get Temperature in DegreesFahrenheit.
        /// </summary>
        public double DegreesFahrenheit
        {
            get { return (kelvins - 459.67 * 5 / 9) * 9 / 5; }
        }

        /// <summary>
        ///     Get Temperature in Kelvins.
        /// </summary>
        public double Kelvins
        {
            get { return kelvins; }
        }

        #endregion // Public Properties
        #endregion // Instance Version
    }
}
