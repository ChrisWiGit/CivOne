using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl.Ibm;



/// <summary>
/// Code addresses of ISOUND.CVL derived from the module itself. None of this is
/// hardcoded, so the parser is not tied to a single build.
/// </summary>
internal sealed class IsoundLayout
{
    public required int DispatchTable { get; init; }
    public required int MaxTuneId { get; init; }
    public required int MusicPlayer { get; init; }
    public required int EffectPlayer { get; init; }
    public required int EffectParamTable { get; init; }

    /// <summary>Timbre code for which the driver plays without an effect (0x7E in the 11-14-91 build).</summary>
    public required int PlainTimbreCode { get; init; }

    /// <summary>First code that points into the effect table (0x65 in the 11-14-91 build).</summary>
    public required int FirstTimbreCode { get; init; }

    public int TuneCount => MaxTuneId + 1;
}

/// <summary>Result of analyzing a single tune handler.</summary>
internal sealed class IsoundTuneInfo
{
    public int TuneId { get; init; }
    public TuneScoreKind Kind { get; init; }
    public int HandlerOffset { get; init; }

    /// <summary>Data-segment offset of the sequence, or -1.</summary>
    public int DataOffset { get; init; } = -1;

    /// <summary>Code offset of the player routine the handler jumps to, or -1.</summary>
    public int PlayerOffset { get; init; } = -1;

    public List<TuneStep> Steps { get; init; } = [];

    /// <summary>Reason why the handler could not be interpreted as a sequence, if any.</summary>
    public string? Diagnostic { get; init; }
}

/// <summary>
/// Reads the tune sequences from ISOUND.CVL (the IBM PC speaker driver).
///
/// Layout, verified against the disassembled driver:
/// <code>
///   Export[1] (PlayTuneFn):  cmp bx,MaxTuneId ; shl bx,1 ; call word ptr cs:[bx+DispatchTable]
///
///   Handler:                 8D 1E &lt;ptr16&gt;   lea bx,[ptr]   ; ptr is DATA-SEGMENT-relative
///                            E9 &lt;rel16&gt;      jmp &lt;Player&gt;
///
///   Music record (4 bytes):  +0 byte timbre/priority
///                            +1 byte duration in worker ticks
///                            +2 word PIT divisor (0 = rest)
///
///   Effect record (10 bytes): +0 word priority   +2 word duration   +4 word noise mask
///                            +6 word PIT divisor +8 word slide/vibrato parameter
///                            Mask 0 = silence; the record is then only 6 bytes long.
///
///   End of a sequence:       word at the start of the record == 0
/// </code>
///
/// The divisor is stored literally in the file; there is <em>no</em> note-to-frequency table.
/// For music, the timbre code selects a vibrato or slide parameter via a table in the code
/// segment; for effects, the parameter is stored directly in the record.
/// </summary>
internal sealed class IsoundParser
{
    private const int MaxStepsPerTune = 8192;
    private const int PlayerScanLength = 192;
    private const int MaxHandlerCandidates = 256;

    private readonly CvlImage _image;

    private IsoundParser(CvlImage image, IsoundLayout layout)
    {
        _image = image;
        Layout = layout;
    }

    public IsoundLayout Layout { get; }

    public CvlImage Image => _image;

    public static IsoundParser Create(CvlImage image)
        => TryCreate(image, out var parser, out string? error)
            ? parser!
            : throw new InvalidOperationException($"ISOUND layout not recognized: {error}");

    public static bool TryCreate(CvlImage image, out IsoundParser? parser, out string? error)
    {
        ArgumentNullException.ThrowIfNull(image);
        parser = null;

        if (!new CvlDispatchTableDelegate().TryRead(image, out int dispatchTable, out int maxTuneId, out error))
            return false;

        if (!TryFindPlayers(image, dispatchTable, maxTuneId, out int musicPlayer, out int effectPlayer, out error))
            return false;

        if (!TryFindEffectParamTable(image, musicPlayer, out int paramTable, out int plainCode, out int firstCode, out error))
            return false;

        parser = new IsoundParser(image, new IsoundLayout
        {
            DispatchTable = dispatchTable,
            MaxTuneId = maxTuneId,
            MusicPlayer = musicPlayer,
            EffectPlayer = effectPlayer,
            EffectParamTable = paramTable,
            PlainTimbreCode = plainCode,
            FirstTimbreCode = firstCode
        });

        error = null;
        return true;
    }

