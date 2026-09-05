using System.Collections.Generic;
using CivOne.Sound.Cvl.Adlib;

namespace CivOne.Sound.Playback.Adlib;



/// <summary>
/// The running state of one voice, mirroring the 30-byte structure the driver keeps per OPL channel.
/// </summary>
/// <remarks>
/// The driver walks its sequence with byte pointers; here the same walk happens over decoded
/// events, so the three pointers become event indices. Everything else keeps the driver's meaning
/// and its start values.
/// </remarks>
internal sealed class AdlibVoiceState
{
    /// <summary>Level a volume envelope can rise to before it stops.</summary>
    public const int MaxVolumeOffset = 0x3F;

    /// <summary>Gets or sets the OPL channel this voice plays on.</summary>
    public int Channel { get; set; }

    /// <summary>Gets or sets the events of this voice.</summary>
    public IReadOnlyList<AdlibEvent> Events { get; set; } = [];

    /// <summary>Gets or sets the ticks left on the current note; <c>0</c> means the voice is done.</summary>
    public int Duration { get; set; }

    /// <summary>Gets or sets the F-number added on every tick while a slide is running.</summary>
    public int PitchSlide { get; set; }

    /// <summary>Gets or sets the step the volume envelope adds each time it fires.</summary>
    public int VolumeDelta { get; set; }

    /// <summary>Gets or sets the step the pan envelope adds each time it fires.</summary>
    public int PanDelta { get; set; }

    /// <summary>Gets or sets the note currently sounding; <c>0</c> is a rest.</summary>
    public int Note { get; set; }

    /// <summary>Gets or sets the selected instrument.</summary>
    public int Instrument { get; set; }

    /// <summary>Gets or sets the channel volume, 0..63.</summary>
    public int Volume { get; set; }

    /// <summary>Gets or sets how many ticks before the note ends the key is released.</summary>
    public int Gate { get; set; }

    /// <summary>Gets or sets the countdown to the key release; <c>0</c> means no release pending.</summary>
    public int GateCounter { get; set; }

    /// <summary>Gets or sets the countdown to the next volume envelope step.</summary>
    public int VolumeEnvelopeCounter { get; set; }

    /// <summary>Gets or sets how many ticks lie between two volume envelope steps.</summary>
    public int VolumeEnvelopePeriod { get; set; }

    /// <summary>Gets or sets the countdown to the next pan envelope step.</summary>
    public int PanEnvelopeCounter { get; set; }

    /// <summary>Gets or sets how many ticks lie between two pan envelope steps.</summary>
    public int PanEnvelopePeriod { get; set; }

    /// <summary>Gets or sets the stereo position; <c>0x40</c> is centre. Ignored on an OPL2.</summary>
    public int Pan { get; set; }

    /// <summary>Gets or sets the index of the next event to read.</summary>
    public int Index { get; set; }

    /// <summary>Gets or sets where the outer repeat block starts.</summary>
    public int OuterLoopStart { get; set; }

    /// <summary>Gets or sets where the inner repeat block starts.</summary>
    public int InnerLoopStart { get; set; }

    /// <summary>Gets or sets how many outer repeats are left.</summary>
    public int OuterLoopCounter { get; set; }

    /// <summary>Gets or sets how many inner repeats are left.</summary>
    public int InnerLoopCounter { get; set; }

    /// <summary>Gets or sets the pitch offset in F-number units.</summary>
    public int Detune { get; set; }

    /// <summary>Gets or sets the level added on top of <see cref="Volume"/>.</summary>
    public int VolumeOffset { get; set; }

    /// <summary>
    /// Gets or sets how often this voice has rewound to the start of its sequence.
    /// A renderer uses this to stop after one full pass instead of looping forever.
    /// </summary>
    public int Restarts { get; set; }

    /// <summary>
    /// Gets or sets whether the voice has entered a repeat that is meant to run indefinitely.
    /// </summary>
    /// <remarks>
    /// A repeat count of <see cref="EndlessRepeatCount"/> is the driver's way of saying "keep
    /// vamping until the host stops me"; the title music uses it so its backing voices carry on
    /// after the melody has finished. From that point on the voice adds nothing new, so a renderer
    /// may treat it as done.
    /// </remarks>
    public bool EndlessRepeat { get; set; }

    /// <summary>Repeat count the driver uses to mean "carry on indefinitely".</summary>
    public const int EndlessRepeatCount = 255;

    /// <summary>Gets whether this voice still has something to play.</summary>
    public bool IsActive => Duration > 0;

    /// <summary>Gets whether this voice has run out of new material to play.</summary>
    public bool PassCompleted => !IsActive || Restarts > 0 || EndlessRepeat;

    /// <summary>
    /// Puts the voice into the state the driver's start routine leaves it in.
    /// </summary>
    /// <param name="channel">The OPL channel to play on.</param>
    /// <param name="events">The events to play.</param>
    /// <param name="defaultPan">The stereo position a voice starts with.</param>
    public void Start(int channel, IReadOnlyList<AdlibEvent> events, int defaultPan)
    {
        Channel = channel;
        Events = events;

        Index = 0;
        OuterLoopStart = 0;
        InnerLoopStart = 0;
        OuterLoopCounter = 0;
        InnerLoopCounter = 0;

        PitchSlide = 0;
        VolumeDelta = 0;
        PanDelta = 0;
        Note = 0;
        Volume = 0;
        Gate = 0;
        GateCounter = 0;
        VolumeEnvelopeCounter = 0;
        PanEnvelopeCounter = 0;
        Detune = 0;
        VolumeOffset = 0;

        VolumeEnvelopePeriod = 0xFF;
        Pan = defaultPan;
        Restarts = 0;
        EndlessRepeat = false;

        // The driver marks the voice as due, so the first tick immediately reads an event.
        Duration = 1;
    }

    /// <summary>
    /// Rewinds the voice to the start of its sequence and drops every modifier, as the driver's
    /// restart opcode does.
    /// </summary>
    /// <param name="defaultPan">The stereo position a voice starts with.</param>
    public void Restart(int defaultPan)
    {
        Restarts++;
        Index = 0;
        OuterLoopStart = 0;
        InnerLoopStart = 0;
        OuterLoopCounter = 0;
        InnerLoopCounter = 0;

        PitchSlide = 0;
        VolumeDelta = 0;
        PanDelta = 0;
        Volume = 0;
        Gate = 0;
        VolumeEnvelopeCounter = 0;
        PanEnvelopeCounter = 0;
        Detune = 0;
        VolumeOffset = 0;
        Pan = defaultPan;
    }

    /// <summary>Stops the voice for good.</summary>
    public void Stop() => Duration = 0;
}
