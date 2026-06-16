using Xunit;

/**
 * CW: 2.Nov.2025:
 * Disable test parallelization, as CivOne game state is static/singleton,
 * and tests may interfere with each other.
 * Otherwise tests will fail intermittently when run in parallel.
 */
[assembly: CollectionBehavior(DisableTestParallelization = true)]