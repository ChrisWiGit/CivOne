using System;
using System.Collections.Generic;
using System.IO;

namespace CivOne.Sound.Cvl;

internal sealed class CvlModule
{
    private const int AsoundTuneDispatchTable = 0x1D0C;
    private const int AsoundInstrumentDataBase = 0x695A;
    private const int AsoundInstrumentSize = 0x2C;
    private const int AsoundInstrumentOp2Off = 0x16;
    private const int IsoundTuneDispatchTable = 0x0588;
    private const int MaxTuneCount = 0x2D;

    public required string FilePath { get; init; }
    public required byte[] Bytes { get; init; }
    public int ImageStart { get; init; }

    public required ushort[] ExportOffsets { get; init; }
    public ushort OverlayCodeSegment { get; init; }
    public ushort ImageSegment { get; init; }
    public ushort ImageSizeParagraphs { get; init; }

    private static ushort U16(byte[] data, int offset)
        => (ushort)(data[offset] | (data[offset + 1] << 8));

    private int ImgFile(int imageOffset) => ImageStart + imageOffset;

    public static CvlModule Load(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 0x60)
            throw new InvalidOperationException("CVL ist zu klein oder ungültig.");

        int imageStart = U16(bytes, 0x08) * 16;
        if (imageStart + 0x50 > bytes.Length)
            throw new InvalidOperationException("CVL-Image-Bereich liegt außerhalb der Datei.");

