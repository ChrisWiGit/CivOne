using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Findet die originalen CVL-Module, falls sie lokal vorhanden sind.
    ///
    /// Die Dateien gehören dem Originalspiel und liegen deshalb bewusst nicht im Repository.
    /// Tests, die sie brauchen, sind Opt-in: fehlt die Datei, überspringen sie sich selbst.
    /// Die eigentliche Parser-Abdeckung liefert <see cref="FakeIsoundModule"/> und läuft immer.
    /// </summary>
    internal static class CvlTestFiles
    {
        public static string IsoundEnvironmentVariable => "CIVONE_ISOUND_CVL";

        public static string AsoundEnvironmentVariable => "CIVONE_ASOUND_CVL";

        public static string? TryFindIsound() => TryFind("ISOUND.CVL", IsoundEnvironmentVariable);

        public static string? TryFindAsound() => TryFind("ASOUND.CVL", AsoundEnvironmentVariable);

        public static string MissingHint(string fileName, string environmentVariable)
            => $"Übersprungen: {fileName} nicht gefunden. "
               + $"Datei nach xunit/src/Sound/Cvl/ legen oder {environmentVariable} auf den Pfad setzen.";

        private static string? TryFind(string fileName, string environmentVariable)
        {
            string? fromEnv = Environment.GetEnvironmentVariable(environmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv)) return fromEnv;

            var candidates = new List<string>();
            string? xunitRoot = FindXunitRoot();

            if (xunitRoot != null)
            {
                AddCasings(candidates, Path.Combine(xunitRoot, "src", "Sound", "Cvl"), fileName);
                AddCasings(candidates, Path.Combine(xunitRoot, "TestData", "Cvl"), fileName);

                string? repoRoot = Directory.GetParent(xunitRoot)?.FullName;
                if (!string.IsNullOrWhiteSpace(repoRoot))
                {
                    AddCasings(candidates, Path.Combine(repoRoot, "temp", "Sound"), fileName);
                    AddCasings(candidates, Path.Combine(repoRoot, "temp"), fileName);
                }
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Testet die tatsächlich auf Linux vorkommende Kleinschreibung des Dateinamens, nicht eine kulturunabhängige Normalisierung.")]
        private static void AddCasings(List<string> candidates, string folder, string fileName)
        {
            candidates.Add(Path.Combine(folder, fileName));
            candidates.Add(Path.Combine(folder, fileName.ToLowerInvariant()));
        }

        private static string? FindXunitRoot()
        {
            foreach (string start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {
                if (string.IsNullOrWhiteSpace(start)) continue;

                var dir = new DirectoryInfo(start);
                if (!dir.Exists && dir.Parent != null) dir = dir.Parent;

                while (dir != null)
                {
                    if (string.Equals(dir.Name, "xunit", StringComparison.OrdinalIgnoreCase)) return dir.FullName;
                    dir = dir.Parent;
                }
            }

            return null;
        }
    }
}
