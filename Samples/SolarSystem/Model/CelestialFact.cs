using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SolarSystem.Model
{
    /// <summary>
    /// Represents a fact about one or more celestial bodies.
    /// </summary>
    public class CelestialFact
    {
        /// <summary>
        /// Gets or sets a collection of bodies that match the fact.
        /// </summary>
        /// <value>
        /// The bodies that match the fact.
        /// </value>
        [JsonProperty(Order = 2)]
        public List<CelestialBody> Bodies { get; set; }

        /// <summary>
        /// Gets or sets a description of the fact.
        /// </summary>
        /// <value>
        /// A description of the fact.
        /// </value>
        [JsonProperty(Order = 1)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a name for the fact.
        /// </summary>
        /// <value>
        /// A name for the fact.
        /// </value>
        [JsonProperty(Order = 0)]
        public string Name { get; set; }
    }
}