    public IsoundTuneInfo ParseTune(int tuneId)
    {
        if (tuneId < 0 || tuneId > Layout.MaxTuneId)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = -1,
                Diagnostic = $"TuneId {tuneId} is outside 0..{Layout.MaxTuneId}."
            };
        }

        if (!_image.TryCodeWord(Layout.DispatchTable + tuneId * 2, out ushort handler) || handler == 0)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = "Dispatch entry is empty."
            };
        }

        if (!_image.TryCodeByte(handler, out byte opcode))
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = $"Handler 0x{handler:X4} is outside the file."
            };
        }

        // RET / RETF: the driver knows the tune but deliberately plays nothing (e.g. tune 4).
        if (opcode is 0xC3 or 0xCB)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Silent,
                HandlerOffset = handler
            };
        }

        if (!TryReadHandlerJump(handler, out int dataOffset, out int player))
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = $"Handler 0x{handler:X4} is not a 'lea bx,[ptr] / jmp player' sequence "
                             + "(likely a control function like Stop or status query)."
            };
        }

        if (player == Layout.MusicPlayer)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Music,
                HandlerOffset = handler,
                DataOffset = dataOffset,
                PlayerOffset = player,
                Steps = ReadMusicSteps(dataOffset)
            };
        }

        if (player == Layout.EffectPlayer)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Effect,
                HandlerOffset = handler,
                DataOffset = dataOffset,
                PlayerOffset = player,
                Steps = ReadEffectSteps(dataOffset)
            };
        }

        return new IsoundTuneInfo
        {
            TuneId = tuneId,
            Kind = TuneScoreKind.Unsupported,
            HandlerOffset = handler,
            DataOffset = dataOffset,
            PlayerOffset = player,
            Diagnostic = $"Handler jumps to 0x{player:X4}, which is not a known player routine."
        };
    }

    /// <summary>Effect parameter the driver looks up for a music timbre code.</summary>
    public int ResolveMusicEffect(int timbreCode)
    {
        if (timbreCode == Layout.PlainTimbreCode) return 0;
        if (timbreCode < Layout.FirstTimbreCode) return 0;

        int index = timbreCode - Layout.FirstTimbreCode;
        return _image.TryCodeWord(Layout.EffectParamTable + index * 2, out ushort value) ? value : 0;
    }

    private bool TryReadHandlerJump(int handler, out int dataOffset, out int player)
    {
        dataOffset = -1;
        player = -1;

        // 8D 1E <ptr16> = lea bx,[ptr]
        if (!_image.CodeMatches(handler, 0x8D, 0x1E)) return false;
        if (!_image.TryCodeWord(handler + 2, out ushort pointer)) return false;

        // E9 <rel16> = jmp near
        if (!_image.CodeMatches(handler + 4, 0xE9)) return false;
        if (!_image.TryCodeWord(handler + 5, out ushort relative)) return false;

        dataOffset = pointer;
        player = (handler + 7 + (short)relative) & 0xFFFF;
        return true;
    }

    private List<TuneStep> ReadMusicSteps(int dataOffset)
    {
        var steps = new List<TuneStep>();
        int p = dataOffset;

        while (steps.Count < MaxStepsPerTune)
        {
            // The worker ends the sequence as soon as the word at the start of the record is 0.
            if (!_image.TryDataWord(p, out ushort head) || head == 0) break;
            if (!_image.TryDataByte(p, out byte timbre)) break;
            if (!_image.TryDataByte(p + 1, out byte duration)) break;
            if (!_image.TryDataWord(p + 2, out ushort divisor)) break;
            p += 4;

            steps.Add(divisor == 0
                ? new TuneStep { Duration = duration, Divisor = 0, Timbre = timbre }
                : new TuneStep
                {
                    Duration = duration,
                    Divisor = divisor,
                    Timbre = timbre,
                    NoiseMask = 1, // the music player hardcodes ds:0x6D to 1
                    Effect = ResolveMusicEffect(timbre)
                });
        }

        return steps;
    }

    private List<TuneStep> ReadEffectSteps(int dataOffset)
    {
        var steps = new List<TuneStep>();
        int p = dataOffset;

        while (steps.Count < MaxStepsPerTune)
        {
            if (!_image.TryDataWord(p, out ushort priority) || priority == 0) break;
            if (!_image.TryDataWord(p + 2, out ushort duration)) break;
            if (!_image.TryDataWord(p + 4, out ushort noiseMask)) break;

            if (noiseMask == 0)
            {
                // A mask of 0 turns the speaker off; the record is then only 6 bytes long.
                steps.Add(new TuneStep { Duration = duration, Divisor = 0, Timbre = priority });
                p += 6;
                continue;
            }

            if (!_image.TryDataWord(p + 6, out ushort divisor)) break;
            if (!_image.TryDataWord(p + 8, out ushort effect)) break;
            p += 10;

            steps.Add(new TuneStep
            {
                Duration = duration,
                Divisor = divisor,
                Timbre = priority,
                NoiseMask = noiseMask,
                Effect = effect
            });
        }

        return steps;
    }

    private static bool TryFindPlayers(CvlImage image, int dispatchTable, int maxTuneId,
        out int musicPlayer, out int effectPlayer, out string? error)
    {
        musicPlayer = -1;
        effectPlayer = -1;

        var targets = new List<int>();
        int limit = Math.Min(maxTuneId, MaxHandlerCandidates);

        for (int tuneId = 0; tuneId <= limit; tuneId++)
        {
            if (!image.TryCodeWord(dispatchTable + tuneId * 2, out ushort handler) || handler == 0) continue;
            if (!image.CodeMatches(handler, 0x8D, 0x1E, -1, -1, 0xE9)) continue;
            if (!image.TryCodeWord(handler + 5, out ushort relative)) continue;

            int target = (handler + 7 + (short)relative) & 0xFFFF;
            if (!targets.Contains(target)) targets.Add(target);
        }

        foreach (int target in targets)
        {
            // 33 C0 C6 06 <imm16> 01 = xor ax,ax ; mov byte ptr ds:[flag],1  -> music player
            if (musicPlayer < 0 && image.CodeMatches(target, 0x33, 0xC0, 0xC6, 0x06, -1, -1, 0x01))
            {
                musicPlayer = target;
                continue;
            }

            // 33 C0 50 A1 <imm16> 3B 07 = xor ax,ax ; push ax ; mov ax,ds:[prio] ; cmp ax,[bx]
            if (effectPlayer < 0 && image.CodeMatches(target, 0x33, 0xC0, 0x50, 0xA1, -1, -1, 0x3B, 0x07))
            {
                effectPlayer = target;
            }
        }

        if (musicPlayer < 0)
        {
            error = targets.Count == 0
                ? $"Dispatch table 0x{dispatchTable:X4} contains no evaluable handlers."
                : "None of the jump targets looks like the music player.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryFindEffectParamTable(CvlImage image, int musicPlayer,
        out int paramTable, out int plainCode, out int firstCode, out string? error)
    {
        paramTable = -1;
        plainCode = -1;
        firstCode = -1;

        // 2E 8B 87 <disp16> = mov ax,cs:[bx+disp16]
        int load = image.FindCodePattern(musicPlayer, PlayerScanLength, 0x2E, 0x8B, 0x87);
        if (load < 0 || !image.TryCodeWord(load + 3, out ushort table))
        {
            error = $"No effect table found in the music player (0x{musicPlayer:X4}).";
            return false;
        }

        // 83 FB <imm8> = cmp bx,<code without effect>
        int compare = image.FindCodePattern(musicPlayer, PlayerScanLength, 0x83, 0xFB);
        if (compare < 0 || !image.TryCodeByte(compare + 2, out byte plain))
        {
            error = $"Music player (0x{musicPlayer:X4}) is missing the comparison for the timbre special case.";
            return false;
        }

        // 83 EB <imm8> = sub bx,<first table code>
        int subtract = image.FindCodePattern(musicPlayer, PlayerScanLength, 0x83, 0xEB);
        if (subtract < 0 || !image.TryCodeByte(subtract + 2, out byte first))
        {
            error = $"Music player (0x{musicPlayer:X4}) is missing the base of the effect table.";
            return false;
        }

        paramTable = table;
        plainCode = plain;
        firstCode = first;
        error = null;
        return true;
    }
}
