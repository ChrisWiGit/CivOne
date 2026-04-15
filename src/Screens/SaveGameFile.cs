using System;
using System.Collections.Generic;
using System.IO;

namespace CivOne.Screens
{
    internal class SaveGameFile
    {
        public bool ValidFile { get; private set; }
        public string SveFile { get; private set; }
        public string MapFile { get; private set; }
        public string CosFile { get; private set; }
        public bool IsYamlFile { get; private set; }
        public int Difficulty { get; private set; }

        public string Name { get; private set; }

        private ushort ReadUShort(BinaryReader reader, int position)
        {
            return Common.BinaryReadUShort(reader, position);
        }

        private string[] ReadStrings(BinaryReader reader, int position, int length, int itemLength)
        {
            return Common.BinaryReadStrings(reader, position, length, itemLength);
        }

        private SaveGameFile(string cosFile, string displayName)
        {
            ValidFile = true;
            IsYamlFile = true;
            CosFile = cosFile;
            Name = displayName;
            SveFile = string.Empty;
            MapFile = string.Empty;
        }

        private static SaveGameFile FromCosFile(string cosFile) =>
            new SaveGameFile(cosFile, Path.GetFileNameWithoutExtension(cosFile));

        public SaveGameFile(string filename)
        {
            ValidFile = false;
            Name = "(EMPTY)";
            CosFile = string.Empty;
            IsYamlFile = false;
            SveFile = string.Format("{0}.SVE", filename);
            MapFile = string.Format("{0}.MAP", filename);
            if (!File.Exists(SveFile) || !File.Exists(MapFile)) return;

            try
            {
                using (FileStream fs = new(SveFile, FileMode.Open))
                using (BinaryReader br = new(fs))
                {
                    if (fs.Length != 37856)
                    {
                        Name = "(INCORRECT FILE SIZE)";
                        return;
                    }

                    string turn = Common.YearString(ReadUShort(br, 0));
                    ushort humanPlayer = ReadUShort(br, 2);
                    ushort difficultyLevel = ReadUShort(br, 10);
                    string leaderName = ReadStrings(br, 16, 112, 14)[humanPlayer];
                    string civName = ReadStrings(br, 128, 96, 12)[humanPlayer];
                    string tribeName = ReadStrings(br, 224, 88, 11)[humanPlayer];
                    string title = Common.DifficultyName(difficultyLevel);

                    Name = string.Format("{0} {1}, {2}/{3}", title, leaderName, civName, turn);
                    Difficulty = (int)difficultyLevel;
                }
                ValidFile = true;
            }
            catch (Exception ex)
            {
                BaseInstance.Log($"Could not open .SVE file: {ex.InnerException}");
                Name = "(COULD NOT READ SAVE FILE HEADER)";
            }
        }

        public static IEnumerable<SaveGameFile> GetSaveGames(char driveLetter)
        {
            string path = Path.Combine(Settings.Instance.SavesDirectory, char.ToLower(driveLetter).ToString());
            for (int i = 0; i < 10; i++)
            {
                string filename = Path.Combine(path, string.Format("CIVIL{0}", i));
                yield return new SaveGameFile(filename);
            }
        }

    }
}