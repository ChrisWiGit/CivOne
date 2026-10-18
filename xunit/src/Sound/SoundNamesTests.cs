using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CivOne.Sound;
using CivOne.Sound.Cvl;
using Xunit;

namespace CivOne.UnitTests.Sound
{
    /// <summary>
    /// Covers the rules the sound names have to keep, because both file names on disk and compiled
    /// plugin assemblies depend on them.
    /// </summary>
    public sealed class SoundNamesTests
    {
        /// <summary>Constant that is a building block rather than a sound name of its own.</summary>
        private const string SuffixConstant = nameof(SoundNames.ShortSuffix);

        private static IReadOnlyList<(string Field, string Value)> Names()
            => [.. typeof(SoundNames)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.Name != SuffixConstant)
                .Select(field => (field.Name, (string)field.GetRawConstantValue()!))];

        /// <summary>
        /// Two names with the same value would share one file on disk and one entry in a pack.
        /// </summary>
        [Fact]
        public void EveryNameIsUnique()
        {
            var duplicates = Names()
                .GroupBy(name => name.Value, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            Assert.Empty(duplicates);
        }

        /// <summary>
        /// A name is used as a file name unchanged, so it may only contain characters that are safe
        /// on every file system.
        /// </summary>
        [Fact]
        public void EveryNameIsUsableAsAFileName()
        {
            foreach ((string field, string value) in Names())
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"{field} is empty.");
                Assert.True(
                    value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_'),
                    $"{field} = '{value}' contains something other than a-z, 0-9 and '_'.");
            }
        }

        /// <summary>
        /// Call sites build the short jingle's name by appending the suffix to a civilization's
        /// tune, so every short name has to be exactly its long counterpart plus that suffix.
        /// </summary>
        [Fact]
        public void EveryShortLeaderNameIsItsLongNamePlusTheSuffix()
        {
            var byField = Names().ToDictionary(name => name.Field, name => name.Value, StringComparer.Ordinal);

            foreach ((string field, string value) in Names().Where(name => name.Field.EndsWith("Short", StringComparison.Ordinal)))
            {
                string longField = field[..^"Short".Length];

                Assert.True(byField.ContainsKey(longField), $"{field} has no long counterpart {longField}.");
                Assert.Equal(byField[longField] + SoundNames.ShortSuffix, value);
            }
        }

        /// <summary>
        /// The catalog is the only place that turns a tune number into a name, so it must not
        /// invent names of its own.
        /// </summary>
        [Fact]
        public void TheCatalogOnlyUsesDeclaredNames()
        {
            var declared = Names().Select(name => name.Value).ToHashSet(StringComparer.Ordinal);

            foreach (CvlTuneDefinition tune in CvlTuneCatalog.Tunes)
            {
                Assert.True(declared.Contains(tune.Name), $"Tune {tune.TuneId} uses undeclared name '{tune.Name}'.");
            }
        }

        /// <summary>
        /// Every declared name should be reachable, so a name nothing can ever play does not sit
        /// around looking supported.
        /// </summary>
        [Fact]
        public void EveryDeclaredNameHasATune()
        {
            var known = CvlTuneCatalog.Tunes.Select(tune => tune.Name).ToHashSet(StringComparer.Ordinal);

            foreach ((string field, string value) in Names())
            {
                Assert.True(known.Contains(value), $"{field} = '{value}' has no tune in the catalog.");
            }
        }
    }
}
