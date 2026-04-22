using System;
using System.Collections.Generic;

#nullable enable

namespace CivOne.Sound.Cvl;

internal static class CivPlayScheduler
{
    public static List<SoundEvent> BuildWorkerTimeline(int ticks, Func<int, bool>? workerReturnNonZero = null)
    {
        var events = new List<SoundEvent>();

        bool fastWorker = false;
        byte counter1 = 5;
        byte counter2 = 0x10;

        for (int t = 1; t <= ticks; t++)
        {
            if (fastWorker)
            {
                events.Add(new SoundEvent { T = t, Type = SoundEventType.Worker, Kind = "fast" });
            }

            counter1--;
            if (counter1 == 0)
            {
                counter1 = 5;
                events.Add(new SoundEvent { T = t, Type = SoundEventType.Worker, Kind = "sound" });

                if (workerReturnNonZero?.Invoke(t) == true)
                {
                    fastWorker = true;
                }
            }

            counter2--;
            if (counter2 == 0)
            {
                counter2 = 0x10;
                events.Add(new SoundEvent { T = t, Type = SoundEventType.Interrupt, IntNo = 0x08 });
            }
        }

        return events;
    }
}


