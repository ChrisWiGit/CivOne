using System;
using System.IO;
using System.Text;

namespace CivOne.UnitTests.Sound.Cvl.Ibm
{
    /// <summary>
    /// Builds a synthetic CVL module that structurally replicates ISOUND.CVL: MZ header,
    /// module header with data segment, export table, dispatch table, handlers in the
    /// <c>lea bx,[ptr] / jmp player</c> pattern, both player routines, the effect parameter
    /// table, and sequence data in both record formats.
    ///
    /// This makes the parser fully testable without storing original data in the repository.
    /// The sequence contents are made up.
    /// </summary>
    internal static class FakeIsoundModule
    {
        public const int HeaderParagraphs = 4;
        public const int ImageStart = HeaderParagraphs * 16;
        public const int ImageLength = 0x480;

        public const int DataSegmentParagraphs = 0x40;
        public const int ExportCount = 11;
        public const int MaxTuneId = 0x2C;

        public const string Signature = "Civil FAKE  01-01-00";

        // Code offsets (code segment is 0, so also image offsets).
        public const int InitSound = 0x0050;
        public const int PlayTune = 0x0060;
        public const int DispatchTable = 0x0100;
        public const int MusicHandlerA = 0x0160;
        public const int MusicHandlerB = 0x0168;
        public const int EffectHandler = 0x0170;
        public const int UnsupportedHandler = 0x0178;
        public const int SilentHandler = 0x02F0;
        public const int EffectParamTable = 0x0300;
        public const int MusicPlayer = 0x0320;
        public const int EffectPlayer = 0x0360;

        public const int PlainTimbreCode = 0x7E;
        public const int FirstTimbreCode = 0x65;

        // Data-segment offsets.
        public const int MusicDataA = 0x0000;
        public const int MusicDataB = 0x0010;
        public const int EffectData = 0x0020;

        public const int TuneMusicA = 3;
        public const int TuneSilent = 4;
        public const int TuneMusicB = 5;
        public const int TuneEffect = 6;
        public const int TuneUnsupported = 7;

        /// <summary>Effect parameters for codes 0x65..0x6F, as looked up by the music player.</summary>
        public static readonly ushort[] EffectParams =
        [
            0x8D20, 0x8F20, 0x8610, 0x8306, 0x8204, 0x8FFF, 0xD204, 0xCC05, 0xDC73, 0x000F, 0xFFF1
        ];

        public static byte[] Build()
        {
            var file = new byte[ImageStart + ImageLength];

            BuildMzHeader(file);
            BuildModuleHeader(file);
            BuildCode(file);
            BuildDispatchTable(file);
            BuildHandlers(file);
            BuildData(file);

            return file;
        }

        public static string WriteToTempFile()
        {
            string path = Path.Combine(Path.GetTempPath(), $"fake-isound-{Guid.NewGuid():N}.cvl");
            File.WriteAllBytes(path, Build());
            return path;
        }

        private static void BuildMzHeader(byte[] file)
        {
            file[0] = (byte)'M';
            file[1] = (byte)'Z';
            WriteWord(file, 0x08, HeaderParagraphs);
        }

        private static void BuildModuleHeader(byte[] file)
        {
            WriteAscii(file, ImageStart + 0x10, Signature);
            WriteWord(file, ImageStart + 0x28, 0x0000);                  // Code segment
            WriteWord(file, ImageStart + 0x2A, DataSegmentParagraphs);   // Data segment
            WriteWord(file, ImageStart + 0x30, ExportCount);

            for (int i = 0; i < ExportCount; i++)
            {
                WriteWord(file, ImageStart + 0x32 + i * 2, i == 1 ? PlayTune : InitSound);
            }
        }

