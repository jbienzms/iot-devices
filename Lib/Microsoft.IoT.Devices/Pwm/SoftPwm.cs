using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IoT.DeviceCore;
using Windows.Devices.Gpio;
using Windows.Devices.Pwm.Provider;
using Microsoft.IoT.DeviceHelpers;

namespace Microsoft.IoT.Devices.Pwm
{
    /// <summary>
    /// A software-based <see cref="IPwmControllerProvider"/> that uses CPU timing to generate PWM 
    /// signals on regular GPIO pins.
    /// </summary>
    /// <remarks>
    /// The number of pins reported as available by <see cref="SoftPwm"/> is the same number of 
    /// pins reported as available by <see cref="GpioController.PinCount"/>. Therefore, developers 
    /// should be careful not to open <see cref="SoftPwm"/> pins that are already allocated for 
    /// other GPIO devices.
    /// </remarks>
    sealed public class SoftPwm : IPwmControllerProvider, IDisposable
    {
        #region Nested Types
        private class SoftPwmPin
        {
            #region Member Variables
            private GpioPin pin;
            internal double targetTicks;
            #endregion // Member Variables

            #region Constructors
            public SoftPwmPin(GpioPin pin)
            {
                this.pin = pin;
            }
            #endregion // Constructors

            #region Public Properties
            public double DutyCycle { get; set; }
            public bool Enabled { get; set; }
            public bool InvertPolarity { get; set; }
            public GpioPin Pin { get { return pin; } }
            #endregion // Public Properties
        }
        #endregion // Nested Types

        #region Constants
        private const double CLOCK_FREQUENCY = 25000000;
        private const int MAX_FREQUENCY = 1000;
        private const int MIN_FREQUENCY = 40;
        private const ushort PULSE_RESOLUTION = 4096;
        #endregion // Constants

        #region Member Variables
        private double actualFrequency;
        private GpioController gpioController;
        private int pinCount;
        private Dictionary<int, SoftPwmPin> pins;
        private Stopwatch stopwatch;
        private long ticksPerSecond;
        private ScheduledUpdater updater;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="SoftPwm"/> instance.
        /// </summary>
        public SoftPwm()
        {
            // Get GPIO
            gpioController = GpioController.GetDefault();

            // Make sure we have it
            if (gpioController == null) { throw new DeviceNotFoundException("GPIO"); }

            // How many pins
            pinCount = gpioController.PinCount;

            // Create pin lookup
            pins = new Dictionary<int, SoftPwmPin>(pinCount);

            // Create
            stopwatch = new Stopwatch();

            // Defaults
            actualFrequency = MIN_FREQUENCY;
            ticksPerSecond = Stopwatch.Frequency;

            // Create the updater. Default to 0 seconds between updates, meaning run as fast as possible.
            // IMPORTANT: Do not use Scheduler.Default, create a new Scheduler.
            // This puts us in parallel priority with other sensors and allows 
            // us to run on a separate core if available.
            updater = new ScheduledUpdater(scheduleOptions: new ScheduleOptions(0), scheduler: new Scheduler());
            updater.SetUpdateAction(Update);
        }
        #endregion // Constructors

        #region Internal Methods
        private void Update()
        {
            var enabledPins = pins.Values.Where(p => p.Enabled && p.DutyCycle != 0).ToList();

            // If there are no enabled pins, stop updates
            if (enabledPins.Count == 0)
            {
                updater.Stop();
                return;
            }

            for (int i = 0; i < enabledPins.Count; i++)
            {
                var softPin = enabledPins[i];
                var value = (softPin.InvertPolarity) ? GpioPinValue.Low : GpioPinValue.High;
                softPin.Pin.Write(value);
            }

            if (!stopwatch.IsRunning) { stopwatch.Start(); } else { stopwatch.Restart(); }

            long startTicks = stopwatch.ElapsedTicks;
            long currentTicks = 0;
            double period = 1000.0 / actualFrequency;

            // Calculate target ticks
            for (int i = 0; i < enabledPins.Count; i++)
            {
                var softPin = enabledPins[i];
                softPin.targetTicks = startTicks + softPin.DutyCycle * period * ticksPerSecond / 1000.0;
            }

            int processedPins = 0;
            while (processedPins < enabledPins.Count)
            {
                currentTicks = stopwatch.ElapsedTicks;

                for (int i = 0; i < enabledPins.Count; i++)
                {
                    var softPin = enabledPins[i];
                    if ((softPin.targetTicks > 0) && (currentTicks > softPin.targetTicks))
                    {
                        softPin.targetTicks = 0;
                        processedPins++;

                        var pinValue = (softPin.InvertPolarity) ? GpioPinValue.High : GpioPinValue.Low;
                        softPin.Pin.Write(pinValue);
                    }
                }
            }

            double endCycleTicks = startTicks + period * ticksPerSecond / 1000.0;
            currentTicks = stopwatch.ElapsedTicks;

            while (currentTicks < endCycleTicks)
            {
                // TODO: Better looping strategy
                currentTicks = stopwatch.ElapsedTicks;
            }
        }
        #endregion // Internal Methods

