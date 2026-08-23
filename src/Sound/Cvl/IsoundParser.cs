using System;
using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Aus dem Modul selbst abgeleitete Codeadressen von ISOUND.CVL. Nichts davon ist
/// fest verdrahtet, damit der Parser nicht an einen einzelnen Build gebunden ist.
/// </summary>
internal sealed class IsoundLayout
{
    public required int DispatchTable { get; init; }
    public required int MaxTuneId { get; init; }
    public required int MusicPlayer { get; init; }
    public required int EffectPlayer { get; init; }
    public required int EffectParamTable { get; init; }

    /// <summary>Timbre-Code, für den der Treiber ohne Effekt spielt (im 11-14-91-Build 0x7E).</summary>
    public required int PlainTimbreCode { get; init; }

    /// <summary>Erster Code, der in die Effekttabelle zeigt (im 11-14-91-Build 0x65).</summary>
    public required int FirstTimbreCode { get; init; }

    public int TuneCount => MaxTuneId + 1;
}

/// <summary>Ergebnis der Analyse eines einzelnen Tune-Handlers.</summary>
internal sealed class IsoundTuneInfo
{
    public int TuneId { get; init; }
    public TuneScoreKind Kind { get; init; }
    public int HandlerOffset { get; init; }

    /// <summary>Datensegment-Offset der Sequenz, oder -1.</summary>
    public int DataOffset { get; init; } = -1;

    /// <summary>Code-Offset der ansprungenen Player-Routine, oder -1.</summary>
    public int PlayerOffset { get; init; } = -1;

    public List<TuneStep> Steps { get; init; } = [];

    /// <summary>Begründung, falls der Handler nicht als Sequenz interpretierbar war.</summary>
    public string? Diagnostic { get; init; }
}

/// <summary>
/// Liest die Tune-Sequenzen aus ISOUND.CVL (IBM-PC-Speaker-Treiber).
///
/// Aufbau, verifiziert am disassemblierten Treiber:
/// <code>
///   Export[1] (PlayTuneFn):  cmp bx,MaxTuneId ; shl bx,1 ; call word ptr cs:[bx+DispatchTable]
///
///   Handler:                 8D 1E &lt;ptr16&gt;   lea bx,[ptr]   ; ptr ist DATENSEGMENT-relativ
///                            E9 &lt;rel16&gt;      jmp &lt;Player&gt;
///
///   Musik-Record (4 Byte):   +0 byte Timbre/Priorität
///                            +1 byte Dauer in Worker-Ticks
///                            +2 word PIT-Divisor (0 = Pause)
///
///   Effekt-Record (10 Byte): +0 word Priorität   +2 word Dauer   +4 word Noise-Maske
///                            +6 word PIT-Divisor +8 word Slide/Vibrato-Parameter
///                            Maske 0 = Stille, der Record ist dann nur 6 Byte lang.
///
///   Ende einer Sequenz:      Wort am Recordanfang == 0
/// </code>
///
/// Der Divisor steht wörtlich in der Datei; es gibt <em>keine</em> Note-nach-Frequenz-Tabelle.
/// Der Timbre-Code wählt bei Musik über eine Tabelle im Codesegment einen Vibrato- oder
/// Slide-Parameter aus; bei Effekten steht der Parameter direkt im Record.
/// </summary>
internal sealed class IsoundParser
{
    private const int MaxStepsPerTune = 8192;
    private const int ExportScanLength = 64;
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
            : throw new InvalidOperationException($"ISOUND-Layout nicht erkannt: {error}");

