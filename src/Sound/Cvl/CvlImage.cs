using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CivOne.Sound.Cvl;

#nullable enable

/// <summary>
/// Ein geladenes CVL-Overlay (MicroProse "Civilization overlay", DOS-MZ-Executable
/// mit zusätzlichem Modulkopf am Anfang des Load-Image).
///
/// Dateiaufbau:
/// <code>
///   0x00            MZ-Header, 0x08 = Headergröße in Paragraphen -> Beginn des Load-Image
///   image + 0x10    20 Byte ASCII-Signatur, z.B. "Civil IBM   11-14-91"
///   image + 0x28    Codesegment  (bei allen bekannten Modulen 0)
///   image + 0x2A    Datensegment in Paragraphen, relativ zum Image-Anfang
///   image + 0x30    Anzahl der Exporte (bei allen bekannten Modulen 11)
///   image + 0x32    Exporttabelle: Word-Offsets im Codesegment
/// </code>
///
/// Die beiden Segmentwörter stehen unrelokiert in der Datei (sie sind Ziele der
/// MZ-Relocation-Tabelle); der DOS-Loader addiert dort das Ladesegment. Da hier das
/// Image selbst die Basis ist, sind die Rohwerte direkt als Paragraphen-Offsets ab
/// <see cref="ImageStart"/> verwendbar.
///
/// Wichtig: Zeiger im Code (z.B. <c>lea bx,[0x0144]</c>) sind <em>datensegment</em>-relativ
/// und müssen über <see cref="DataStart"/> aufgelöst werden, nicht über <see cref="ImageStart"/>.
/// </summary>
internal sealed class CvlImage
{
    private const ushort MzSignature = 0x5A4D;
    private const int HeaderParagraphsField = 0x08;
    private const int SignatureField = 0x10;
    private const int SignatureLength = 20;
    private const int CodeSegmentField = 0x28;
    private const int DataSegmentField = 0x2A;
    private const int ExportCountField = 0x30;
    private const int ExportTableField = 0x32;
    private const int MaxExportCount = 64;

    /// <summary>Index der von CIVPLAY genutzten Exporte in <see cref="Exports"/>.</summary>
    public const int ExportInitSound = 0;
    public const int ExportPlayTune = 1;
    public const int ExportCloseSound = 2;
    public const int ExportSoundWorker = 3;
    public const int ExportFastSoundWorker = 4;
    public const int ExportSoundTimer = 5;

    private CvlImage(string? filePath, byte[] bytes, int imageStart, string signature,
        ushort codeSegment, ushort dataSegment, ushort[] exports)
    {
        FilePath = filePath;
        Bytes = bytes;
        ImageStart = imageStart;
        Signature = signature;
        CodeSegment = codeSegment;
        DataSegment = dataSegment;
        Exports = exports;
    }

    public string? FilePath { get; }
    public byte[] Bytes { get; }

    /// <summary>Dateioffset des Load-Image (MZ-Headergröße in Paragraphen * 16).</summary>
    public int ImageStart { get; }

    /// <summary>ASCII-Signatur inklusive Builddatum, z.B. "Civil IBM   11-14-91".</summary>
    public string Signature { get; }

    public ushort CodeSegment { get; }
    public ushort DataSegment { get; }

    /// <summary>Dateioffset, ab dem Code-Offsets zählen.</summary>
    public int CodeStart => ImageStart + CodeSegment * 16;

    /// <summary>Dateioffset, ab dem Datensegment-Offsets zählen.</summary>
    public int DataStart => ImageStart + DataSegment * 16;

    public IReadOnlyList<ushort> Exports { get; }