        return new CvlModule
        {
            FilePath = Path.GetFullPath(path),
            Bytes = bytes,
            ImageStart = imageStart,
            OverlayCodeSegment = U16(bytes, imageStart + 0x28),
            ImageSegment = U16(bytes, imageStart + 0x2A),
            ImageSizeParagraphs = U16(bytes, imageStart + 0x2C),
            ExportOffsets = new[]
            {
                U16(bytes, imageStart + 0x32),
                U16(bytes, imageStart + 0x34),
                U16(bytes, imageStart + 0x36),
                U16(bytes, imageStart + 0x38),
                U16(bytes, imageStart + 0x3A),
                U16(bytes, imageStart + 0x3C)
            }
        };
    }

    public IReadOnlyList<int> GetTuneVoiceDataPointers(int tuneId)
    {
        if (tuneId < 0 || tuneId >= MaxTuneCount) return [];

        int tableFileOff = ImgFile(AsoundTuneDispatchTable + tuneId * 2);
        if (tableFileOff + 1 >= Bytes.Length) return [];

        int handlerImgOff = U16(Bytes, tableFileOff);
        if (handlerImgOff == 0) return [];

        int p = ImgFile(handlerImgOff);
        int limit = Math.Min(p + 512, Bytes.Length - 4);

        var pointers = new List<int>();
        while (p < limit)
        {
            byte op = Bytes[p];
            if (op == 0xCB || op == 0xC3) break;

            if (op == 0x8D && Bytes[p + 1] == 0x0E)
            {
                pointers.Add(Bytes[p + 2] | (Bytes[p + 3] << 8));
                p += 7;
            }
            else
            {
                p++;
            }
        }

        return pointers;
    }

    public bool TryGetIsoundTuneNotePairs(int tuneId, out IReadOnlyList<(byte Note, byte Duration)> notePairs)
    {
        notePairs = [];
        if (tuneId < 0 || tuneId >= MaxTuneCount) return false;

        int tableFileOff = ImgFile(IsoundTuneDispatchTable + tuneId * 2);
        if (tableFileOff + 1 >= Bytes.Length) return false;

        int handlerImgOff = U16(Bytes, tableFileOff);
        if (handlerImgOff <= 0) return false;

        int handlerFileOff = ImgFile(handlerImgOff);
        if (handlerFileOff < 0 || handlerFileOff >= Bytes.Length) return false;

        byte firstOp = Bytes[handlerFileOff];
        if (firstOp is 0xC3 or 0xCB)
        {
            notePairs = [];
            return true;
        }

        int bestPointer = FindBestIsoundSequencePointer(handlerFileOff);
        if (bestPointer < 0) return false;

        notePairs = ReadIsoundNotePairs(bestPointer);
        return true;
    }

    public IReadOnlyList<byte> GetUsedInstruments(IReadOnlyList<int> voiceImagePointers)
    {
        var seen = new HashSet<byte>();
        var ordered = new List<byte>();

        foreach (int imgPtr in voiceImagePointers)
        {
            int f = ImgFile(imgPtr);
            int lim = Math.Min(f + 8192, Bytes.Length - 1);

            for (int i = f; i < lim;)
            {
                byte b = Bytes[i];
                if (b == 0xFD) break;

                if (b == 0xFC && i + 1 < lim)
                {
                    byte instr = Bytes[i + 1];
                    if (seen.Add(instr)) ordered.Add(instr);
                    i += 2;
                }
                else if (b == 0xF3 || b == 0xF8)
                {
                    i += 3;
                }
                else
                {
                    i += 2;
                }
            }
        }

        return ordered;
    }

    public IReadOnlyList<(int Register, int Value)> GetInstrumentOplRegisters(int instrumentNumber)
    {
        int op1File = ImgFile(AsoundInstrumentDataBase + instrumentNumber * AsoundInstrumentSize);
        int op2File = op1File + AsoundInstrumentOp2Off;
        if (op2File + AsoundInstrumentOp2Off > Bytes.Length) return [];

        var writes = new List<(int, int)>();
        BuildOpRegs(writes, Bytes, op1File, slot: 0);
        BuildOpRegs(writes, Bytes, op2File, slot: 3);

        int connection = Bytes[op1File + 10];
        int feedbackBit = Bytes[op1File + 13] == 0 ? 1 : 0;
        writes.Add((0xC0, ((connection << 1) | feedbackBit) & 0xFF));
        return writes;
    }

    private int FindBestIsoundSequencePointer(int handlerFileOff)
    {
        int limit = Math.Min(handlerFileOff + 128, Bytes.Length - 4);
        int bestPointer = -1;
        int bestScore = int.MinValue;

        for (int i = handlerFileOff; i < limit; i++)
        {
            if (Bytes[i] == 0x8D && Bytes[i + 1] == 0x1E)
            {
                int candidateImgOff = U16(Bytes, i + 2);
                int score = ScoreIsoundSequencePointer(candidateImgOff);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPointer = candidateImgOff;
                }
            }

            if (Bytes[i] is 0xC3 or 0xCB) break;
        }

        return bestScore >= 4 ? bestPointer : -1;
    }

    private int ScoreIsoundSequencePointer(int imageOffset)
    {
        int fileOff = ImgFile(imageOffset);
        if (fileOff < 0 || fileOff + 7 >= Bytes.Length) return int.MinValue;

        int score = 0;
        int zeroPairs = 0;
        int validPairs = 0;

        for (int i = 0; i < 24 && fileOff + (i * 2) + 1 < Bytes.Length; i++)
        {
            byte note = Bytes[fileOff + (i * 2)];
            byte duration = Bytes[fileOff + (i * 2) + 1];

            if (note == 0 && duration == 0)
            {
                zeroPairs++;
                score += 1;
                continue;
            }

            if (IsLikelyIsoundNoteByte(note) && duration is > 0 and <= 0x80)
            {
                validPairs++;
                score += 3;
                continue;
            }

            if (duration <= 0xC0)
            {
                score -= 1;
            }
            else
            {
                score -= 3;
            }
        }

        if (validPairs == 0) return int.MinValue;
        return score + Math.Min(zeroPairs, 2);
    }

    private IReadOnlyList<(byte Note, byte Duration)> ReadIsoundNotePairs(int imageOffset)
    {
        int fileOff = ImgFile(imageOffset);
        if (fileOff < 0 || fileOff >= Bytes.Length) return [];

        var result = new List<(byte Note, byte Duration)>();
        int zeroPairRun = 0;
        int limit = Math.Min(fileOff + 512, Bytes.Length - 1);

        for (int i = fileOff; i + 1 < limit && result.Count < 128; i += 2)
        {
            byte note = Bytes[i];
            byte duration = Bytes[i + 1];

            if (note == 0 && duration == 0)
            {
                zeroPairRun++;
                if (zeroPairRun >= 2 && !HasNonZeroByteAhead(i + 2, limit, 12))
                    break;
                continue;
            }

            zeroPairRun = 0;
            if (!IsLikelyIsoundNoteByte(note) || duration == 0)
            {
                if (result.Count > 0) break;
                continue;
            }

            result.Add((note, duration));
        }

        return result;
    }

    private bool HasNonZeroByteAhead(int start, int limit, int length)
    {
        int end = Math.Min(start + length, limit);
        for (int i = start; i < end; i++)
        {
            if (Bytes[i] != 0) return true;
        }
        return false;
    }

    private static bool IsLikelyIsoundNoteByte(byte note)
        => note is >= 0x40 and <= 0x9F;

    private static void BuildOpRegs(List<(int, int)> writes, byte[] bytes, int fileOff, int slot)
    {
        byte attack = bytes[fileOff + 0];
        byte decay = bytes[fileOff + 1];
        byte sustain = bytes[fileOff + 2];
        byte release = bytes[fileOff + 3];
        bool egt = bytes[fileOff + 4] != 0;
        bool ksr = bytes[fileOff + 5] != 0;
        byte tl = bytes[fileOff + 6];
        byte ksl = bytes[fileOff + 7];
        byte waveform = bytes[fileOff + 8];
        byte mult = bytes[fileOff + 9];
        bool am = bytes[fileOff + 11] != 0;
        bool vib = bytes[fileOff + 12] != 0;

        writes.Add((0x20 + slot, ((am ? 0x80 : 0) | (vib ? 0x40 : 0) | (egt ? 0x20 : 0) | (ksr ? 0x10 : 0) | (mult & 0x0F)) & 0xFF));
        writes.Add((0x40 + slot, ((0x3F - (tl & 0x3F)) | ((ksl & 3) << 6)) & 0xFF));
        writes.Add((0x60 + slot, (((attack & 0x0F) << 4) | (decay & 0x0F)) & 0xFF));
        writes.Add((0x80 + slot, (((sustain & 0x0F) << 4) | (release & 0x0F)) & 0xFF));
        writes.Add((0xE0 + slot, (waveform & 3) & 0xFF));
    }
}


