using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CivOne.UnitTests.Sound.Cvl.Ibm;

namespace CivOne.UnitTests.Sound.Cvl
{
    /// <summary>
    /// Finds the original CVL modules when they are present locally.
    ///
    /// The files belong to the original game and are deliberately kept out of the repository.
    /// Tests that need them are opt-in: when the file is missing, they skip themselves.
    /// The actual parser coverage comes from <see cref="FakeIsoundModule"/> and always runs.
    /// </summary>
    internal static class CvlTestFiles
    {
        /// <summary>Environment variable that names the path to ISOUND.CVL.</summary>
        public static string IsoundEnvironmentVariable => "CIVONE_ISOUND_CVL";

        /// <summary>Environment variable that names the path to ASOUND.CVL.</summary>
        public static string AsoundEnvironmentVariable => "CIVONE_ASOUND_CVL";

        /// <summary>
        /// Looks for the PC speaker driver ISOUND.CVL.
        /// </summary>
        /// <returns>The path to the file, or <c>null</c> when it is not available.</returns>
        public static string? TryFindIsound() => TryFind("ISOUND.CVL", IsoundEnvironmentVariable);

        /// <summary>
        /// Looks for the AdLib driver ASOUND.CVL.
        /// </summary>
        /// <returns>The path to the file, or <c>null</c> when it is not available.</returns>
        public static string? TryFindAsound() => TryFind("ASOUND.CVL", AsoundEnvironmentVariable);

        /// <summary>
        /// Builds the message a test writes to its output when it skips itself.
        /// </summary>
        /// <param name="fileName">Name of the missing file, for example <c>ASOUND.CVL</c>.</param>
        /// <param name="environmentVariable">Environment variable that can point at the file.</param>
        /// <returns>A hint that names both places the file can be put.</returns>
        public static string MissingHint(string fileName, string environmentVariable)
            => $"Skipped: {fileName} not found. "
               + $"Put the file into xunit/src/Sound/Cvl/ or set {environmentVariable} to its path.";

        /// <summary>
        /// Searches the environment variable first, then the known folders inside the repository.
        /// </summary>
        /// <param name="fileName">Name of the file to look for.</param>
        /// <param name="environmentVariable">Environment variable that can point at the file.</param>
        /// <returns>The first path that exists, or <c>null</c> when none does.</returns>
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

        /// <summary>
        /// Adds both the upper and the lower case spelling of the file name for one folder.
        /// </summary>
        /// <param name="candidates">List the paths are appended to.</param>
        /// <param name="folder">Folder to look in.</param>
        /// <param name="fileName">Name of the file in its original upper case spelling.</param>
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Covers the lower case spelling of the file name that actually occurs on Linux, rather than normalizing for culture independence.")]
        private static void AddCasings(List<string> candidates, string folder, string fileName)
        {
            candidates.Add(Path.Combine(folder, fileName));
            candidates.Add(Path.Combine(folder, fileName.ToLowerInvariant()));
        }

        /// <summary>
        /// Walks up from the test binary and the working directory to find the <c>xunit</c> folder.
        /// </summary>
        /// <returns>The full path of the folder, or <c>null</c> when it is not on either path.</returns>
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
