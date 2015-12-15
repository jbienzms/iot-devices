using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SolarSystem.Model;
using SolarSystem.Speech;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Media.SpeechRecognition;

namespace SolarSystem.Speech
{
    /// <summary>
    /// Manages speech recognition services for the application.
    /// </summary>
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
        /// <param name="system">
        /// The <see cref="CelestialSystem"/> used to build voice commands and return results.
        /// </param>
        public SpeechManager(CelestialSystem system)
        {
            if (system == null) throw new ArgumentNullException("system");
            this.system = system;
        }
        #endregion // Constructors

        #region Overrides / Event Handlers
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
            var body = system.Bodies.Where(b => b.BodyName.ToLower() == bodyName).FirstOrDefault();

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
        /// <returns>
        /// A <see cref="Task"/> that yields the result of the operation.
        /// </returns>
        public async Task<SpeechRecognitionResultStatus> InitializeAsync()
        {
            // Create recognizer
            recognizer = new SpeechRecognizer();

            // Subscribe to events
            recognizer.StateChanged += RecognizerStateChanged;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            // Get file
            var file = await Package.Current.InstalledLocation.GetFileAsync(GRAMMAR_FILE);

            // Load constraint
            var constraint = new SpeechRecognitionGrammarFileConstraint(file);

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
                await recognizer.ContinuousRecognitionSession.StartAsync();
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
