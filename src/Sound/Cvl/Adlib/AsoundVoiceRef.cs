namespace CivOne.Sound.Cvl.Adlib;

/// <summary>One voice of an arrangement: which OPL channel plays which stream.</summary>
/// <param name="Channel">OPL channel index, 0..8.</param>
/// <param name="DataOffset">Data-segment offset of the voice stream.</param>
internal readonly record struct AsoundVoiceRef(int Channel, int DataOffset);
