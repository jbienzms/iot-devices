// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.Devices
{
    /// <summary>
    /// A default implementation of the <see cref="IScheduler"/> interface.
    /// </summary>
    public sealed class Scheduler : IScheduler
    {
        #region Nested Classes
        private class Subscription
        {
            public bool IsSuspended { get; set; }
            public ScheduleOptions Options { get; set; }
        }
        private class Lookup<T> : Dictionary<T, Subscription> { }
        #endregion // Nested Classes

        #region Static Version
        #region Constants
        private const uint DefaultReportInterval = 500;
        #endregion // Constants

        #region Member Variables
        static private Scheduler defaultScheduler;
        #endregion // Member Variables

        #region Public Properties
        /// <summary>
        /// Gets the default shared scheduler.
        /// </summary>
        static public Scheduler Default
        {
            get
            {
                if (defaultScheduler == null)
                {
                    defaultScheduler = new Scheduler();
                }
                return defaultScheduler;
            }
        }
        #endregion // Public Properties
        #endregion // Static Version

        #region Instance Version
        #region Member Variables
        private Lookup<IAsyncAction> asyncSubscriptions;
        private CancellationTokenSource cancellationSource;
        private uint reportInterval = DefaultReportInterval;
        private Lookup<ScheduledAction> subscriptions;
        private Task updateTask;
        #endregion // Member Variables

        #region Constructors
        /// <summary>
        /// Initializes a new <see cref="Scheduler"/> instance.
        /// </summary>
        public Scheduler()
        {
            AutoStart = true;
        }
        #endregion // Constructors


        #region Internal Methods
        /// <summary>
        /// Ensures that the report inverval is at least as short as the specified interval.
        /// </summary>
        /// <param name="interval">
        /// The interval to check.
        /// </param>
        private void EnsureMinReportInterval(uint interval)
        {
            // Get count
            int acount = (asyncSubscriptions?.Count ?? 0);
            int scount = (subscriptions?.Count ?? 0);

            // If only one subscriber, just use it. Otherwise make sure minimum
            if ((acount + scount) == 1)
            {
                reportInterval = interval;
            }
            else
            {
                reportInterval = Math.Min(reportInterval, interval);
            }
        }

        private Subscription GetSubscription(IAsyncAction subscriber, bool throwIfMissing = true)
        {
            // Validate
            if (subscriber == null) throw new ArgumentNullException("subscriber");

            // Try to get the subscription
            Subscription sub = null;
            if ((asyncSubscriptions == null) || (!asyncSubscriptions.TryGetValue(subscriber, out sub)))
            {
                if (throwIfMissing)
                {
                    throw new InvalidOperationException(Strings.SubscriptionNotFound);
                }
            }
            return sub;
        }

        private Subscription GetSubscription(ScheduledAction subscriber, bool throwIfMissing = true)
        {
            // Validate
            if (subscriber == null) throw new ArgumentNullException("subscriber");

            // Try to get the subscription
            Subscription sub = null;
            if ((subscriptions == null) || (!subscriptions.TryGetValue(subscriber, out sub)))
            {
                if (throwIfMissing)
                {
                    throw new InvalidOperationException(Strings.SubscriptionNotFound);
                }
            }
            return sub;
        }

        private void QueryStart()
        {
            if (AutoStart)
            {
                Start();
            }
        }

        private void QueryStop()
        {
            if ((asyncSubscriptions == null) || (asyncSubscriptions.Count == 0))
            {
                if ((subscriptions == null) || (subscriptions.Count == 0))
                {
                    Stop();
                }
            }
        }

        /// <summary>
        /// Recalculates the shortest report interval based on all subscribers that are enabled.
        /// </summary>
        private void RecalcReportInterval()
        {
            uint asyncMin = DefaultReportInterval;
            uint syncMin = DefaultReportInterval;

            if ((asyncSubscriptions != null) && (asyncSubscriptions.Count > 0))
            {
                lock (asyncSubscriptions)
                {
                    asyncMin = asyncSubscriptions.Values.Where((s) => !s.IsSuspended).Min((s) => s.Options.UpdateInterval);
                }
            }

            if ((subscriptions != null) && (subscriptions.Count > 0))
            {
                lock (subscriptions)
                {
                    syncMin = subscriptions.Values.Where((s) => !s.IsSuspended).Min((s) => s.Options.UpdateInterval);
                }
            }

            reportInterval = Math.Min(asyncMin, syncMin);
        }

        private Task StartUpdateLoopAsync()
        {
            return Task.Run(async () =>
            {
                // TODO: Find a higher resolution way of tracking time
                while (!cancellationSource.IsCancellationRequested)
                {
                    // Capture start time
                    var loopStart = DateTime.Now;

                    // TODO: Start all asynchronous subscribers

                    // Run all synchronous subscribers
                    if (subscriptions != null)
                    {
                        lock (subscriptions)
                        {
                            foreach (var sub in subscriptions)
                            {
                                if (!sub.Value.IsSuspended)
                                {
                                    sub.Key();
                                }
                            }
                        }
                    }

                    // TODO: Wait for asynchronous subscribers to finish

                    // How much time did the loop take?
                    var loopTime = (DateTime.Now - loopStart).TotalMilliseconds;

                    // If there's any time left, give CPU back
                    if (loopTime < reportInterval)
                    {
                        await Task.Delay((int)(reportInterval - loopTime));
                    }
                }
            });
        }
        #endregion // Internal Methods

        #region Public Methods
        public void Resume(ScheduledAction subscriber)
        {
            var s = GetSubscription(subscriber);
            s.IsSuspended = false;
            EnsureMinReportInterval(s.Options.UpdateInterval);
        }

        public void Resume(IAsyncAction subscriber)
        {
            var s = GetSubscription(subscriber);
            s.IsSuspended = false;
            EnsureMinReportInterval(s.Options.UpdateInterval);
        }

        public void Schedule(ScheduledAction subscriber, ScheduleOptions options)
        {
            // Check for existing subscription
            var sub = GetSubscription(subscriber, false);
            if (sub != null) { throw new InvalidOperationException(Strings.AlreadySubscribed); }

            // Make sure lookup exists
            if (subscriptions == null) { subscriptions = new Lookup<ScheduledAction>(); }

            // Threadsafe
            lock (subscriptions)
            {
                // Add lookup
                subscriptions[subscriber] = new Subscription() { Options = options };
            }

            // Ensure interval
            EnsureMinReportInterval(options.UpdateInterval);

            // Start?
            QueryStart();
        }

        public void Schedule(IAsyncAction subscriber, ScheduleOptions options)
        {
            // Check for existing subscription
            var sub = GetSubscription(subscriber, false);
            if (sub != null) { throw new InvalidOperationException(Strings.AlreadySubscribed); }

            // Make sure lookup exists
            if (asyncSubscriptions == null) { asyncSubscriptions = new Lookup<IAsyncAction>(); }

            // Threadsafe
            lock (asyncSubscriptions)
            {
                // Add lookup
                asyncSubscriptions[subscriber] = new Subscription() { Options = options };
            }

            // Ensure interval
            EnsureMinReportInterval(options.UpdateInterval);

            // Start?
            QueryStart();
        }

        /// <summary>
        /// Starts execution of the scheduler.
        /// </summary>
        /// <remarks>
        /// Important: This method is ignored if no subscribers are scheduled. 
        /// If this method is called on a scheduler that has already started 
        /// it is ignored.
        /// </remarks>
        public void Start()
        {
            // If already running, ignore
            if (IsRunning) { return; }

            // Create (or rest) the cancellation source
            cancellationSource = new CancellationTokenSource();

            // Start the loop
            updateTask = StartUpdateLoopAsync().FailFastOnException();
        }

        /// <summary>
        /// Stops execution of the scheduler.
        /// </summary>
        /// <remarks>
        /// If the scheduler is already stopped this method is ignored.
        /// </remarks>
        public void Stop()
        {
            // If not running, ignore
            if (!IsRunning) { return; }

            // Set cancel flag
            cancellationSource.Cancel();

            // Wait for loop to complete
            updateTask.Wait();

            // Clear variables
            updateTask = null;
            cancellationSource = null;
        }

        public void Suspend(ScheduledAction subscriber)
        {
            GetSubscription(subscriber).IsSuspended = true;
            RecalcReportInterval();
        }

        public void Suspend(IAsyncAction subscriber)
        {
            GetSubscription(subscriber).IsSuspended = true;
            RecalcReportInterval();
        }

        public void Unschedule(ScheduledAction subscriber)
        {
            if (subscriptions != null)
            {
                lock (subscriptions)
                {
                    subscriptions.Remove(subscriber); // Unschedule
                }
            }

            // See if we should stop
            QueryStop();

            // Recalcualte the report interval
            RecalcReportInterval();
        }

        public void Unschedule(IAsyncAction subscriber)
        {
            if (asyncSubscriptions != null)
            {
                lock (asyncSubscriptions)
                {
                    asyncSubscriptions.Remove(subscriber); // Unschedule
                }
            }

            // See if we should stop
            QueryStop();

            // Recalcualte the report interval
            RecalcReportInterval();
        }

        public void UpdateSchedule(ScheduledAction subscriber, ScheduleOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            GetSubscription(subscriber).Options = options;
            if (reportInterval < options.UpdateInterval)
            {
                EnsureMinReportInterval(options.UpdateInterval);
            }
            else
            {
                RecalcReportInterval();
            }
        }

        public void UpdateSchedule(IAsyncAction subscriber, ScheduleOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            GetSubscription(subscriber).Options = options;
            if (reportInterval < options.UpdateInterval)
            {
                EnsureMinReportInterval(options.UpdateInterval);
            }
            else
            {
                RecalcReportInterval();
            }
        }
        #endregion // Public Methods

        #region Public Properties
        /// <summary>
        /// Gets or sets a value that indicates if the scheduler should automatically start 
        /// when the first subscriber is scheduled.
        /// </summary>
        /// <value>
        /// <c>true</c> if if the scheduler should automatically start when the first 
        /// subscriber is scheduled; otherwise false. The default is <c>true</c>.
        /// </value>
        public bool AutoStart { get; set; }

        /// <summary>
        /// Gets a value that indicates if the scheduler is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if if the scheduler is running; otherwise false.
        /// </value>
        public bool IsRunning { get { return updateTask != null; } }
        #endregion // Public Properties
        #endregion // Instance Version
    }
}
