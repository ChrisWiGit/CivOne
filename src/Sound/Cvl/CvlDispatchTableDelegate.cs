using System;
using System.Diagnostics.CodeAnalysis;

namespace CivOne.Sound.Cvl;



/// <summary>
/// Reads the tune dispatch table out of a CVL module's <c>PlayTune</c> export.
/// </summary>
/// <remarks>
/// Every known driver uses the same prologue, so this works for the PC speaker module as well as
/// for the AdLib one:
/// <code>
///   cmp bx,MaxTuneId          83 FB &lt;imm8&gt;
///   shl bx,1
///   call word ptr cs:[bx+t]   2E FF 97 &lt;disp16&gt;
/// </code>
/// </remarks>
internal sealed class CvlDispatchTableDelegate
{
    private const int ExportScanLength = 64;

    /// <summary>
    /// Locates the dispatch table and the highest tune number the driver accepts.
    /// </summary>
    /// <param name="image">The loaded CVL module.</param>
    /// <param name="dispatchTable">Code-segment offset of the table, or <c>-1</c> on failure.</param>
    /// <param name="maxTuneId">Highest addressable tune number, or <c>-1</c> on failure.</param>
    /// <param name="error">Reason why the layout was not recognized, or <c>null</c> on success.</param>
    /// <returns><c>true</c> when both values were found.</returns>
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method by design.")]
    public bool TryRead(CvlImage image, out int dispatchTable, out int maxTuneId, out string? error)
    {
        ArgumentNullException.ThrowIfNull(image);

        dispatchTable = -1;
        maxTuneId = -1;

        if (image.Exports.Count <= CvlImage.ExportPlayTune)
        {
            error = "Das Modul hat keinen PlayTune-Export.";
            return false;
        }

        int playTune = image.Exports[CvlImage.ExportPlayTune];

        int call = image.FindCodePattern(playTune, ExportScanLength, 0x2E, 0xFF, 0x97);
        if (call < 0 || !image.TryCodeWord(call + 3, out ushort table))
        {
            error = $"In PlayTune (0x{playTune:X4}) wurde kein 'call cs:[bx+disp16]' gefunden.";
            return false;
        }

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
}
