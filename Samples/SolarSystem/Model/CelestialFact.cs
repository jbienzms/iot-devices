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
        [JsonProperty(Order = 3)]
        public List<CelestialBody> Bodies { get; set; }

        /// <summary>
        /// Gets or sets the contributor of the fact.
        /// </summary>
        /// <value>
        /// The contributor of the fact.
        /// </value>
        [JsonProperty(Order = 1)]
        public string Contributor { get; set; }

        /// <summary>
        /// Gets or sets a description of the fact.
        /// </summary>
        /// <value>
        /// A description of the fact.
        /// </value>
        /// <remarks>
        /// The description provides additional detail text displayed on the screen.
        /// </remarks>
        [JsonProperty(Order = 2)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a title for the fact.
        /// </summary>
        /// <value>
        /// A title for the fact.
        /// </value>
        /// <remarks>
        /// The title is what should be spoken during speech recognition for the 
        /// fact to be displayed.
        /// </remarks>
        [JsonProperty(Order = 0)]
        public string Title { get; set; }
    }
}
