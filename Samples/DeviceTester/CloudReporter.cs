using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.Connections.Azure;
using Microsoft.IoT.Connections.Azure.EventHubs;

namespace DeviceTester
{
    /// <summary>
    /// Reports sensor readings to the cloud.
    /// </summary>
    static public class CloudReporter
    {
        #region Member Variables
        static private ConnectionParameters connectionParams;
        static private HttpSender sender;
        #endregion // Member Variables


        #region Internal Methods
        static private void EnsureSender()
        {
            if (sender == null)
            {
                // Create connection params
                var connectionParams = new ConnectionParameters(
                "jbienzms", // Service Bus Name
                "sensornet", // Event Hub Name
                "sender", // Policy Name
                "oRvc8a3dul1cI7DLz4SfTan2KRnO10/ZxvqxqCj6LHY=", // Policy Key
                "DeviceTester", // Publisher Name
                TimeSpan.FromSeconds(20) // Auth Token Time To Live
                );

                // Create sender
                sender = new HttpSender(connectionParams);
            }
        }
        #endregion // Internal Methods


        #region Public Methods
        /// <summary>
        /// Sends an analog reading to the cloud.
        /// </summary>
        /// <param name="ratio">
        /// The ratio of the analog sensor.
        /// </param>
        /// <returns>
        /// A <see cref="SendResult"/> that indicates the result of the operation.
        /// </returns>
        static public SendResult ReportAnalog(double ratio)
        {
            // Ensure the sender has been created
            EnsureSender();

            // Create the measurement
            var measurement = new SensorMeasurement()
            {
                DisplayName = "Office",
                Guid = Guid.NewGuid(),
                Location = "Houston",
                MeasureName = "Analog",
                UnitOfMeasure = "Ratio",
                Value = ratio
            };

            // Send the measurement
            return sender.Send(measurement);
        }
        #endregion // Public Methods
    }
}