    public static CvlImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Pfad fehlt.", nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("CVL-Datei nicht gefunden.", path);
        return FromBytes(File.ReadAllBytes(path), Path.GetFullPath(path));
    }

    public static CvlImage FromBytes(byte[] bytes, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        string where = filePath ?? "<memory>";
        if (bytes.Length < 0x40)
            throw new InvalidOperationException($"{where}: Datei ist zu klein für einen MZ-Header.");
        if (ReadU16(bytes, 0, where) != MzSignature)
            throw new InvalidOperationException($"{where}: keine MZ-Signatur, das ist keine CVL-Datei.");

        int imageStart = ReadU16(bytes, HeaderParagraphsField, where) * 16;
        if (imageStart <= 0 || imageStart + ExportTableField + 2 > bytes.Length)
            throw new InvalidOperationException($"{where}: Load-Image liegt außerhalb der Datei.");

        ushort codeSegment = ReadU16(bytes, imageStart + CodeSegmentField, where);
        ushort dataSegment = ReadU16(bytes, imageStart + DataSegmentField, where);

        int dataStart = imageStart + dataSegment * 16;
        if (dataStart < 0 || dataStart >= bytes.Length)
            throw new InvalidOperationException($"{where}: Datensegment 0x{dataSegment:X4} liegt außerhalb der Datei.");

        int exportCount = ReadU16(bytes, imageStart + ExportCountField, where);
        if (exportCount is <= 0 or > MaxExportCount)
            throw new InvalidOperationException($"{where}: unplausible Exportanzahl {exportCount}.");

        int exportTable = imageStart + ExportTableField;
        if (exportTable + exportCount * 2 > bytes.Length)
            throw new InvalidOperationException($"{where}: Exporttabelle liegt außerhalb der Datei.");

        var exports = new ushort[exportCount];
        for (int i = 0; i < exportCount; i++)
        {
            exports[i] = ReadU16(bytes, exportTable + i * 2, where);
        }

        string signature = ReadSignature(bytes, imageStart + SignatureField);

        return new CvlImage(filePath, bytes, imageStart, signature, codeSegment, dataSegment, exports);
    }

    public bool TryCodeByte(int offset, out byte value) => TryReadByte(CodeStart + offset, out value);

    public bool TryCodeWord(int offset, out ushort value) => TryReadWord(CodeStart + offset, out value);

    public bool TryDataByte(int offset, out byte value) => TryReadByte(DataStart + offset, out value);

    public bool TryDataWord(int offset, out ushort value) => TryReadWord(DataStart + offset, out value);

    public byte CodeByte(int offset)
        => TryCodeByte(offset, out byte value)
            ? value
            : throw new InvalidOperationException($"Code-Offset 0x{offset:X4} liegt außerhalb der Datei.");

    public ushort CodeWord(int offset)
        => TryCodeWord(offset, out ushort value)
            ? value
            : throw new InvalidOperationException($"Code-Offset 0x{offset:X4} liegt außerhalb der Datei.");

    /// <summary>
    /// Prüft ein Opcode-Muster im Codesegment. <c>-1</c> in <paramref name="pattern"/> ist ein Platzhalter.
    /// </summary>
    public bool CodeMatches(int offset, params int[] pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] < 0) continue;
            if (!TryCodeByte(offset + i, out byte value) || value != pattern[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Sucht das erste Vorkommen eines Opcode-Musters im Codesegment innerhalb von
    /// <paramref name="length"/> Bytes ab <paramref name="offset"/>. <c>-1</c> ist ein Platzhalter.
    /// Liefert den Code-Offset oder -1.
    /// </summary>
    public int FindCodePattern(int offset, int length, params int[] pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        if (pattern.Length == 0) return -1;

        for (int i = 0; i <= length - pattern.Length; i++)
        {
            if (CodeMatches(offset + i, pattern)) return offset + i;
        }

        return -1;
    }

    private bool TryReadByte(int fileOffset, out byte value)
    {
        if (fileOffset < 0 || fileOffset >= Bytes.Length)
        {
            value = 0;
            return false;
        }

        value = Bytes[fileOffset];
        return true;
    }

    private bool TryReadWord(int fileOffset, out ushort value)
    {
        if (fileOffset < 0 || fileOffset + 1 >= Bytes.Length)
        {
            value = 0;
            return false;
        }

        value = (ushort)(Bytes[fileOffset] | (Bytes[fileOffset + 1] << 8));
        return true;
    }

    private static ushort ReadU16(byte[] bytes, int offset, string where)
    {
        if (offset < 0 || offset + 1 >= bytes.Length)
            throw new InvalidOperationException($"{where}: Offset 0x{offset:X4} liegt außerhalb der Datei.");
        return (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
    }

    private static string ReadSignature(byte[] bytes, int offset)
    {
        if (offset < 0 || offset >= bytes.Length) return string.Empty;

        int end = Math.Min(offset + SignatureLength, bytes.Length);
        var text = new StringBuilder(SignatureLength);
        for (int i = offset; i < end; i++)
        {
            byte b = bytes[i];
            if (b == 0) break;
            text.Append(b is >= 0x20 and < 0x7F ? (char)b : '?');
        }

        return text.ToString().TrimEnd();
    }
}
