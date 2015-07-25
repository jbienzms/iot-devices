// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.IoT.Devices
{
    static internal class SchedulerExtensions
    {
        static private void GetSubscriber(Delegate subscriber, out IAsyncAction a, out ScheduledAction s)
        {
            a = subscriber as IAsyncAction;
            s = subscriber as ScheduledAction;

            if ((a == null) && (s == null))
            {
                throw new InvalidOperationException(Strings.InvalidSubscriberType);
            }
        }

        static internal void ValidateSubscriber(Delegate subscriber)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
        }

        static public void Resume(this IScheduler scheduler, Delegate subscriber)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
            if (a != null)
            {
                scheduler.Resume(a);
            }
            else
            {
                scheduler.Resume(s);
            }
        }

        static public void Schedule(this IScheduler scheduler, Delegate subscriber, ScheduleOptions options)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
            if (a != null)
            {
                scheduler.Schedule(a, options);
            }
            else
            {
                scheduler.Schedule(s, options);
            }
        }

        static public void Suspend(this IScheduler scheduler, Delegate subscriber)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
            if (a != null)
            {
                scheduler.Suspend(a);
            }
            else
            {
                scheduler.Suspend(s);
            }
        }

        static public void Unschedule(this IScheduler scheduler, Delegate subscriber)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
            if (a != null)
            {
                scheduler.Unschedule(a);
            }
            else
            {
                scheduler.Unschedule(s);
            }
        }

        static public void UpdateSchedule(this IScheduler scheduler, Delegate subscriber, ScheduleOptions options)
        {
            IAsyncAction a;
            ScheduledAction s;
            GetSubscriber(subscriber, out a, out s);
            if (a != null)
            {
                scheduler.UpdateSchedule(a, options);
            }
            else
            {
                scheduler.UpdateSchedule(s, options);
            }
        }
    }
}
