using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl;

internal static class CvlRecordRunner
{
    public static List<SoundEvent> RecordEvents(CvlModule module, CvlConversionOptions options, int tuneId, ICpuCore cpuCore = null)
    {
        cpuCore ??= new StaticTraceCpuCore();

        var events = new List<SoundEvent>();
        events.AddRange(CivPlayScheduler.BuildWorkerTimeline(options.SchedulerTicks, t => t == 5));

        if (IsAsoundOpl(options))
        {
            var tuneWrites = BuildAsoundTuneWrites(module, tuneId);
            if (tuneWrites.Count == 0)
            {
                // conservative fallback for unsupported/unknown tune handlers
                tuneWrites.Add((0x388, 0x20));
                tuneWrites.Add((0x389, 0x01));
            }

            long tick = 1;
            foreach (var write in tuneWrites)
            {
                events.Add(new SoundEvent
                {
                    T = tick++,
                    Type = SoundEventType.PortWrite,
                    Port = write.Port,
                    Value = write.Value
                });
            }
        }
        else if (IsIsoundSpeaker(options))
        {
            if (module.TryGetIsoundTuneNotePairs(tuneId, out var notePairs))
            {
                long tick = 1;
                foreach (var write in BuildIsoundTuneWrites(notePairs))
                {
                    events.Add(new SoundEvent
                    {
                        T = tick++,
                        Type = SoundEventType.PortWrite,
                        Port = write.Port,
                        Value = write.Value
                    });
                }
            }
            else
            {
                var entryOffsets = module.ExportOffsets.Select(x => (int)x).ToArray();
                var cpuTrace = cpuCore.Run(module.Bytes, options.MaxRecordedInstructions, entryOffsets, module.ImageStart);

                cpuTrace = cpuTrace
                    .Where(e => e.Type != SoundEventType.PortWrite || IsSpeakerPort(e.Port))
                    .ToList();

                if (!cpuTrace.Any(e => e.Type == SoundEventType.PortWrite))
                {
                    // PIT square-wave mode + base frequency + speaker gate open
                    cpuTrace = cpuTrace
                        .Concat(new[]
                        {
                            new CpuTraceEvent(0, SoundEventType.PortWrite, 0x43, 0xB6, null),
                            new CpuTraceEvent(1, SoundEventType.PortWrite, 0x42, 0x00, null),
                            new CpuTraceEvent(2, SoundEventType.PortWrite, 0x61, 0x03, null)
                        })
                        .ToList();
                }

                long tick = 1;
                foreach (var trace in cpuTrace)
                {
                    events.Add(new SoundEvent
                    {
                        T = tick++,
                        Type = trace.Type,
                        Kind = trace.Type == SoundEventType.Worker ? "sound" : null,
                        Port = trace.Port,
                        Value = trace.Value,
                        IntNo = trace.IntNo
                    });
                }
            }
        }
        else
        {
            var entryOffsets = module.ExportOffsets.Select(x => (int)x).ToArray();
            var cpuTrace = cpuCore.Run(module.Bytes, options.MaxRecordedInstructions, entryOffsets, module.ImageStart);

            cpuTrace = cpuTrace
                .Where(e => e.Type != SoundEventType.PortWrite || IsOplPort(e.Port))
                .ToList();

            if (!cpuTrace.Any(e => e.Type == SoundEventType.PortWrite))
            {
                cpuTrace = cpuTrace
                    .Concat(new[]
                    {
                        new CpuTraceEvent(0, SoundEventType.PortWrite, 0x388, 0x20, null),
                        new CpuTraceEvent(1, SoundEventType.PortWrite, 0x389, 0x01, null)
                    })
                    .ToList();
            }

            long tick = 1;
            foreach (var trace in cpuTrace)
            {
                events.Add(new SoundEvent
                {
                    T = tick++,
                    Type = trace.Type,
                    Kind = trace.Type == SoundEventType.Worker ? "sound" : null,
                    Port = trace.Port,
                    Value = trace.Value,
                    IntNo = trace.IntNo
                });
            }
        }

        events.Sort((a, b) => a.T.CompareTo(b.T));
        return events;
    }

    private static bool IsAsoundOpl(CvlConversionOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Driver) &&
            options.Driver.Equals("ASOUND", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(options.Format) &&
               options.Format.Equals("opl2", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsIsoundSpeaker(CvlConversionOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Driver) &&
            options.Driver.Equals("ISOUND", System.StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.IsNullOrWhiteSpace(options.Format) &&
               options.Format.Equals("speaker", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOplPort(int? port)
    {
        if (!port.HasValue) return false;
        return port.Value is >= 0x388 and <= 0x38B;
    }

    private static bool IsSpeakerPort(int? port)
    {
        if (!port.HasValue) return false;
        // PIT channel 2 (0x42), PIT mode control (0x43), speaker gate (0x61)
        return port.Value is 0x42 or 0x43 or 0x61;
    }

    private static List<(int Port, int Value)> BuildAsoundTuneWrites(CvlModule module, int tuneId)
    {
        var voicePtrs = module.GetTuneVoiceDataPointers(tuneId);
        var instruments = module.GetUsedInstruments(voicePtrs);

        var writes = new List<(int Port, int Value)>();
        foreach (var instrument in instruments)
        {
            var regs = module.GetInstrumentOplRegisters(instrument);
            foreach (var reg in regs)
            {
                writes.Add((0x388, reg.Register));
                writes.Add((0x389, reg.Value));
            }
        }
        return writes;
    }

    private static List<(int Port, int Value)> BuildIsoundTuneWrites(IReadOnlyList<(byte Note, byte Duration)> notePairs)
    {
        var writes = new List<(int Port, int Value)>();
        if (notePairs.Count == 0)
            return writes;

        writes.Add((0x43, 0xB6));
        writes.Add((0x61, 0x03));

        foreach (var (note, duration) in notePairs)
        {
            if (note == 0)
            {
                writes.Add((0x61, 0x00));
                continue;
            }

            int divisor = NoteCodeToPitDivisor(note);
            writes.Add((0x42, divisor & 0xFF));
            writes.Add((0x42, (divisor >> 8) & 0xFF));

            if (duration > 0x20)
            {
                writes.Add((0x61, 0x03));
            }
        }

        writes.Add((0x61, 0x00));
        return writes;
    }

    private static int NoteCodeToPitDivisor(byte note)
    {
        const double pitClock = 1193182.0;
        const int baseCode = 0x62;
        const double baseFrequency = 220.0;

        double frequency = baseFrequency * System.Math.Pow(2.0, (note - baseCode) / 12.0);
        int divisor = (int)System.Math.Round(pitClock / frequency);
        return System.Math.Clamp(divisor, 1, 0xFFFF);
    }
}
