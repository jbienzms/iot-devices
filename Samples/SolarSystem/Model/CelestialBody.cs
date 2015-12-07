using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SolarSystem.Model
{
    /// <summary>
    /// Represents a celestial body (planet, star, etc.)
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CelestialBody
    {
        /// <summary>
        /// Gets or sets the duration of a single rotation around the bodies own axis.
        /// </summary>
        /// <value>
        /// The duration of a single rotation around the bodies own axis.
        /// </value>
        [JsonProperty(Order = 2)]

        public TimeSpan Day { get; set; }

        /// <summary>
        /// Gets or sets a description of the body.
        /// </summary>
        /// <value>
        /// A description of the body.
        /// </value>
        [JsonProperty(Order = 4)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a name for the body.
        /// </summary>
        /// <value>
        /// A name for the body.
        /// </value>
        [JsonProperty(Order = 0)]
        public string BodyName { get; set; }

        /// <summary>
        /// Gets or sets the average elliptical distance from the body it orbits, in millions of kilometers.
        /// </summary>
        /// <value>
        /// The average elliptical distance from the body it orbits, in millions of kilometers.
        /// </value>
        [JsonProperty(Order = 1)]
        public double Orbit { get; set; }

        /// <summary>
        /// Gets or sets the duration of complete orbit.
        /// </summary>
        /// <value>
        /// The duration of complete orbit.
        /// </value>
        [JsonProperty(Order = 3)]
        public TimeSpan Year { get; set; }
    }
}
