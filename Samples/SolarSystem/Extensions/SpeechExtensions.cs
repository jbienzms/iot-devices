using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace SolarSystem
{
    /// <summary>
    /// Extension methods for helping with speech.
    /// </summary>
    static public class SpeechExtensions
    {
        /// <summary>
        /// Gets the value for the specified tag in the speech recognition result.
        /// </summary>
        /// <param name="result">
        /// The result used to obtain the tag.
        /// </param>
        /// <param name="tag">
        /// The tag to get.
        /// </param>
        /// <param name="defaultValue">
        /// A default value to use if the tag is not found.
        /// </param>
        /// <returns>
        /// The tag value or default value.
        /// </returns>
        static public string GetTag(this SpeechRecognitionResult result, string tag, string defaultValue)
        {
            // Shortcut
            var props = result.SemanticInterpretation.Properties;

            // Property exist?
            return props.ContainsKey(tag) ? props[tag][0].ToString() : defaultValue;
        }

        /// <summary>
        /// Gets the value for the specified tag in the speech recognition result.
        /// </summary>
        /// <param name="result">
        /// The result used to obtain the tag.
        /// </param>
        /// <param name="tag">
        /// The tag to get.
        /// </param>
        /// <returns>
        /// The tag value if found; otherwise <see langword="null"/>.
        /// </returns>
        static public string GetTag(this SpeechRecognitionResult result, string tag)
        {
            return GetTag(result, tag, null);
        }
    }
}
