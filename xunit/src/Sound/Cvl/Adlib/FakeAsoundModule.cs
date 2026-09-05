using System;
using System.IO;
using System.Text;

namespace CivOne.UnitTests.Sound.Cvl.Adlib
{
    /// <summary>
    /// Builds a synthetic CVL module with the structure of ASOUND.CVL: MZ header, module header
    /// with a data segment, export table, dispatch table, the nine voice thunks, handlers in the
    /// <c>lea cx,[stream] / call thunk</c> shape, an arrangement selector, the code the parser
    /// reads the instrument bank and the chip tables from, and a few voice streams.
    ///
    /// This keeps the parser and the player fully testable without putting original game data in
    /// the repository. The music itself is made up.
    /// </summary>
    internal static class FakeAsoundModule
    {
        public const int HeaderParagraphs = 4;
        public const int ImageStart = HeaderParagraphs * 16;
        public const int ImageLength = 0x700;

        public const int DataSegmentParagraphs = 0x40;
        public const int ExportCount = 11;
        public const int MaxTuneId = 0x2C;
        public const int VoiceCount = 9;

        public const string Signature = "FAKE Adlib  01-01-00";

        // Code offsets. The code segment is zero, so these double as image offsets.
        public const int InitSound = 0x0040;
        public const int PlayTune = 0x0060;
        public const int PatchLoader = 0x0090;
        public const int NoteRoutine = 0x00E0;
        public const int DispatchTable = 0x0100;
        public const int ThunkTable = 0x0180;
        public const int ThunkStride = 7;

        /// <summary>
        /// The shared tail starts right behind the last thunk, which therefore needs no jump.
        /// </summary>
        public const int ThunkTail = ThunkTable + ((VoiceCount - 1) * ThunkStride) + 4;

        public const int PlainHandler = 0x0220;
        public const int ArrangementSelector = 0x0250;
        public const int ArrangementTable = 0x0270;
        public const int ArrangementHandler = 0x0280;
        public const int OpcodeHandler = 0x02C0;
        public const int SilentHandler = 0x02E0;
        public const int UnsupportedHandler = 0x02E8;
        public const int NoisyHandler = 0x0300;

        // Data offsets.
        public const int PlainVoiceA = 0x0000;
        public const int PlainVoiceB = 0x0020;
        public const int OpcodeVoice = 0x0040;
        public const int ArrangementVoice = 0x00C0;
        public const int InstrumentBank = 0x0100;
        public const int InstrumentStride = 44;
        public const int OperatorStride = 22;
        public const int InstrumentCount = 4;
        public const int FrequencyTable = InstrumentBank + (InstrumentCount * InstrumentStride);
        public const int DeepTremoloFlag = FrequencyTable + 24;
        public const int DeepVibratoFlag = DeepTremoloFlag + 2;
        public const int NoteSelectFlag = DeepVibratoFlag + 2;
        public const int ChannelOperatorTable = 0x01E0;
        public const int OperatorRegisterTable = 0x0200;

        public const int TunePlain = 3;
        public const int TuneArrangements = 5;
        public const int TuneOpcodes = 6;
        public const int TuneSilent = 7;
        public const int TuneUnsupported = 8;
        public const int TuneNoise = 9;

        public const int DefaultPan = 0x40;

        /// <summary>The same twelve semitone F-numbers the real driver uses.</summary>
        public static readonly ushort[] FrequencyNumbers =
            [512, 542, 575, 609, 645, 683, 724, 767, 813, 861, 912, 967];

        /// <summary>Operator register offsets, in the order the chip lays them out.</summary>
        public static readonly byte[] OperatorRegisters =
            [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D,
             0x10, 0x11, 0x12, 0x13, 0x14, 0x15];

        /// <summary>
        /// Builds the module.
        /// </summary>
        /// <returns>The file contents.</returns>
        public static byte[] Build()
        {
            var file = new byte[ImageStart + ImageLength];

            BuildMzHeader(file);
            BuildModuleHeader(file);
            BuildCode(file);
            BuildThunks(file);
            BuildDispatchTable(file);
            BuildHandlers(file);
            BuildData(file);

            return file;
        }

