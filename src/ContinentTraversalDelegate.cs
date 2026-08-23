// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System;
using System.Collections.Generic;

namespace CivOne
{
    internal sealed class ContinentTraversalDelegate
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int[,] _relativePositions;
        private readonly Func<int, int, bool> _isOceanAt;
        private readonly Func<int, int, int> _getContinentIdAt;
        private readonly Action<int, int, int> _setContinentIdAt;

        internal ContinentTraversalDelegate(
            int width,
            int height,
            int[,] relativePositions,
            Func<int, int, bool> isOceanAt,
            Func<int, int, int> getContinentIdAt,
            Action<int, int, int> setContinentIdAt)
        {
            _width = width;
            _height = height;
            _relativePositions = relativePositions;
            _isOceanAt = isOceanAt;
            _getContinentIdAt = getContinentIdAt;
            _setContinentIdAt = setContinentIdAt;
        }

        internal ulong CountContinent(int x, int y, bool ocean, int continentId)
        {
            if (_height <= 0 || _width <= 0)
            {
                return 0;
            }

            x = WrapX(x);
            if (y < 0 || y >= _height)
            {
                return 0;
            }

            if (_isOceanAt(x, y) != ocean || _getContinentIdAt(x, y) > 0)
            {
                return 0;
            }

            ulong continentSize = 1;
            Queue<(int X, int Y)> queue = new Queue<(int X, int Y)>();
            _setContinentIdAt(x, y, continentId);
            queue.Enqueue((x, y));

            while (queue.Count > 0)
            {
                (int cx, int cy) = queue.Dequeue();
                for (int i = 0; i < _relativePositions.GetLength(0); i++)
                {
                    int nx = WrapX(cx + _relativePositions[i, 0]);
                    int ny = cy + _relativePositions[i, 1];

                    if (ny < 0 || ny >= _height)
                    {
                        continue;
                    }

                    if (_isOceanAt(nx, ny) != ocean)
                    {
                        continue;
                    }

                    if (_getContinentIdAt(nx, ny) > 0)
                    {
                        continue;
                    }

                    _setContinentIdAt(nx, ny, continentId);
                    continentSize++;
                    queue.Enqueue((nx, ny));
                }
            }

            return continentSize;
        }

        private int WrapX(int x)
        {
            if (x < 0)
            {
                return x + _width;
            }

            if (x >= _width)
            {
                return x - _width;
            }

            return x;
        }
    }
}