        #region Public Methods
        /// <inheritdoc/>
        public void AcquirePin(int pin)
        {
            if ((pin < 0) || (pin > (pinCount - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pins)
            {
                if (pins.ContainsKey(pin)) { throw new UnauthorizedAccessException(); }
                var gpioPin = gpioController.OpenPin(pin);
                gpioPin.SetDriveMode(GpioPinDriveMode.Output);
                pins[pin] = new SoftPwmPin(gpioPin);
            }
        }

        /// <inheritdoc/>
        public void DisablePin(int pin)
        {
            if ((pin < 0) || (pin > (pinCount - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pins)
            {
                if (!pins.ContainsKey(pin)) { throw new UnauthorizedAccessException(); }
                pins[pin].Enabled = false;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (updater != null)
            {
                updater.Dispose();
                updater = null;
            }
            // Dispose each pin
            lock(pins)
            {
                for (int i = pinCount - 1; i >= 0; i--)
                {
                    if (pins.ContainsKey(i))
                    {
                        pins[i].Pin.Dispose();
                        pins.Remove(i);
                    }
                }
            }
            pins = null;
        }

        /// <inheritdoc/>
        public void EnablePin(int pin)
        {
            if ((pin < 0) || (pin > (pinCount - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pins)
            {
                if (!pins.ContainsKey(pin)) { throw new UnauthorizedAccessException(); }
                pins[pin].Enabled = true;
            }

            // Make sure updates are running
            if (!updater.IsStarted) { updater.Start(); }
        }

        /// <inheritdoc/>
        public void ReleasePin(int pin)
        {
            if ((pin < 0) || (pin > (pinCount - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pins)
            {
                if (!pins.ContainsKey(pin)) { throw new UnauthorizedAccessException(); }
                pins[pin].Pin.Dispose();
                pins.Remove(pin);
            }
        }

        /// <inheritdoc/>
        public double SetDesiredFrequency(double frequency)
        {
            if (frequency < MIN_FREQUENCY || frequency > MAX_FREQUENCY)
            {
                throw new ArgumentOutOfRangeException(nameof(frequency));
            }

            actualFrequency = frequency;

            return actualFrequency;
        }

        /// <inheritdoc/>
        public void SetPulseParameters(int pin, double dutyCycle, bool invertPolarity)
        {
            if ((pin < 0) || (pin > (pinCount - 1))) throw new ArgumentOutOfRangeException("pin");

            lock (pins)
            {
                if (!pins.ContainsKey(pin)) { throw new UnauthorizedAccessException(); }
                var softPin = pins[pin];
                softPin.DutyCycle = dutyCycle;
                softPin.InvertPolarity = invertPolarity;
            }
            
            // If duty cycle isn't zero we need to make sure updates are running
            if ((dutyCycle != 0) && (!updater.IsStarted)) { updater.Start(); }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <inheritdoc/>
        public double ActualFrequency
        {
            get
            {
                return actualFrequency;
            }
        }

        /// <inheritdoc/>
        public double MaxFrequency
        {
            get
            {
                return MAX_FREQUENCY;
            }
        }

        /// <inheritdoc/>
        public double MinFrequency
        {
            get
            {
                return MIN_FREQUENCY;
            }
        }

        /// <inheritdoc/>
        public int PinCount
        {
            get
            {
                return pinCount;
            }
        }
        #endregion // Public Properties
    }
}
