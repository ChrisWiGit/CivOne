namespace CivOne.Persistence.Model
{
    using System;

    public class Map2d<T>
    {
        public Map2d()
        {
            Data = new T[0, 0];
        }

        public Map2d(int width, int height)
        {
            Data = new T[width, height];
            Array.Clear(Data, 0, Data.Length);
        }

        public Map2d(T[,] ownsData)
        {
            ArgumentNullException.ThrowIfNull(ownsData);
            Data = ownsData;
        }

        public Map2d(T[][] ownsData)
        {
            ArgumentNullException.ThrowIfNull(ownsData);

            int height = ownsData.Length;
            int width = height == 0 ? 0 : ownsData[0].Length;

            Data = new T[width, height];

            Array.Clear(Data, 0, Data.Length);
            for (int y = 0; y < height; y++)
            {
                ArgumentNullException.ThrowIfNull(ownsData[y]);
                for (int x = 0; x < width; x++)
                    Data[x, y] = ownsData[x][y];
            }
        }

        public Map2d(Map2d<T> other)
        {
            ArgumentNullException.ThrowIfNull(other);

            int w = other.Data.GetLength(0);
            int h = other.Data.GetLength(1);

            Array.Copy(other.Data, Data = new T[w, h], other.Data.Length);
        }

        public (int x, int y) Size() => (Width(), Height());

        public int Width() => Data.GetLength(0);
        public int Height() => Data.GetLength(1);

        public T this[int x, int y]
        {
            get => Data[x, y];
            set => Data[x, y] = value;
        }

        public T[] this[int y]
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfNegative(y, nameof(y));
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height(), nameof(y));

                var row = new T[Width()];
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

        protected T[,] Data { get; set; }

        public T[,] ToArray() => Data;
    }
}