        /// <summary>
        /// Writes the module to a temporary file.
        /// </summary>
        /// <returns>Path of the file.</returns>
        public static string WriteToTempFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"fake-asound-{Guid.NewGuid():N}.cvl");
            File.WriteAllBytes(path, Build());
            return path;
        }

        /// <summary>Gets the code offset of a voice thunk.</summary>
        /// <param name="voice">Voice index, 0..8.</param>
        /// <returns>The code offset.</returns>
        public static int Thunk(int voice) => ThunkTable + (voice * ThunkStride);

        private static void BuildMzHeader(byte[] file)
        {
            file[0] = (byte)'M';
            file[1] = (byte)'Z';
            WriteWord(file, 0x08, HeaderParagraphs);
        }

        private static void BuildModuleHeader(byte[] file)
        {
            WriteAscii(file, ImageStart + 0x10, Signature);
            WriteWord(file, ImageStart + 0x28, 0x0000);
            WriteWord(file, ImageStart + 0x2A, DataSegmentParagraphs);
            WriteWord(file, ImageStart + 0x30, ExportCount);

            for (int index = 0; index < ExportCount; index++)
            {
                WriteWord(file, ImageStart + 0x32 + (index * 2), index == 1 ? PlayTune : InitSound);
            }
        }

        private static void BuildCode(byte[] file)
        {
            // InitSound: mov dx,0x388 ; xor ax,ax ; retf - the port write is what marks the device.
            WriteCode(file, InitSound, 0xBA, 0x88, 0x03, 0x33, 0xC0, 0xCB);

            // PlayTune: push bp ; mov bp,sp ; mov bx,[bp+6] ; cmp bx,MaxTuneId ; jg end
            //           shl bx,1 ; call word ptr cs:[bx+DispatchTable] ; pop bp ; retf
            WriteCode(file, PlayTune,
                0x55,
                0x8B, 0xEC,
                0x8B, 0x5E, 0x06,
                0x83, 0xFB, MaxTuneId,
                0x7F, 0x08,
                0xD1, 0xE3,
                0x2E, 0xFF, 0x97, Low(DispatchTable), High(DispatchTable),
                0x5D,
                0xCB);

            BuildPatchLoader(file);
            BuildNoteRoutine(file);
        }

        /// <summary>
        /// The instruction sequences the parser reads the bank address, the operator tables and the
        /// chip flags from. Only the shapes matter, not that the surrounding code would run.
        /// </summary>
        private static void BuildPatchLoader(byte[] file)
        {
            int offset = PatchLoader;

            // mov cx,stride ; imul cx ; mov cx,bank ; add cx,ax  (modulator)
            offset = WriteCode(file, offset,
                0xB9, InstrumentStride, 0x00, 0xF7, 0xE9, 0xB9, Low(InstrumentBank), High(InstrumentBank),
                0x03, 0xC8);

            // shl bx,1 ; mov al,[bx+channelTable] ; cbw ; mov bx,ax ; mov al,[bx+registerTable]
            offset = WriteCode(file, offset,
                0xD1, 0xE3,
                0x8A, 0x87, Low(ChannelOperatorTable), High(ChannelOperatorTable),
                0x98, 0x8B, 0xD8,
                0x8A, 0x87, Low(OperatorRegisterTable), High(OperatorRegisterTable));

            // The same address calculation again, this time with the second operator's offset.
            offset = WriteCode(file, offset,
                0xB9, InstrumentStride, 0x00, 0xF7, 0xE9, 0xB9, Low(InstrumentBank), High(InstrumentBank),
                0x03, 0xC8, 0x83, 0xC1, OperatorStride);

            // The three chip flags, each tested with the driver's own idiom.
            offset = WriteFlagTest(file, offset, DeepTremoloFlag, 0x80);
            offset = WriteFlagTest(file, offset, DeepVibratoFlag, 0x40);
            WriteFlagTest(file, offset, NoteSelectFlag, 0x40);
        }

        private static int WriteFlagTest(byte[] file, int offset, int address, int bit)
            => WriteCode(file, offset,
                0x8A, 0x06, Low(address), High(address),
                0x25, 0xFF, 0x00,
                0x23, 0xC0,
                0x74, 0x05,
                0xB8, bit, 0x00);

        /// <summary>
        /// The note routine, which is where the parser finds the semitone table.
        /// </summary>
        private static void BuildNoteRoutine(byte[] file)
            => WriteCode(file, NoteRoutine,
                0xD1, 0xE3,
                0x03, 0x87, Low(FrequencyTable), High(FrequencyTable),
                0xC3);

        /// <summary>
        /// The nine start thunks. Eight jump into a shared tail, the last falls through into it.
        /// </summary>
        private static void BuildThunks(byte[] file)
        {
            for (int voice = 0; voice < VoiceCount - 1; voice++)
            {
                int offset = Thunk(voice);
                int state = 0x741C + (voice * 0x1E);
                int relative = ThunkTail - (offset + 6);

                WriteCode(file, offset, 0x8D, 0x1E, Low(state), High(state), 0xEB, relative & 0xFF, 0x90);
            }

            // The last thunk sits directly in front of the tail.
            int lastState = 0x741C + ((VoiceCount - 1) * 0x1E);
            WriteCode(file, ThunkTail - 4, 0x8D, 0x1E, Low(lastState), High(lastState));

            // The tail sets the start values; the parser reads the default pan out of it.
            WriteCode(file, ThunkTail,
                0xC6, 0x47, 0x0A, 0xFF,
                0xC6, 0x47, 0x0D, DefaultPan,
                0xC6, 0x07, 0x01,
                0xC3);
        }

        private static void BuildDispatchTable(byte[] file)
        {
            for (int tuneId = 0; tuneId <= MaxTuneId; tuneId++)
            {
                WriteCodeWord(file, DispatchTable + (tuneId * 2), UnsupportedHandler);
            }

            WriteCodeWord(file, DispatchTable + (TunePlain * 2), PlainHandler);
            WriteCodeWord(file, DispatchTable + (TuneArrangements * 2), ArrangementSelector);
            WriteCodeWord(file, DispatchTable + (TuneOpcodes * 2), OpcodeHandler);
            WriteCodeWord(file, DispatchTable + (TuneSilent * 2), SilentHandler);
            WriteCodeWord(file, DispatchTable + (TuneUnsupported * 2), UnsupportedHandler);
            WriteCodeWord(file, DispatchTable + (TuneNoise * 2), NoisyHandler);
        }

        private static void BuildHandlers(byte[] file)
        {
            // Two voices: the first started with a call, the second with a tail jump.
            int offset = WriteVoiceStart(file, PlainHandler, PlainVoiceA, 0, tail: false);
            WriteVoiceStart(file, offset, PlainVoiceB, 1, tail: true);

            // mov bx,[bp+8] ; shl bx,1 ; and bx,6 ; jmp word ptr cs:[bx+table]
            WriteCode(file, ArrangementSelector,
                0x8B, 0x5E, 0x08,
                0xD1, 0xE3,
                0x83, 0xE3, 0x06,
                0x2E, 0xFF, 0xA7, Low(ArrangementTable), High(ArrangementTable));

            for (int index = 0; index < 4; index++)
            {
                int handler = ArrangementHandler + (index * 8);
                WriteCodeWord(file, ArrangementTable + (index * 2), handler);
                WriteVoiceStart(file, handler, ArrangementVoice + (index * 0x10), index, tail: true);
            }

            WriteVoiceStart(file, OpcodeHandler, OpcodeVoice, 2, tail: true);

            // retf: the driver knows the tune but plays nothing.
            WriteCode(file, SilentHandler, 0xCB);

            // A control function: no voice is started.
            WriteCode(file, UnsupportedHandler, 0x33, 0xC0, 0xC3);

            // A handler with extra code between the pairs, like the ones that randomize a byte.
            // The 0xC3 inside 'add bx,5' must not be mistaken for a return.
            int noisy = WriteCode(file, NoisyHandler, 0x8D, 0x0E, Low(PlainVoiceA), High(PlainVoiceA));
            noisy = WriteCode(file, noisy, 0x8B, 0xD9, 0x83, 0xC3, 0x05, 0x88, 0x07);
            WriteCode(file, noisy, 0xE9, 0, 0);
            WriteCodeWord(file, noisy + 1, (Thunk(0) - (noisy + 3)) & 0xFFFF);
        }

        /// <summary>
        /// Writes one <c>lea cx,[stream]</c> plus the call or jump into a voice thunk.
        /// </summary>
        /// <returns>The offset just after what was written.</returns>
        private static int WriteVoiceStart(byte[] file, int offset, int stream, int voice, bool tail)
        {
            WriteCode(file, offset, 0x8D, 0x0E, Low(stream), High(stream));

            int branch = offset + 4;
            file[ImageStart + branch] = tail ? (byte)0xE9 : (byte)0xE8;
            WriteCodeWord(file, branch + 1, (Thunk(voice) - (branch + 3)) & 0xFFFF);

            return branch + 3;
        }

        private static void BuildData(byte[] file)
        {
            int data = ImageStart + (DataSegmentParagraphs * 16);

            // A plain melody that runs to the terminator.
            WriteData(file, data + PlainVoiceA,
                0xFC, 0x00,             // instrument 0
                0xF9, 0x40,             // volume 0x40 -> 0x20
                0xFB, 0x02,             // release two ticks early
                60, 0x20,               // note, duration
                62, 0x20,
                64, 0x40,
                0x00, 0x10,             // rest
                0x00, 0x00);            // end

            WriteData(file, data + PlainVoiceB,
                0xFC, 0x01,
                0xF9, 0x30,
                48, 0x40,
                50, 0x40,
                0x00, 0x00);

            // Every control opcode, then a repeat and a restart.
            WriteData(file, data + OpcodeVoice,
                0xFC, 0x02,             // instrument
                0xF4, 0x40,             // pan centre
                0xF5, 0x10,             // volume offset
                0xF9, 0x40,             // volume
                0xF8, 0x04, 0x01,       // volume envelope, period 4, +1
                0xF3, 0x08, 0xFF,       // pan envelope, period 8, -1
                0xF7, 0x05,             // detune
                0xFB, 0x01,             // gate
                0xFA, 0x02,             // pitch slide
                0xFF, 0x00,             // mark the start of the outer block
                60, 0x08,
                62, 0x08,
                0xFE, 0x00,             // mark the start of the inner block
                64, 0x08,
                0xFE, 0x02,             // repeat the inner block twice
                0xFF, 0x03,             // repeat the outer block three times
                0xF6, 0x02, 65, 67, 0x00,  // pick one of two notes and patch the note that follows
                66, 0x08,
                0xFD);                  // restart

            for (int index = 0; index < 4; index++)
            {
                WriteData(file, data + ArrangementVoice + (index * 0x10),
                    0xFC, 0x00,
                    0xF9, 0x40,
                    (byte)(60 + index), 0x20,
                    0x00, 0x00);
            }

            BuildInstruments(file, data);
            BuildTables(file, data);
        }

        private static void BuildInstruments(byte[] file, int data)
        {
            // Three plain melodic patches and one that drives the noise generator.
            for (int index = 0; index < InstrumentCount; index++)
            {
                int start = data + InstrumentBank + (index * InstrumentStride);

                WriteOperator(file, start, attack: 10, decay: 2, level: 0x28, multiplier: 1, feedback: 3);
                WriteOperator(file, start + OperatorStride,
                    attack: 9, decay: 2, level: 0x3F, multiplier: 1, feedback: 3);

                if (index != 3) continue;

                // Noise parameters live in the tail of the first operator block.
                WriteData(file, start + 14, 0x20, 0x00);                    // duration
                WriteWord(file, start + 16, 0x00FF);                        // mask
                WriteWord(file, start + 18, 0x0200);                        // base
                WriteWord(file, start + 20, 0x0010);                        // step
            }
        }

        private static void WriteOperator(byte[] file, int start, int attack, int decay, int level,
            int multiplier, int feedback)
            => WriteData(file, start,
                (byte)attack,       // attack rate
                (byte)decay,        // decay rate
                0x08,               // sustain level
                0x03,               // release rate
                0x01,               // sustaining
                0x00,               // key scale rate
                (byte)level,        // level
                0x00,               // key scale level
                0x00,               // waveform
                (byte)multiplier,   // frequency multiplier
                (byte)feedback,     // feedback
                0x00,               // tremolo
                0x00,               // vibrato
                0x01);              // frequency modulation

        private static void BuildTables(byte[] file, int data)
        {
            for (int semitone = 0; semitone < FrequencyNumbers.Length; semitone++)
            {
                WriteWord(file, data + FrequencyTable + (semitone * 2), FrequencyNumbers[semitone]);
            }

            file[data + DeepTremoloFlag] = 1;
            file[data + DeepVibratoFlag] = 1;
            file[data + NoteSelectFlag] = 1;

            for (int channel = 0; channel < VoiceCount; channel++)
            {
                int group = channel / 3;
                int within = channel % 3;

                file[data + ChannelOperatorTable + (channel * 2)] = (byte)((group * 6) + within);
                file[data + ChannelOperatorTable + (channel * 2) + 1] = (byte)((group * 6) + within + 3);
            }

            for (int index = 0; index < OperatorRegisters.Length; index++)
            {
                file[data + OperatorRegisterTable + index] = OperatorRegisters[index];
            }
        }

        private static int WriteCode(byte[] file, int offset, params int[] bytes)
        {
            for (int index = 0; index < bytes.Length; index++)
            {
                file[ImageStart + offset + index] = (byte)bytes[index];
            }

            return offset + bytes.Length;
        }

        private static void WriteData(byte[] file, int absolute, params int[] bytes)
        {
            for (int index = 0; index < bytes.Length; index++)
            {
                file[absolute + index] = (byte)bytes[index];
            }
        }

        /// <summary>Writes a word at an absolute file offset.</summary>
        private static void WriteWord(byte[] file, int offset, int value)
        {
            file[offset] = (byte)(value & 0xFF);
            file[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        /// <summary>Writes a word at a code-segment offset.</summary>
        private static void WriteCodeWord(byte[] file, int offset, int value)
            => WriteWord(file, ImageStart + offset, value);

        private static void WriteAscii(byte[] file, int offset, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            Array.Copy(bytes, 0, file, offset, bytes.Length);
        }

        private static int Low(int value) => value & 0xFF;

        private static int High(int value) => (value >> 8) & 0xFF;
    }
}
