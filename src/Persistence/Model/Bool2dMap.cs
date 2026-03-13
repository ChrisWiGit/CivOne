namespace CivOne.Persistence.Model
{
    using System;
    using YamlDotNet.Serialization;

    public class Bool2dMap
    {
        public Bool2dMap()
        {
            Data = new bool[0, 0];
        }

        public Bool2dMap(int width, int height)
        {
            Data = new bool[width, height];
            Array.Clear(Data, 0, Data.Length);
        }

        public Bool2dMap(bool[,] ownsData)
        {
            Data = ownsData;
        }
        public Bool2dMap(bool[][] ownsData)
        {
            int height = ownsData.Length;
            int width = ownsData[0].Length;

            Data = new bool[width, height];

            Array.Clear(Data, 0, Data.Length);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    Data[x, y] = ownsData[x][y];
        }

        public Bool2dMap(string[] rows)
        {
            Data = FromRows(rows);
        }

        public Bool2dMap(Bool2dMap other)
        {
            ArgumentNullException.ThrowIfNull(other);

            int w = other.Data.GetLength(0);
            int h = other.Data.GetLength(1);

            Array.Copy(other.Data, Data = new bool[w, h], other.Data.Length);
        }

        public static implicit operator Bool2dMap(bool[,] data) => new(data);
        public static implicit operator bool[,](Bool2dMap map) => map.Data;

        public (int x, int y) Size() => (Width(), Height());

        public int Width() => Data.GetLength(0);
        public int Height() => Data.GetLength(1);

        public bool this[int x, int y]
        {
            get => Data[x, y];
            set => Data[x, y] = value;
        }
        
        public bool[] this[int y]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height(), nameof(y));
                
                var row = new bool[Width()];
                for (int x = 0; x < Width(); x++)
                    row[x] = Data[x, y];
                return row;
            }
            set
            {
				ArgumentNullException.ThrowIfNull(value);
                ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height(), nameof(y));
                for (int x = 0; x < Width(); x++)
                    Data[x, y] = value[x];
            }
        }

        protected bool[,] Data { get; set; }

        public string[] Rows
        {
            get => ToRows();
            set => Data = FromRows(value);
        }

        public bool[,] ToArray() => Data;

        private string[] ToRows()
        {
            var result = new string[Height()];

            for (int y = 0; y < Height(); y++)
            {
                char[] row = new char[Width()];

                for (int x = 0; x < Width(); x++)
                    row[x] = Data[x, y] ? '1' : '0';

                result[y] = new string(row);
            }

            return result;
        }

        private bool[,] FromRows(string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;

            var result = new bool[width, height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    result[x, y] = rows[y][x] == '1';

            return result;
        }
    }
}