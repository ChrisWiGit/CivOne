using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CivOne.Sound.Playback;

/// <summary>
/// Renders a sound pack's tunes on the thread pool and remembers what has been asked for.
/// </summary>
/// <remarks>
/// <para>
/// A single tune takes between a few milliseconds and about three seconds to render, which is far
/// too long to do while the game waits for a sound. Nothing here ever runs on the game thread.
/// </para>
/// <para>
/// The first use of a pack also starts a background warm-up of the whole pack, so by the time the
/// game asks for a tune its wave file usually exists already. Only the first arrangement of each
/// tune is warmed up; the alternative arrangements of the leader themes would quadruple both the
/// work and the disk space, and rendering one on demand no longer blocks anything.
/// </para>
/// </remarks>
internal sealed class SoundPackRenderQueue : ISoundPackRenderQueue
{
    private readonly SoundPackWaveRenderService _renderer;
    private readonly SoundPackWarmUpOrderDelegate _order = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> _renders = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _warmedPacks = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _warmUpLock = new();
    private Task _warmUpChain = Task.CompletedTask;

    /// <summary>
    /// Creates the queue.
    /// </summary>
    /// <param name="renderer">
    /// The renderer to drive, or <c>null</c> for the built-in one. It is used from several threads
    /// at once during a warm-up.
    /// </param>
    public SoundPackRenderQueue(SoundPackWaveRenderService? renderer = null)
        => _renderer = renderer ?? new SoundPackWaveRenderService();

    /// <inheritdoc/>
    public string? TryGetCached(string packFolder, string fileName, int arrangement)
        => _renderer.TryGetCached(packFolder, fileName, arrangement);

    /// <inheritdoc/>
    public Task<string?> Request(string packFolder, string fileName, int arrangement)
    {
        string key = Key(packFolder, fileName, arrangement);
        Forget(key);

        // Lazy rather than a bare Task: under contention ConcurrentDictionary may also run the
        // factory of the call that loses the race, which would start a second render of the same
        // tune writing to the same file.
        Lazy<Task<string?>> render = _renders.GetOrAdd(
            key,
            _ => new Lazy<Task<string?>>(
                () => Task.Run(() => _renderer.Render(packFolder, fileName, arrangement)),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return render.Value;
    }

    /// <inheritdoc/>
    public void WarmPack(string packFolder)
    {
        if (string.IsNullOrWhiteSpace(packFolder)) return;

        // TryAdd both marks the pack as being worked on and tells us whether somebody got here
        // first. The mark is dropped again when the warm-up ends, so a pack whose folder did not
        // exist yet - or whose cache has been deleted since - is picked up on the next attempt.
        if (!_warmedPacks.TryAdd(packFolder, 0)) return;

        lock (_warmUpLock)
        {
            // Packs are warmed one after another. Two of them running at once would ask for twice
            // the cores this is allowed to use, which is what the limit below is there to avoid.
            _warmUpChain = _warmUpChain
                .ContinueWith(_ => WarmAsync(packFolder), CancellationToken.None,
                    TaskContinuationOptions.None, TaskScheduler.Default)
                .Unwrap();
        }
    }

    /// <summary>
    /// Drops a remembered render whose wave file is no longer on disk, so the next request renders
    /// it again instead of handing out a path that points at nothing.
    /// </summary>
    /// <remarks>
    /// Renders that failed are kept: they would only fail again, and retrying one on every request
    /// would cost seconds each time.
    /// </remarks>
    private void Forget(string key)
    {
        if (!_renders.TryGetValue(key, out Lazy<Task<string?>>? render)) return;
        if (!render.IsValueCreated) return;

        Task<string?> task = render.Value;
        if (task.Status != TaskStatus.RanToCompletion) return;
        if (task.Result == null || File.Exists(task.Result)) return;

        // The pair overload removes only while the value is still this very render, so a render
        // that another thread has started in the meantime is left alone.
        _renders.TryRemove(new KeyValuePair<string, Lazy<Task<string?>>>(key, render));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The warm-up is a background convenience; a failure in it must not surface as an unobserved task exception.")]
    private async Task WarmAsync(string packFolder)
    {
        try
        {
            IReadOnlyList<string> files = _order.Order(packFolder);
            if (files.Count == 0) return;

            var options = new ParallelOptions
            {
                // Leave one core to the game itself, so warming up in the background never costs
                // frame rate.
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
            };

            await Parallel.ForEachAsync(files, options,
                async (file, _) => await Request(packFolder, file, 0).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // A pack that cannot be warmed up is still played, one tune at a time, on demand.
        }
        finally
        {
            // Releasing the mark is what makes a second attempt possible. It matters when the pack
            // did not exist yet at the first attempt - at game start the sound data may still be
            // waiting to be converted - and when its cache has been removed since.
            _warmedPacks.TryRemove(packFolder, out _);
        }
    }

    private static string Key(string packFolder, string fileName, int arrangement)
        => string.Create(CultureInfo.InvariantCulture, $"{packFolder}|{fileName}|{arrangement}");
}