    public static bool TryCreate(CvlImage image, out IsoundParser? parser, out string? error)
    {
        ArgumentNullException.ThrowIfNull(image);
        parser = null;

        if (!TryFindDispatch(image, out int dispatchTable, out int maxTuneId, out error))
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
                Diagnostic = $"TuneId {tuneId} liegt außerhalb von 0..{Layout.MaxTuneId}."
            };
        }

        if (!_image.TryCodeWord(Layout.DispatchTable + tuneId * 2, out ushort handler) || handler == 0)
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = "Dispatch-Eintrag ist leer."
            };
        }

        if (!_image.TryCodeByte(handler, out byte opcode))
        {
            return new IsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = $"Handler 0x{handler:X4} liegt außerhalb der Datei."
            };
        }

        // RET / RETF: der Treiber kennt den Tune, spielt aber bewusst nichts (z.B. Tune 4).
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
                Diagnostic = $"Handler 0x{handler:X4} ist keine 'lea bx,[ptr] / jmp player'-Sequenz "
                             + "(vermutlich eine Steuerfunktion wie Stop oder Statusabfrage)."
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
            Diagnostic = $"Handler springt nach 0x{player:X4}, das ist keine bekannte Player-Routine."
        };
    }

    /// <summary>Effektparameter, den der Treiber für einen Musik-Timbre-Code nachschlägt.</summary>
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
            // Der Worker beendet die Sequenz, sobald das Wort am Recordanfang 0 ist.
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
                    NoiseMask = 1, // der Musikplayer setzt ds:0x6D fest auf 1
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
                // Maske 0 schaltet den Speaker ab; der Record ist dann nur 6 Byte lang.
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

    private static bool TryFindDispatch(CvlImage image, out int dispatchTable, out int maxTuneId, out string? error)
    {
        dispatchTable = -1;
        maxTuneId = -1;

        if (image.Exports.Count <= CvlImage.ExportPlayTune)
        {
            error = "Das Modul hat keinen PlayTune-Export.";
            return false;
        }

        int playTune = image.Exports[CvlImage.ExportPlayTune];

        // 2E FF 97 <disp16> = call word ptr cs:[bx+disp16]
        int call = image.FindCodePattern(playTune, ExportScanLength, 0x2E, 0xFF, 0x97);
        if (call < 0 || !image.TryCodeWord(call + 3, out ushort table))
        {
            error = $"In PlayTune (0x{playTune:X4}) wurde kein 'call cs:[bx+disp16]' gefunden.";
            return false;
        }

        // 83 FB <imm8> = cmp bx,MaxTuneId
        int compare = image.FindCodePattern(playTune, ExportScanLength, 0x83, 0xFB);
        if (compare < 0 || !image.TryCodeByte(compare + 2, out byte limit))
        {
            error = $"In PlayTune (0x{playTune:X4}) wurde keine Bereichsprüfung 'cmp bx,imm8' gefunden.";
            return false;
        }

        dispatchTable = table;
        maxTuneId = limit;
        error = null;
        return true;
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
            // 33 C0 C6 06 <imm16> 01 = xor ax,ax ; mov byte ptr ds:[flag],1  -> Musikplayer
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
                ? $"Dispatch-Tabelle 0x{dispatchTable:X4} enthält keine auswertbaren Handler."
                : "Keine der angesprungenen Routinen sieht wie der Musikplayer aus.";
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
            error = $"Im Musikplayer (0x{musicPlayer:X4}) wurde keine Effekttabelle gefunden.";
            return false;
        }

        // 83 FB <imm8> = cmp bx,<Code ohne Effekt>
        int compare = image.FindCodePattern(musicPlayer, PlayerScanLength, 0x83, 0xFB);
        if (compare < 0 || !image.TryCodeByte(compare + 2, out byte plain))
        {
            error = $"Im Musikplayer (0x{musicPlayer:X4}) fehlt der Vergleich auf den Timbre-Sonderfall.";
            return false;
        }

        // 83 EB <imm8> = sub bx,<erster Tabellencode>
        int subtract = image.FindCodePattern(musicPlayer, PlayerScanLength, 0x83, 0xEB);
        if (subtract < 0 || !image.TryCodeByte(subtract + 2, out byte first))
        {
            error = $"Im Musikplayer (0x{musicPlayer:X4}) fehlt die Basis der Effekttabelle.";
            return false;
        }

        paramTable = table;
        plainCode = plain;
        firstCode = first;
        error = null;
        return true;
    }
}
