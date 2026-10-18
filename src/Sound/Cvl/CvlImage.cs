using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CivOne.Sound.Cvl;



/// <summary>
/// A loaded CVL overlay (MicroProse "Civilization overlay", a DOS MZ executable
/// with an extra module header at the start of the load image).
///
/// File layout:
/// <code>
///   0x00            MZ header, 0x08 = header size in paragraphs -> start of the load image
///   image + 0x10    20-byte ASCII signature, e.g. "Civil IBM   11-14-91"
///   image + 0x28    code segment  (0 for every known module)
///   image + 0x2A    data segment in paragraphs, relative to the start of the image
///   image + 0x30    export count (11 for every known module)
///   image + 0x32    export table: word offsets in the code segment
/// </code>
///
/// Both segment words are stored unrelocated in the file (they are targets of the
/// MZ relocation table); the DOS loader adds the load segment to them there. Since the
/// image itself is the base here, the raw values can be used directly as paragraph
/// offsets from <see cref="ImageStart"/>.
///
/// Important: pointers in the code (e.g. <c>lea bx,[0x0144]</c>) are <em>data-segment</em>-relative
/// and must be resolved via <see cref="DataStart"/>, not via <see cref="ImageStart"/>.
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

    /// <summary>Index of the exports used by CIVPLAY within <see cref="Exports"/>.</summary>
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

    /// <summary>File offset of the load image (MZ header size in paragraphs * 16).</summary>
    public int ImageStart { get; }

    /// <summary>ASCII signature including build date, e.g. "Civil IBM   11-14-91".</summary>
    public string Signature { get; }

    public ushort CodeSegment { get; }
    public ushort DataSegment { get; }

    /// <summary>File offset from which code offsets are counted.</summary>
    public int CodeStart => ImageStart + CodeSegment * 16;

    /// <summary>File offset from which data-segment offsets are counted.</summary>
    public int DataStart => ImageStart + DataSegment * 16;

    /// <summary>Size of the code segment in bytes, i.e. everything up to the data segment.</summary>
    public int CodeLength => Math.Max(0, Math.Min(DataStart, Bytes.Length) - CodeStart);

    public IReadOnlyList<ushort> Exports { get; }

    public static CvlImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is missing.", nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("CVL file not found.", path);
        return FromBytes(File.ReadAllBytes(path), Path.GetFullPath(path));
    }

    public static CvlImage FromBytes(byte[] bytes, string? filePath = null)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        string where = filePath ?? "<memory>";
        if (bytes.Length < 0x40)
            throw new InvalidOperationException($"{where}: file is too small for an MZ header.");
        if (ReadU16(bytes, 0, where) != MzSignature)
            throw new InvalidOperationException($"{where}: no MZ signature, this is not a CVL file.");

        int imageStart = ReadU16(bytes, HeaderParagraphsField, where) * 16;
        if (imageStart <= 0 || imageStart + ExportTableField + 2 > bytes.Length)
            throw new InvalidOperationException($"{where}: load image is outside the file.");

        ushort codeSegment = ReadU16(bytes, imageStart + CodeSegmentField, where);
        ushort dataSegment = ReadU16(bytes, imageStart + DataSegmentField, where);

        int dataStart = imageStart + dataSegment * 16;
        if (dataStart < 0 || dataStart >= bytes.Length)
            throw new InvalidOperationException($"{where}: data segment 0x{dataSegment:X4} is outside the file.");

        int exportCount = ReadU16(bytes, imageStart + ExportCountField, where);
        if (exportCount is <= 0 or > MaxExportCount)
            throw new InvalidOperationException($"{where}: implausible export count {exportCount}.");

        int exportTable = imageStart + ExportTableField;
        if (exportTable + exportCount * 2 > bytes.Length)
            throw new InvalidOperationException($"{where}: export table is outside the file.");

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
            : throw new InvalidOperationException($"Code offset 0x{offset:X4} is outside the file.");

    public ushort CodeWord(int offset)
        => TryCodeWord(offset, out ushort value)
            ? value
            : throw new InvalidOperationException($"Code offset 0x{offset:X4} is outside the file.");

    /// <summary>
    /// Checks an opcode pattern in the code segment. A <c>-1</c> in <paramref name="pattern"/> is a wildcard.
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
    /// Searches for the first occurrence of an opcode pattern in the code segment within
    /// <paramref name="length"/> bytes starting at <paramref name="offset"/>. <c>-1</c> is a wildcard.
    /// Returns the code offset, or -1.
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
            throw new InvalidOperationException($"{where}: offset 0x{offset:X4} is outside the file.");
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
