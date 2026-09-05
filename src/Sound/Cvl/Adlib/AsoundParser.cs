using System;
using System.Collections.Generic;
using System.Linq;

namespace CivOne.Sound.Cvl.Adlib;

/// <summary>
/// Reads the tunes and the instrument bank from ASOUND.CVL, the AdLib / Sound Blaster driver.
///
/// Layout, verified against the disassembled driver:
/// <code>
///   Export[1] (PlayTuneFn): cmp bx,MaxTuneId ; shl bx,1 ; call word ptr cs:[bx+DispatchTable]
///
///   Handler:                8D 0E &lt;ptr16&gt;   lea cx,[stream]  ; DATA-SEGMENT-relative
///                           E8/E9 &lt;rel16&gt;   call/jmp &lt;voice thunk&gt;
///                           ... repeated, one pair per voice
///
///   Leader themes:          8B 5E 08 D1 E3 83 E3 06 2E FF A7 &lt;tbl16&gt;
///                           -&gt; four arrangement handlers of the shape above
///
///   Voice thunk:            8D 1E &lt;voiceState16&gt;  EB &lt;rel8&gt;   -&gt; shared tail
///
///   Instrument:             44 bytes = 2 x 22; the first 14 bytes of a block are OPL fields,
///                           bytes 14..21 of the first block hold the noise generator parameters.
/// </code>
///
/// The voice streams themselves are decoded by <see cref="AdlibBytecodeDecoderDelegate"/>.
/// </summary>
internal sealed class AsoundParser
{
    private const int MaxHandlerBytes = 192;
    private const int SemitoneCount = 12;
    private const int OperatorFieldCount = 14;
    private const int ArrangementCount = 4;

    private readonly CvlImage _image;
    private readonly AdlibBytecodeDecoderDelegate _decoder = new();
    private readonly Dictionary<int, int> _voiceByThunk;

    private AsoundParser(CvlImage image, AsoundLayout layout)
    {
        _image = image;
        Layout = layout;

        _voiceByThunk = [];
        for (int voice = 0; voice < layout.VoiceThunks.Count; voice++)
        {
            _voiceByThunk[layout.VoiceThunks[voice]] = voice;
        }
    }

    /// <summary>Gets the discovered addresses of the module.</summary>
    public AsoundLayout Layout { get; }

    /// <summary>Gets the module this parser reads from.</summary>
    public CvlImage Image => _image;

    /// <summary>
    /// Creates a parser for <paramref name="image"/>.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <returns>The parser.</returns>
    /// <exception cref="InvalidOperationException">The module layout was not recognized.</exception>
    public static AsoundParser Create(CvlImage image)
        => TryCreate(image, out AsoundParser? parser, out string? error)
            ? parser!
            : throw new InvalidOperationException($"ASOUND layout not recognized: {error}");

    /// <summary>
    /// Tries to create a parser for <paramref name="image"/>.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <param name="parser">The parser on success, otherwise <c>null</c>.</param>
    /// <param name="error">Reason why the layout was not recognized, or <c>null</c> on success.</param>
    /// <returns><c>true</c> when the module could be interpreted.</returns>
    public static bool TryCreate(CvlImage image, out AsoundParser? parser, out string? error)
    {
        ArgumentNullException.ThrowIfNull(image);
        parser = null;

        if (!new CvlDispatchTableDelegate().TryRead(image, out int dispatchTable, out int maxTuneId, out error))
            return false;

        if (!TryFindVoiceThunks(image, out int[] thunks, out int defaultPan, out error)) return false;
        if (!TryFindInstrumentBank(image, out int bank, out int stride, out int operatorStride, out error)) return false;
        if (!TryFindOperatorTables(image, out int channelTable, out int registerTable, out error)) return false;
        if (!TryFindFrequencyTable(image, out int frequencyTable, out error)) return false;
        if (!TryFindChipFlags(image, out int tremolo, out int vibrato, out int noteSelect, out error)) return false;

        parser = new AsoundParser(image, new AsoundLayout
        {
            DispatchTable = dispatchTable,
            MaxTuneId = maxTuneId,
            VoiceThunks = thunks,
            InstrumentBank = bank,
            InstrumentStride = stride,
            OperatorStride = operatorStride,
            ChannelOperatorTable = channelTable,
            OperatorRegisterTable = registerTable,
            FrequencyNumberTable = frequencyTable,
            DeepTremoloFlag = tremolo,
            DeepVibratoFlag = vibrato,
            NoteSelectFlag = noteSelect,
            DefaultPan = defaultPan
        });

        error = null;
        return true;
    }

