namespace CivOne.Persistence.Model
{
    using System;

    public class Bool2dMap : Map2d<bool>
    {
        public Bool2dMap() : base()
        {
        }

        public Bool2dMap(int width, int height) : base(width, height)
        {
        }

        public Bool2dMap(bool[,] ownsData) : base(ownsData)
        {
        }

        public Bool2dMap(bool[][] ownsData) : base(ownsData)
        {
        }

        public Bool2dMap(string[] rows)
            : base(FromRows(rows))
        {
        }

        public Bool2dMap(Bool2dMap other) : base((Map2d<bool>)other)
        {
        }

        public static implicit operator Bool2dMap(bool[,] data) => new(data);
        public static implicit operator bool[,](Bool2dMap map) => map.Data;

        /// <summary>
        /// Converts a 2D boolean array to a Bool2dMap, and vice versa.
        /// This is an explicit version of the implicit conversions to allow for more explicit control over when conversions occur, and to avoid unintended conversions in complex expressions.
        /// </summary>
        /// <param name="data">The 2D boolean array to convert.</param>
        /// <returns>A Bool2dMap instance corresponding to the 2D boolean array.</returns>
        public static Bool2dMap ToBool2dMap(bool[,] data) => new(data);
        /// <summary>
        /// Converts a Bool2dMap to a 2D boolean array.
        /// This is an explicit version of the implicit conversions to allow for more explicit control over when conversions occur, and to avoid unintended conversions in complex expressions.
        /// </summary>
        /// <param name="map">The Bool2dMap instance to convert.</param>
        /// <returns>A 2D boolean array corresponding to the Bool2dMap instance.</returns>
        public static bool[,] FromBool2dMap(Bool2dMap map) => map.Data;

        public string[] Rows
        {
            get => ToRows();
            set => Data = FromRows(value);
        }

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

        private static bool[,] FromRows(string[] rows)
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