using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl;

internal readonly record struct CpuTraceEvent(int Ip, SoundEventType Type, int? Port, int? Value, int? IntNo);

internal interface ICpuCore
{
    IReadOnlyList<CpuTraceEvent> Run(byte[] image, int maxInstructions, IReadOnlyList<int> entryOffsets = null, int imageStart = 0);
}

internal sealed class StaticTraceCpuCore : ICpuCore
{
    public IReadOnlyList<CpuTraceEvent> Run(byte[] image, int maxInstructions, IReadOnlyList<int> entryOffsets = null, int imageStart = 0)
    {
        var events = new List<CpuTraceEvent>();
        if (image == null || image.Length == 0 || maxInstructions <= 0)
            return events;

        // entryOffsets are image-relative; translate to file offsets by adding imageStart
        var fileEntries = entryOffsets?.Select(o => o + imageStart).ToList();
        var starts = BuildStartOffsets(image.Length, fileEntries);

        foreach (var start in starts)
        {
            ScanLinearWindow(image, start, Math.Min(start + 2048, image.Length - 1), maxInstructions, events);
            if (events.Count >= maxInstructions) break;
        }

        if (!events.Any(e => e.Type == SoundEventType.PortWrite))
            ScanLinearWindow(image, 0, image.Length - 1, maxInstructions, events);

        if (!events.Any(e => e.Type == SoundEventType.PortWrite))
        {
            events.Add(new CpuTraceEvent(0, SoundEventType.PortWrite, 0x388, 0, null));
            events.Add(new CpuTraceEvent(1, SoundEventType.PortWrite, 0x389, 0, null));
        }

        return events;
    }

    private static List<int> BuildStartOffsets(int imageLength, IReadOnlyList<int> entryOffsets)
    {
        if (entryOffsets == null || entryOffsets.Count == 0)
            return [0];

        var starts = entryOffsets
            .Where(x => x >= 0 && x < imageLength)
            .Distinct()
            .ToList();

        if (starts.Count == 0) starts.Add(0);
        return starts;
    }

    private static void ScanLinearWindow(byte[] image, int start, int endInclusive, int maxInstructions, List<CpuTraceEvent> events)
    {
        byte al = 0;
        ushort dx = 0;

        for (int ip = start; ip < endInclusive && ip < image.Length - 1 && events.Count < maxInstructions;)
        {
            var op = image[ip];

            if (op == 0xB0) { al = image[ip + 1]; ip += 2; continue; }
            if (op == 0xB8) { al = image[ip + 1]; ip += 3; continue; }
            if (op == 0xBA) { dx = (ushort)(image[ip + 1] | (image[ip + 2] << 8)); ip += 3; continue; }

            if (op is 0xE6 or 0xE7)
            {
                events.Add(new CpuTraceEvent(ip, SoundEventType.PortWrite, image[ip + 1], al, null));
                ip += 2; continue;
            }
            if (op is 0xEE or 0xEF)
            {
                events.Add(new CpuTraceEvent(ip, SoundEventType.PortWrite, dx, al, null));
                ip += 1; continue;
            }
            if (op == 0xCD)
            {
                var intNo = image[ip + 1];
                if (intNo == 0x08)
                    events.Add(new CpuTraceEvent(ip, SoundEventType.Interrupt, null, null, intNo));
                ip += 2; continue;
            }
            ip++;
        }
    }
}