        private static void BuildCode(byte[] file)
        {
            // InitSound: xor ax,ax ; retf
            WriteCode(file, InitSound, 0x33, 0xC0, 0xCB);

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

            // Music player: xor ax,ax ; mov byte ptr ds:[0x90],1 ; mov bx,ds:[0x57]
            //              cmp bx,PlainTimbreCode ; je end ; cmp bx,FirstTimbreCode ; jb end
            //              sub bx,FirstTimbreCode ; shl bx,1 ; mov ax,cs:[bx+EffectParamTable] ; ret
            WriteCode(file, MusicPlayer,
                0x33, 0xC0,
                0xC6, 0x06, 0x90, 0x00, 0x01,
                0x8B, 0x1E, 0x57, 0x00,
                0x83, 0xFB, PlainTimbreCode,
                0x74, 0x0C,
                0x83, 0xFB, FirstTimbreCode,
                0x72, 0x07,
                0x83, 0xEB, FirstTimbreCode,
                0xD1, 0xE3,
                0x2E, 0x8B, 0x87, Low(EffectParamTable), High(EffectParamTable),
                // Drive the speaker - this is how CvlDeviceDetector recognizes the device.
                0xE6, 0x42,             // out 0x42,al  (divisor low)
                0x8A, 0xC4,             // mov al,ah
                0xE6, 0x42,             // out 0x42,al  (divisor high)
                0xB0, 0xB6,             // mov al,0xB6
                0xE6, 0x43,             // out 0x43,al  (PIT channel 2, mode 3)
                0xE4, 0x61,             // in al,0x61
                0x0C, 0x03,             // or al,3
                0xE6, 0x61,             // out 0x61,al  (gate on)
                0xC3);

            // Effect player: xor ax,ax ; push ax ; mov ax,ds:[0x57] ; cmp ax,[bx] ; pop ax ; ret
            WriteCode(file, EffectPlayer,
                0x33, 0xC0,
                0x50,
                0xA1, 0x57, 0x00,
                0x3B, 0x07,
                0x58,
                0xC3);

            // Handler for a deliberately silent tune.
            WriteCode(file, SilentHandler, 0xC3);

            for (int i = 0; i < EffectParams.Length; i++)
            {
                WriteWord(file, ImageStart + EffectParamTable + i * 2, EffectParams[i]);
            }
        }

        private static void BuildDispatchTable(byte[] file)
        {
            SetDispatch(file, TuneMusicA, MusicHandlerA);
            SetDispatch(file, TuneSilent, SilentHandler);
            SetDispatch(file, TuneMusicB, MusicHandlerB);
            SetDispatch(file, TuneEffect, EffectHandler);
            SetDispatch(file, TuneUnsupported, UnsupportedHandler);
        }

        private static void BuildHandlers(byte[] file)
        {
            WriteSequenceHandler(file, MusicHandlerA, MusicDataA, MusicPlayer);
            WriteSequenceHandler(file, MusicHandlerB, MusicDataB, MusicPlayer);
            WriteSequenceHandler(file, EffectHandler, EffectData, EffectPlayer);

            // lea bx,[ptr] ; mov ds:[0x5D],bx - no jmp, so no sequence (like the Stop function in the original).
            WriteCode(file, UnsupportedHandler,
                0x8D, 0x1E, Low(MusicDataA), High(MusicDataA),
                0x89, 0x1E, 0x5D, 0x00);
        }

        private static void BuildData(byte[] file)
        {
            // Music: 4-byte records {timbre, duration, divisor}, terminated by a zero word.
            WriteData(file, MusicDataA,
                0x7E, 22, 0xA8, 0x20,   // tone, 22 ticks, divisor 8360
                0x62, 2, 0x00, 0x00,    // rest, 2 ticks
                0x69, 30, 0x54, 0x10,   // tone with vibrato timbre, 30 ticks, divisor 4180
                0x00, 0x00);

            WriteData(file, MusicDataB,
                0x65, 10, 0xF8, 0x30,   // tone, 10 ticks, divisor 12536
                0x00, 0x00);

            // Effect: 10-byte records; a mask of 0 shortens the record to 6 bytes (silence).
            WriteData(file, EffectData,
                0x5E, 0x00, 0x07, 0x00, 0xFF, 0x0F, 0x98, 0x08, 0x14, 0x00,
                0x5E, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x63, 0x00, 0x5A, 0x00, 0xFF, 0x0F, 0xD0, 0x07, 0x19, 0x00,
                0x00, 0x00);
        }

        private static void WriteSequenceHandler(byte[] file, int handler, int dataOffset, int player)
        {
            int relative = (player - (handler + 7)) & 0xFFFF;

            WriteCode(file, handler,
                0x8D, 0x1E, Low(dataOffset), High(dataOffset),
                0xE9, Low(relative), High(relative));
        }

        private static void SetDispatch(byte[] file, int tuneId, int handler)
            => WriteWord(file, ImageStart + DispatchTable + tuneId * 2, handler);

        private static void WriteCode(byte[] file, int codeOffset, params int[] bytes)
            => WriteBytes(file, ImageStart + codeOffset, bytes);

        private static void WriteData(byte[] file, int dataOffset, params int[] bytes)
            => WriteBytes(file, ImageStart + DataSegmentParagraphs * 16 + dataOffset, bytes);

        private static void WriteBytes(byte[] file, int offset, int[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                file[offset + i] = (byte)bytes[i];
            }
        }

        private static void WriteWord(byte[] file, int offset, int value)
        {
            file[offset] = Low(value);
            file[offset + 1] = High(value);
        }

        private static void WriteAscii(byte[] file, int offset, string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            Array.Copy(bytes, 0, file, offset, bytes.Length);
        }

        private static byte Low(int value) => (byte)(value & 0xFF);

        private static byte High(int value) => (byte)((value >> 8) & 0xFF);
    }
}
