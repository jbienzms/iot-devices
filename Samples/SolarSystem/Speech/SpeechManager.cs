using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using SolarSystem.Model;
using SolarSystem.Speech;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Xml;

namespace SolarSystem.Speech
{
    /// <summary>
    /// Manages speech recognition services for the application.
    /// </summary>
    /// <remarks>
    /// Awesome sample code for dynamic SRGS from <see href="http://www.robwirving.com/2014/03/07/wp8-speech-recognition-dynamically-generating-srgs-grammar-files/">Rob Irving</see>.
    /// </remarks>
    public class SpeechManager
    {
        #region Constants
        private const string GRAMMAR_FILE = "Speech\\CelestialGrammar.xml";
        private const string TAG_BODYNAME = "bodyName";
        #endregion // Constants

        #region Member Variables
        private bool isInitialized;
        private bool isSuspended;
        private SpeechRecognizer recognizer;
        private CelestialSystem system;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Constructs a new <see cref="SpeechManager"/>.
        /// </summary>
        public SpeechManager()
        {
        }
        #endregion // Constructors

        private void AddDynamicRules(XNamespace xmlns, XElement rootElement)
        {
            var bodyRule = new XElement(xmlns + "rule", new XAttribute("id", TAG_BODYNAME));
            var bodyCollection = new XElement(xmlns + "one-of");
            foreach (var body in system.Bodies)
            {
                bodyCollection.Add(new XElement(xmlns + "item", new XText(body.Name),
                    new XElement(xmlns + "tag", new XText(string.Format("out.{0}=\"{1}\";", TAG_BODYNAME, body.Name)))));
            }

            // It's important to have at least one element, without this you'll get an exception when you try to use the grammar file
            if (bodyCollection.HasElements)
            {
                bodyRule.Add(bodyCollection);
            }

            // Add to root rule
            rootElement.Add(bodyRule);
        }

        private async Task<ISpeechRecognitionConstraint> LoadDynamicConstraintAsync()
        {
            // Get template file
            var templateFile = await Package.Current.InstalledLocation.GetFileAsync(GRAMMAR_FILE);

            // Create dynamic file
            var dynamicFile = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("DynamicGrammar.xml", CreationCollisionOption.ReplaceExisting);

            // Copy from template to dynamic and add new rules
            using (var templateStream = await templateFile.OpenReadAsync())
            {
                // Import grammar namespace
                XNamespace xmlns = "http://www.w3.org/2001/06/grammar";

                // Load template
                XDocument dynamicDoc = XDocument.Load(templateStream.AsStreamForRead());

                // Add dynamic rules
                AddDynamicRules(xmlns, dynamicDoc.Root);

                // Write out to temp file
                using (var dynamicStream = await dynamicFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    // Customize settings to be SRGS friendly
                    XmlWriterSettings srgsSettings = new XmlWriterSettings
                    {
                        Indent = true,
                        NewLineHandling = NewLineHandling.Entitize,
                        NewLineOnAttributes = false
                    };

                    // Create writer for dynamic file with proper settings
                    using (var dynamicWriter = XmlWriter.Create(dynamicStream.AsStreamForWrite(), srgsSettings))
                    {
                        // Save dynamic to file
                        dynamicDoc.WriteTo(dynamicWriter);
                    }
                }
            }

            // Load constraint from dynamic file
            var constraint = new SpeechRecognitionGrammarFileConstraint(dynamicFile);

            // Return the loaded constraint
            return constraint;
        }

        #region Overrides / Event Handlers
        private void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            Debug.WriteLine("Continuous Recognition Session Completed: " + args.Status.ToString());
        }

        private void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            // Output debug strings
            Debug.WriteLine(args.Result.Status);
            Debug.WriteLine(args.Result.Text);

            int count = args.Result.SemanticInterpretation.Properties.Count;

            Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);

            // Read tags
            var bodyName = args.Result.GetTag(TAG_BODYNAME);

            Debug.WriteLine("Body: " + bodyName);

            // Try to find the body
            var body = system.Bodies.Where(b => b.Name == bodyName).FirstOrDefault();

            // Notify
            if (ResultRecognized != null)
            {
                if ((body != null))
                {
                    var bodies = new List<CelestialBody>() { body };
                    ResultRecognized(this, new CelestialSpeechResult(new List<CelestialBody>(bodies)));
                }
            }
        }

        // Recognizer state changed
        private void RecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State.ToString());
        }
        #endregion // Overrides / Event Handlers

        #region Public Methods
        /// <summary>
        /// Initializes speech recognition and begins listening.
        /// </summary>
        /// <param name="system">
        /// The <see cref="CelestialSystem"/> used to build voice commands and return results.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that yields the result of the operation.
        /// </returns>
        public async Task<SpeechRecognitionResultStatus> InitializeAsync(CelestialSystem system)
        {
            // Validate
            if (isInitialized) { throw new InvalidOperationException("Already initialized."); }
            if (system == null) throw new ArgumentNullException("system");

            // Store
            this.system = system;

            // Create recognizer
            recognizer = new SpeechRecognizer();

            // Configure to never stop listening
            recognizer.ContinuousRecognitionSession.AutoStopSilenceTimeout = TimeSpan.MaxValue;

            // Subscribe to events
            recognizer.StateChanged += RecognizerStateChanged;
            recognizer.ContinuousRecognitionSession.Completed += ContinuousRecognitionSession_Completed;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            // Load constraint
            var constraint = await LoadDynamicConstraintAsync();

            // Add constraint to recognizer
            recognizer.Constraints.Add(constraint);

            // Compile
            var compileResult = await recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Grammar Compiled: " + compileResult.Status.ToString());

            // We're initialized now
            isInitialized = true;

            // If successful start recognition
            if (compileResult.Status == SpeechRecognitionResultStatus.Success)
            {
                await recognizer.ContinuousRecognitionSession.StartAsync(SpeechContinuousRecognitionMode.Default);
            }

            // Return the result
            return compileResult.Status;
        }

        /// <summary>
        /// Stops speech recognition.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        public async Task ResumeRecognitionAsync()
        {
            if (isInitialized) { throw new InvalidOperationException("Speech not initialized."); }

            if ((recognizer != null) && (isSuspended))
            {
                await recognizer.ContinuousRecognitionSession.StartAsync();
                isSuspended = false;
            }
        }

        /// <summary>
        /// Stops speech recognition.
        /// </summary>
        /// <returns>
        /// A <see cref="Task"/> that represents the operation.
        /// </returns>
        public async Task SuspendRecognitionAsync()
        {
            if (isInitialized) { throw new InvalidOperationException("Speech not initialized."); }

            if ((recognizer != null) && (!isSuspended))
            {
                await recognizer.ContinuousRecognitionSession.StopAsync();
                isSuspended = true;
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets a value that indicates if the speech manager has been initialized.
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Gets a value that indicates if the speech manager has been suspended.
        /// </summary>
        public bool IsSuspended => isSuspended;
        #endregion // Public Properties

        #region Public Events
        /// <summary>
        /// Occurs when a speech result is recognized.
        /// </summary>
        public event TypedEventHandler<SpeechManager, CelestialSpeechResult> ResultRecognized;
        #endregion // Public Events
    }
}
