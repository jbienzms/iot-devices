using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SolarSystem.Model
{
    /// <summary>
    /// Represents a celestial system (e.g. our solar system).
    /// </summary>
    public class CelestialSystem
    {
        /// <summary>
        /// Gets or sets a collection of bodies in the system.
        /// </summary>
        /// <value>
        /// The bodies in the system.
        /// </value>
        [JsonProperty(Order = 0)]
        public List<CelestialBody> Bodies { get; set; }

        /// <summary>
        /// Gets or sets a collection of facts about the system.
        /// </summary>
        /// <value>
        /// Facts about the system.
        /// </value>
        [JsonProperty(Order = 1)]
        public List<CelestialFact> Facts { get; set; }
    }
}
