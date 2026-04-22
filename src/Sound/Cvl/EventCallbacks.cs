using System.Collections.Generic;

namespace CivOne.Sound.Cvl;

internal readonly record struct EventContext(ulong Sequence, long Tick, ulong VirtualTimeNs);

internal readonly record struct PortWriteEvent(int Port, int Value, EventContext Context);

internal interface IAudioBackend
{
    void OnWorkerCall(string kind, EventContext context);
    void OnPortWrite(int port, int value, EventContext context);
    void OnInterrupt(int intNo, EventContext context);
}

internal sealed class RecordingAudioBackend : IAudioBackend
{
    public List<PortWriteEvent> PortWrites { get; } = [];

    public void OnWorkerCall(string kind, EventContext context)
    {
    }

    public void OnPortWrite(int port, int value, EventContext context)
        => PortWrites.Add(new PortWriteEvent(port, value, context));

    public void OnInterrupt(int intNo, EventContext context)
    {
    }
}

internal sealed class AudioBackendEventSink : IEventSink
{
    private readonly IAudioBackend _backend;
    private readonly IEventSink _innerSink;

    public AudioBackendEventSink(IAudioBackend backend, IEventSink innerSink = null)
    {
        _backend = backend;
        _innerSink = innerSink;
    }

    public void OnWorkerCall(string kind, EventContext context)
    {
        _backend.OnWorkerCall(kind, context);
        _innerSink?.OnWorkerCall(kind, context);
    }

    public void OnPortWrite(int port, int value, EventContext context)
    {
        _backend.OnPortWrite(port, value, context);
        _innerSink?.OnPortWrite(port, value, context);
    }

    public void OnInterrupt(int intNo, EventContext context)
    {
        _backend.OnInterrupt(intNo, context);
        _innerSink?.OnInterrupt(intNo, context);
    }
}

internal interface IEventSink
{
    void OnWorkerCall(string kind, EventContext context);
    void OnPortWrite(int port, int value, EventContext context);
    void OnInterrupt(int intNo, EventContext context);
}

internal sealed class CollectingEventSink : IEventSink
{
    public List<string> Calls { get; } = [];

    public void OnWorkerCall(string kind, EventContext context)
        => Calls.Add($"{context.Sequence}:{context.Tick}:worker:{kind}");

    public void OnPortWrite(int port, int value, EventContext context)
        => Calls.Add($"{context.Sequence}:{context.Tick}:port:{port:X3}:{value:X2}");

    public void OnInterrupt(int intNo, EventContext context)
        => Calls.Add($"{context.Sequence}:{context.Tick}:int:{intNo:X2}");
}


