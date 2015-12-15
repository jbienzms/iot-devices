using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolarSystem.Model;

namespace SolarSystem.Speech
{
    /// <summary>
    /// Represents a result of a customized speech recognition.
    /// </summary>
    public class CelestialSpeechResult
    {
        #region Member Variables
        private List<CelestialBody> bodies;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="CelestialSpeechResult"/>.
        /// </summary>
        /// <param name="bodies">
        /// The celestial bodies recognized.
        /// </param>
        public CelestialSpeechResult(List<CelestialBody> bodies)
        {
            this.bodies = bodies;
        }
        #endregion // Constructors

        #region Public Properties
        /// <summary>
        /// Gets the bodies that were recognized.
        /// </summary>
        public List<CelestialBody> Bodies => bodies;
        #endregion // Public Properties
    }
}