    /// <summary>
    /// Analyzes the handler of one tune.
    /// </summary>
    /// <param name="tuneId">Tune number, 0..<see cref="AsoundLayout.MaxTuneId"/>.</param>
    /// <returns>What the handler turned out to be.</returns>
    public AsoundTuneInfo ParseTune(int tuneId)
    {
        if (tuneId < 0 || tuneId > Layout.MaxTuneId)
        {
            return new AsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = -1,
                Diagnostic = $"TuneId {tuneId} is outside 0..{Layout.MaxTuneId}."
            };
        }

        if (!_image.TryCodeWord(Layout.DispatchTable + tuneId * 2, out ushort handler) || handler == 0)
        {
            return new AsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = "Dispatch entry is empty."
            };
        }

        if (!_image.TryCodeByte(handler, out byte opcode))
        {
            return new AsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = $"Handler 0x{handler:X4} is outside the file."
            };
        }

        // RET / RETF: the driver knows the tune but deliberately plays nothing.
        if (opcode is 0xC3 or 0xCB)
        {
            return new AsoundTuneInfo { TuneId = tuneId, Kind = TuneScoreKind.Silent, HandlerOffset = handler };
        }

        var arrangements = new List<List<AsoundVoiceRef>>();
        bool exact = true;

        foreach (int entry in ArrangementHandlers(handler))
        {
            var voices = new List<AsoundVoiceRef>();
            exact &= WalkHandler(entry, voices);
            if (voices.Count > 0) arrangements.Add(voices);
        }

        if (arrangements.Count == 0)
        {
            return new AsoundTuneInfo
            {
                TuneId = tuneId,
                Kind = TuneScoreKind.Unsupported,
                HandlerOffset = handler,
                Diagnostic = $"Handler 0x{handler:X4} starts no voice "
                             + "(likely a control function like Stop or status query)."
            };
        }

        return new AsoundTuneInfo
        {
            TuneId = tuneId,
            Kind = CvlTuneCatalog.IsNamedTune(tuneId) ? TuneScoreKind.Music : TuneScoreKind.Effect,
            HandlerOffset = handler,
            Arrangements = arrangements,
            Diagnostic = exact
                ? null
                : $"Handler 0x{handler:X4} contains extra code (e.g. a random variation) "
                  + "that is not reproduced."
        };
    }

    /// <summary>
    /// Decodes the stream of a single voice.
    /// </summary>
    /// <param name="dataOffset">Data-segment offset of the stream.</param>
    /// <returns>The decoded events.</returns>
    public List<AdlibEvent> DecodeVoice(int dataOffset) => _decoder.Decode(_image, dataOffset);

    /// <summary>
    /// Reads the whole instrument bank.
    /// </summary>
    /// <returns>All instruments the bank has room for, in bank order.</returns>
    public List<AdlibInstrument> ReadInstruments()
    {
        var instruments = new List<AdlibInstrument>();
        int available = Layout.FrequencyNumberTable - Layout.InstrumentBank;
        int count = Layout.InstrumentStride <= 0 ? 0 : available / Layout.InstrumentStride;

        for (int index = 0; index < count; index++)
        {
            int start = Layout.InstrumentBank + index * Layout.InstrumentStride;

            instruments.Add(new AdlibInstrument
            {
                Index = index,
                Modulator = ReadOperator(start),
                Carrier = ReadOperator(start + Layout.OperatorStride),
                NoiseDuration = DataByte(start + OperatorFieldCount),
                NoiseMask = DataWord(start + OperatorFieldCount + 2),
                NoiseBase = DataWord(start + OperatorFieldCount + 4),
                NoiseStep = DataWord(start + OperatorFieldCount + 6)
            });
        }

        return instruments;
    }

    /// <summary>
    /// Reads the twelve F-numbers, one per semitone.
    /// </summary>
    /// <returns>The F-number of each semitone of an octave.</returns>
    public int[] ReadFrequencyNumbers()
    {
        var numbers = new int[SemitoneCount];
        for (int semitone = 0; semitone < SemitoneCount; semitone++)
        {
            numbers[semitone] = DataWord(Layout.FrequencyNumberTable + semitone * 2);
        }

        return numbers;
    }

    /// <summary>
    /// Reads which OPL register offsets the two operators of each channel use.
    /// </summary>
    /// <returns>Per channel the register offset of the modulator and of the carrier.</returns>
    public (int Modulator, int Carrier)[] ReadChannelOperators()
    {
        var operators = new (int, int)[Layout.VoiceCount];
        for (int channel = 0; channel < Layout.VoiceCount; channel++)
        {
            int modulator = DataByte(Layout.ChannelOperatorTable + channel * 2);
            int carrier = DataByte(Layout.ChannelOperatorTable + channel * 2 + 1);
            operators[channel] = (DataByte(Layout.OperatorRegisterTable + modulator),
                                  DataByte(Layout.OperatorRegisterTable + carrier));
        }

        return operators;
    }

    /// <summary>
    /// Reads the three chip-wide settings the driver applies whenever it loads an instrument.
    /// </summary>
    /// <returns>
    /// Whether the deeper tremolo and vibrato depths are selected, and whether the chip's keyboard
    /// split point is moved.
    /// </returns>
    public (bool DeepTremolo, bool DeepVibrato, bool NoteSelect) ReadChipFlags()
        => (DataByte(Layout.DeepTremoloFlag) != 0,
            DataByte(Layout.DeepVibratoFlag) != 0,
            DataByte(Layout.NoteSelectFlag) != 0);

    private AdlibOperator ReadOperator(int start) => new()
    {
        AttackRate = DataByte(start) & 0x0F,
        DecayRate = DataByte(start + 1) & 0x0F,
        SustainLevel = DataByte(start + 2) & 0x0F,
        ReleaseRate = DataByte(start + 3) & 0x0F,
        Sustaining = DataByte(start + 4) != 0,
        KeyScaleRate = DataByte(start + 5) != 0,
        Level = DataByte(start + 6) & 0x3F,
        KeyScaleLevel = DataByte(start + 7) & 0x03,
        Waveform = DataByte(start + 8) & 0x03,
        FrequencyMultiplier = DataByte(start + 9) & 0x0F,
        Feedback = DataByte(start + 10) & 0x07,
        Tremolo = DataByte(start + 11) != 0,
        Vibrato = DataByte(start + 12) != 0,
        FrequencyModulation = DataByte(start + 13) != 0
    };

    /// <summary>
    /// Yields the handlers to walk: the four entries of an arrangement table, or the handler itself.
    /// </summary>
    private IEnumerable<int> ArrangementHandlers(int handler)
    {
        // 8B 5E 08 D1 E3 83 E3 06 2E FF A7 <tbl16> = pick one of four arrangements by argument.
        if (_image.CodeMatches(handler, 0x8B, 0x5E, 0x08, 0xD1, 0xE3, 0x83, 0xE3, 0x06, 0x2E, 0xFF, 0xA7)
            && _image.TryCodeWord(handler + 11, out ushort table))
        {
            for (int index = 0; index < ArrangementCount; index++)
            {
                if (_image.TryCodeWord(table + index * 2, out ushort entry) && entry != 0) yield return entry;
            }

            yield break;
        }

        yield return handler;
    }

    /// <summary>
    /// Follows a handler and collects the <c>lea cx,[stream]</c> / voice-thunk pairs it executes.
    /// </summary>
    /// <remarks>
    /// A few handlers run extra code between the pairs, for example the ones that randomize a byte
    /// of their sequence. We do not decode x86 in general, so an unknown byte drops the walk out of
    /// instruction sync; from there on only the unmistakable patterns still count, and the caller
    /// is told that the handler held more than we understood.
    /// </remarks>
    /// <returns><c>true</c> when every byte on the way was understood.</returns>
    private bool WalkHandler(int handler, List<AsoundVoiceRef> voices)
    {
        bool exact = true;
        bool synchronized = true;
        int offset = handler;
        int pending = -1;
        var visited = new HashSet<int>();

        for (int step = 0; step < MaxHandlerBytes; step++)
        {
            if (!visited.Add(offset)) break;
            if (!_image.TryCodeByte(offset, out byte opcode)) break;

            // RET / RETF - but only trust it while we still know where instructions begin.
            if (synchronized && opcode is 0xC3 or 0xCB) break;

            // 8D 0E <ptr16> = lea cx,[stream]. Out of sync we only accept it right in front of a
            // branch, which is how the handlers actually use it.
            if (_image.CodeMatches(offset, 0x8D, 0x0E)
                && (synchronized || IsBranch(offset + 4))
                && _image.TryCodeWord(offset + 2, out ushort pointer))
            {
                pending = pointer;
                offset += 4;
                continue;
            }

            // EB <rel8> = jmp short, used to share code between handlers.
            if (synchronized && opcode == 0xEB && _image.TryCodeByte(offset + 1, out byte shortRelative))
            {
                offset = (offset + 2 + (sbyte)shortRelative) & 0xFFFF;
                continue;
            }

            if (opcode is 0xE8 or 0xE9 && _image.TryCodeWord(offset + 1, out ushort relative))
            {
                int target = (offset + 3 + (short)relative) & 0xFFFF;

                if (_voiceByThunk.TryGetValue(target, out int voice))
                {
                    if (pending >= 0) voices.Add(new AsoundVoiceRef(voice, pending));
                    pending = -1;
                    synchronized = true;

                    // A jump into the thunk is the tail call that ends the handler.
                    if (opcode == 0xE9) break;

                    offset += 3;
                    continue;
                }

                if (synchronized && opcode == 0xE9)
                {
                    offset = target;
                    continue;
                }

                if (synchronized)
                {
                    exact = false;
                    offset += 3;
                    continue;
                }
            }

            exact = false;
            synchronized = false;
            offset++;
        }

        return exact;
    }

    private bool IsBranch(int offset)
        => _image.TryCodeByte(offset, out byte opcode) && opcode is 0xE8 or 0xE9 or 0xEB;

    private byte DataByte(int offset) => _image.TryDataByte(offset, out byte value) ? value : (byte)0;

    private int DataWord(int offset) => _image.TryDataWord(offset, out ushort value) ? value : 0;

    /// <summary>
    /// Finds the per-voice start thunks. Every thunk is <c>lea bx,[voiceState]</c> followed by a
    /// short jump into one shared tail, and the last one falls through into that tail.
    /// </summary>
    private static bool TryFindVoiceThunks(CvlImage image, out int[] thunks, out int defaultPan, out string? error)
    {
        thunks = [];
        defaultPan = 0x40;

        var candidates = new List<(int Offset, int Target)>();

        for (int offset = 0; offset <= image.CodeLength - 6; offset++)
        {
            if (!image.CodeMatches(offset, 0x8D, 0x1E)) continue;
            if (!image.TryCodeByte(offset + 4, out byte jump) || jump != 0xEB) continue;
            if (!image.TryCodeByte(offset + 5, out byte relative)) continue;

            candidates.Add((offset, (offset + 6 + (sbyte)relative) & 0xFFFF));
        }

        if (candidates.Count == 0)
        {
            error = "No voice thunks (lea bx,[state] / jmp short) found.";
            return false;
        }

        int tail = candidates
            .GroupBy(c => c.Target)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First()
            .Key;

        var offsets = candidates.Where(c => c.Target == tail).Select(c => c.Offset).ToList();

        // The thunk of the last voice needs no jump - it sits directly in front of the shared tail.
        if (image.CodeMatches(tail - 4, 0x8D, 0x1E)) offsets.Add(tail - 4);

        if (offsets.Count < 2)
        {
            error = $"Only {offsets.Count} voice thunk(s) found, which is too few for the player.";
            return false;
        }

        offsets.Sort();
        thunks = [.. offsets];

        // C6 47 0D <imm8> = mov byte ptr [bx+0x0D],pan - the stereo position a voice starts with.
        int pan = image.FindCodePattern(tail, MaxHandlerBytes, 0xC6, 0x47, 0x0D);
        if (pan >= 0 && image.TryCodeByte(pan + 3, out byte panValue)) defaultPan = panValue;

        error = null;
        return true;
    }

    /// <summary>
    /// Finds the instrument bank via the address calculation of the patch loader:
    /// <c>mov cx,stride ; imul cx ; mov cx,bank</c>, followed by <c>add cx,operatorStride</c> for
    /// the second operator.
    /// </summary>
    private static bool TryFindInstrumentBank(CvlImage image, out int bank, out int stride,
        out int operatorStride, out string? error)
    {
        bank = -1;
        stride = -1;
        operatorStride = -1;

        int first = image.FindCodePattern(0, image.CodeLength, 0xB9, -1, 0x00, 0xF7, 0xE9, 0xB9);
        if (first < 0 || !image.TryCodeWord(first + 1, out ushort strideValue)
            || !image.TryCodeWord(first + 6, out ushort bankValue))
        {
            error = "The instrument bank was not found (no 'imul stride / mov cx,bank').";
            return false;
        }

        // The same calculation runs a second time for the carrier, with 'add cx,operatorStride'.
        int second = image.FindCodePattern(first + 8, image.CodeLength - first - 8,
            0xB9, -1, 0x00, 0xF7, 0xE9, 0xB9, -1, -1, 0x03, 0xC8, 0x83, 0xC1);
        if (second < 0 || !image.TryCodeByte(second + 12, out byte operatorValue))
        {
            error = "The size of an operator block was not found (no 'add cx,imm8').";
            return false;
        }

        bank = bankValue;
        stride = strideValue;
        operatorStride = operatorValue;

        if (stride <= 0 || operatorStride <= 0 || operatorStride * 2 > stride)
        {
            error = $"Implausible instrument size: {stride} bytes with operator block {operatorStride}.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Finds the two operator tables. The patch loader reads them back to back:
    /// <c>shl bx,1 ; mov al,[bx+channelTable] ; cbw ; mov bx,ax ; mov al,[bx+registerTable]</c>.
    /// </summary>
    private static bool TryFindOperatorTables(CvlImage image, out int channelTable, out int registerTable,
        out string? error)
    {
        channelTable = -1;
        registerTable = -1;

        int load = image.FindCodePattern(0, image.CodeLength,
            0xD1, 0xE3, 0x8A, 0x87, -1, -1, 0x98, 0x8B, 0xD8, 0x8A, 0x87);

        if (load < 0 || !image.TryCodeWord(load + 4, out ushort channels)
            || !image.TryCodeWord(load + 11, out ushort registers))
        {
            error = "The operator tables (channel -> operator -> register offset) were not found.";
            return false;
        }

        channelTable = channels;
        registerTable = registers;
        error = null;
        return true;
    }

    /// <summary>
    /// Finds the semitone table via <c>add ax,[bx+table]</c> in the routine that turns a note into
    /// an F-number.
    /// </summary>
    private static bool TryFindFrequencyTable(CvlImage image, out int frequencyTable, out string? error)
    {
        frequencyTable = -1;

        int add = image.FindCodePattern(0, image.CodeLength, 0x03, 0x87);
        if (add < 0 || !image.TryCodeWord(add + 2, out ushort table))
        {
            error = "The semitone frequency table was not found.";
            return false;
        }

        frequencyTable = table;
        error = null;
        return true;
    }

    /// <summary>
    /// Finds the three chip-wide flags. The patch loader tests each of them with the same idiom,
    /// <c>mov al,[flag] ; and ax,0FFh ; and ax,ax ; jz +n ; mov ax,bit</c>, and the two that select
    /// the same bit appear in the order deep vibrato, then note select.
    /// </summary>
    private static bool TryFindChipFlags(CvlImage image, out int deepTremolo, out int deepVibrato,
        out int noteSelect, out string? error)
    {
        deepTremolo = FindFlag(image, 0, 0x80, out _);
        deepVibrato = FindFlag(image, 0, 0x40, out int vibratoCode);
        noteSelect = vibratoCode < 0 ? -1 : FindFlag(image, vibratoCode + 1, 0x40, out _);

        if (deepTremolo < 0 || deepVibrato < 0 || noteSelect < 0)
        {
            error = "The global chip flags (tremolo/vibrato depth, note select) were not found.";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Finds the data address of a flag that the code turns into <paramref name="bit"/>.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <param name="from">Code offset to start searching at.</param>
    /// <param name="bit">The register bit the flag stands for.</param>
    /// <param name="codeOffset">Where the pattern was found, so the next search can continue past it.</param>
    /// <returns>The data offset of the flag, or <c>-1</c>.</returns>
    private static int FindFlag(CvlImage image, int from, int bit, out int codeOffset)
    {
        codeOffset = image.FindCodePattern(from, image.CodeLength - from,
            0x8A, 0x06, -1, -1, 0x25, 0xFF, 0x00, 0x23, 0xC0, 0x74, -1, 0xB8, bit, 0x00);

        return codeOffset < 0 || !image.TryCodeWord(codeOffset + 2, out ushort address) ? -1 : address;
    }
}
