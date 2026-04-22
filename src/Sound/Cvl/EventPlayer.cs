using System;
using System.Linq;

namespace CivOne.Sound.Cvl;

internal sealed class EventPlayer
{
    public void PlayTune(SoundPack pack, int tuneId, IEventSink sink, int loopIterations = 1)
    {
        var tune = pack.Tunes.FirstOrDefault(x => x.TuneId == tuneId)
            ?? throw new InvalidOperationException($"Tune {tuneId} nicht gefunden in Pack {pack.Id}.");

        int loops = tune.EndlessLoop ? Math.Max(loopIterations, 1) : 1;
        ulong sequence = 0;

        for (int i = 0; i < loops; i++)
        {
            foreach (var e in tune.Events)
            {
                sequence++;
                var timeNs = ToVirtualTimeNs(e.T, pack.TickRate);
                var ctx = new EventContext(sequence, e.T, timeNs);

                switch (e.Type)
                {
                    case SoundEventType.Worker:
                        sink.OnWorkerCall(e.Kind ?? "sound", ctx);
                        break;

                    case SoundEventType.PortWrite:
                        sink.OnPortWrite(e.Port ?? 0, e.Value ?? 0, ctx);
                        break;

                    case SoundEventType.Interrupt:
                        sink.OnInterrupt(e.IntNo ?? 0, ctx);
                        break;

                    default:
                        throw new InvalidOperationException($"Unbekannter Event-Typ: {e.Type}");
                }
            }
        }
    }

    private static ulong ToVirtualTimeNs(long tick, int tickRate)
    {
        if (tick <= 0 || tickRate <= 0) return 0;
        return (ulong)((tick * 1_000_000_000L) / tickRate);
    }
}


