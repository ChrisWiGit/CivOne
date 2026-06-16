// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.

//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System;
using CivOne;
using CivOne.Units;
using System.Collections.ObjectModel;
using CivOne.Enums;
using CivOne.Tiles;
using CivOne.Wonders;
using System.Linq;

// An algorithm using AStar for finding best route from start to finish when using "goto" command in CivOne.
// "Stolen" from https://stackoverflow.com/questions/38387646/concise-universal-a-search-in-c-sharp by kaalus
// and modified to my taste
namespace CivOne
{

#pragma warning disable CA1822
    public sealed class AStar
    {
        public struct SPosition { 
            public int iX { get; set; }
            public int iY { get; set; }
        };
        private SPosition _GoalPosition;
        private IUnit _unit = null!;
        static Map Map => Map.Instance;
        private static readonly Node[,] _nodes = new Node[Map.WIDTH, Map.HEIGHT];
        public class Node
        {
            public SPosition Position { get; set; }
            public SPosition PreviousPosition { get; set; }
            public float F { get; set; }                  // F = G + H
            public float G { get; set; }                  // Cost from start to current node
            public float H { get; set; }                  // Heuristic cost from current node to goal
            public bool IsClosed { get; set; }
            public int Steps { get; set; }                  // number of steps from StartPosition
        }

        private int m_nodesCacheIndex;
        private readonly List<Node> m_nodesCache = [];
        private readonly List<Node> m_openHeap = [];
        private readonly List<Node> m_neighbors = [];
        private readonly ICollection<SPosition> path = [];

        // User must override Neighbors, Cost and Heuristic functions to define search domain.  Well.... I did replaced them
        // It is optional to override StorageClear, StorageGet and StorageAdd functions. 
        // Default implementation of these storage functions uses a Dictionary<T, object>, this works for all possible search domains. Don't like <T>  removed
        // A domain-specific storage algorihm may be significantly faster.
        // For example if searching on a finite 2D or 3D grid, storage using fixed array with each element representing one world point benchmarks an order of magnitude faster.


        /*  ******************************************************************************************************** */
        /// Clear A* storage.
        /// This will be called every time before a search starts and before any other user functions are called.
        /// Optional override when using domain-optimized storage.
        /// </summary>
        void StorageClear()
        {
            Array.Clear(_nodes, 0, _nodes.Length);
        }

        /*  ******************************************************************************************************** */
        private void BuildPathFromEndNode(ICollection<SPosition> path, Node startNode, Node endNode)
        {
            for (Node node = endNode; node != startNode; node = (Node)StorageGet(node.PreviousPosition))
            {
                path.Add(node.Position);
            }
        }

        /*  ******************************************************************************************************** */
        private void HeapEnqueue(Node node)
        {
            m_openHeap.Add(node);
            HeapifyFromPosToStart(m_openHeap.Count - 1);
        }

        /*  ******************************************************************************************************** */

        // Return node at pos 0 in open heap
        private Node HeapDequeue()
        {
            Node result = m_openHeap[0];
            if (m_openHeap.Count <= 1)
            {
                m_openHeap.Clear();
            }
            else
            {
                m_openHeap[0] = m_openHeap[m_openHeap.Count - 1];
                m_openHeap.RemoveAt(m_openHeap.Count - 1);
                HeapifyFromPosToEnd(0);
            }
            return result;
        }

        /*  ******************************************************************************************************** */

        private void HeapUpdate(Node node)
        {
            int pos = -1;
            for (int i = 0; i < m_openHeap.Count; ++i)
            {
                if (m_openHeap[i] == node)
                {
                    pos = i;
                    break;
                }
            }
            HeapifyFromPosToStart(pos);
        }

        /*  ******************************************************************************************************** */
        // Locate the the open node with the lowest F ( heuristics + cost ) by moving it to position 0
        private void HeapifyFromPosToEnd(int pos)
        {
            while (true)
            {
                int smallest = pos;
                int left = 2 * pos + 1;
                int right = 2 * pos + 2;
                if (left < m_openHeap.Count && m_openHeap[left].F < m_openHeap[smallest].F)
                    smallest = left;
                if (right < m_openHeap.Count && m_openHeap[right].F < m_openHeap[smallest].F)
                    smallest = right;
                if (smallest != pos)
                {
                    Node tmp = m_openHeap[smallest];
                    m_openHeap[smallest] = m_openHeap[pos];
                    m_openHeap[pos] = tmp;
                    pos = smallest;
                }
                else
                {
                    break;
                }
            }
        }

        /*  ******************************************************************************************************** */

        private void HeapifyFromPosToStart(int pos)
        {
            int childPos = pos;
            while (childPos > 0)
            {
                int parentPos = (childPos - 1) / 2;
                Node parentNode = m_openHeap[parentPos];
                Node childNode = m_openHeap[childPos];
                if (parentNode.F > childNode.F)
                {
                    m_openHeap[parentPos] = childNode;
                    m_openHeap[childPos] = parentNode;
                    childPos = parentPos;
                }
                else
                {
                    break;
                }
            }
        }

        /*  ******************************************************************************************************** */
        private Node NewNode(SPosition position, SPosition previousPosition, float g, float h)
        {
            while (m_nodesCacheIndex >= m_nodesCache.Count)
            {
                m_nodesCache.Add(new Node());
            }
            Node node = m_nodesCache[m_nodesCacheIndex++];
            node.Position = position;
            node.PreviousPosition = previousPosition;
            node.F = g + h;
            node.G = g;
            node.H = h;
            node.IsClosed = false;
            return node;
        }

        /*  ******************************************************************************************************** */
        /// Retrieve data from storage at given point.
        /// Optional override when using domain-optimized storage.
        /// <param name="p">Point to retrieve data at</param>
        /// <returns>Data stored for point p or null if nothing stored</returns>

        Node StorageGet(SPosition position)
        {
            return _nodes[position.iX, position.iY];
        }

        /*    virtual object StorageGet(sPosition position)
        {
            object data;
            m_defaultStorage.TryGetValue(position, out data);
            return data;
        }
        */

        /*  ******************************************************************************************************** */
        /// <summary>
        /// Add data to storage at given point.
        /// There will never be any data already stored at that point.
        /// Optional override when using domain-optimized storage.
        /// </summary>
        /// <param name="p">Point to add data at</param>
        /// <param name="data">Data to add</param>
        void StorageAdd(SPosition position, object data)
        {
            _nodes[position.iX, position.iY] = (Node)data;

            //		m_defaultStorage.Add(position, data);
        }


        /*  ******************************************************************************************************** */
        /// <summary>
        /// Find best path from start to nearest goal.
        /// Goal is any point for which Heuristic override returns 0.
        /// If maxPositionsToCheck limit is reached, best path found so far is returned.
        /// If there is no path to goal, path to a point nearest to goal is returned instead.
        /// </summary>
        /// <param name="path">Path will contain steps to reach goal from start in reverse order (first step at the end of collection)</param>
        /// <Starting point is the position of current unit>
        /// <endpoint is  _GoalPosition
        /// <param name="maxPositionsToCheck">Maximum number of positions to check</param>
        /// <returns>True when path to goal was found, false if partial path only</returns>
        bool FindPath(ICollection<SPosition> path, int maxPositionsToCheck = int.MaxValue)
        {
            // Check arguments
            ArgumentNullException.ThrowIfNull(path);

            // Reset cache and storage
            path.Clear();
            m_nodesCacheIndex = 0;
            m_openHeap.Clear();
            StorageClear();

            // Put start node
            SPosition StartPosition = new() {
                iX = _unit.X,
                iY = _unit.Y
            };
            Node startNode = NewNode(StartPosition, StartPosition, 0, 0);   // Start node point to itself
            StorageAdd(StartPosition, startNode);
            HeapEnqueue(startNode);

            // Astar loop
            Node? bestNode = null;
            int checkedPositions = 0;
            while (true)
            {
                // Get next node from heap
                Node? currentNode = m_openHeap.Count > 0 ? HeapDequeue() : null;

                // Check end conditions
                if (currentNode == null || checkedPositions >= maxPositionsToCheck)
                {
                    // No more nodes or limit reached, path not found, return path to best node if possible
                    if (bestNode != null)
                    {
                        BuildPathFromEndNode(path, startNode, bestNode);
                    }
                    return false;
                }

                else if (Heuristic(currentNode.Position) <= 0)
                {
                    // Node is goal, return path
                    BuildPathFromEndNode(path, startNode, currentNode);
                    return true;
                }

                // Remember node with best heuristic; ignore start node
                if (currentNode != startNode && (bestNode == null || currentNode.H < bestNode.H))
                {
                    bestNode = currentNode;
                }

                // Move current node from open to closed in the storage
                currentNode.IsClosed = true;
                ++checkedPositions;

                // Try all neighbors
                m_neighbors.Clear();
                Neighbors(currentNode, m_neighbors);
                for (int i = 0; i < m_neighbors.Count; ++i)
                {
                    // Get a neighbour
                    Node NeighborNode = m_neighbors[i];

                    // Check if this node is already in list(Closed)
                    Node NodeInList = (Node)StorageGet(NeighborNode.Position);

                    // If position was already analyzed, ignore step
                    if (NodeInList != null && NodeInList.IsClosed == true)      // if alredy in "closed" list
                    {
                        continue;
                    }

                    float cost = Cost(currentNode.Position, NeighborNode.Position, currentNode.Steps);

                    // If position is not passable, ignore step 
                    if (cost == float.PositiveInfinity)
                    {
                        continue;
                    }

                    // Calculate A* values
                    float g = currentNode.G + cost;
                    float h = Heuristic(NeighborNode.Position);
                    // Update or create new node at position
                    if (NodeInList != null)
                    {
                        // Update existing node if better
                        if (g < NodeInList.G)
                        {
                            NodeInList.G = g;
                            NodeInList.F = g + NodeInList.H;
                            NodeInList.PreviousPosition = currentNode.Position;
                            NodeInList.Steps = currentNode.Steps + 1;
                            HeapUpdate(NodeInList);
                        }
                    }
                    else
                    {
                        // Create new open node if not yet exists
                        Node node = NewNode(NeighborNode.Position, currentNode.Position, g, h);
                        node.Steps = currentNode.Steps + 1;
                        StorageAdd(node.Position, node);
                        HeapEnqueue(node);
                    }
                }
            }
        }

        /// <summary>
        /// Return all neighbors of the given point.
        /// </summary>
        /// <param name="CurrNode">Point to return neighbors for</param>
        /// <param name="neighbors">Empty collection to fill with neighbors</param>

		void Neighbors(Node CurrNode, List<Node> neighbors)
        {
            int[,] aiRelPos = new int[,] { { -1, -1 }, { -1, 0 }, { -1, 1 }, { 0, -1 }, { 0, 1 }, { 1, -1 }, { 1, 0 }, { 1, 1 } };

            for (uint i = 0; i < 8; i++)
            {
                SPosition CurPosition = CurrNode.Position;
                SPosition NewPosition = new();
                uint j = i  % 8;
                NewPosition.iX = CurPosition.iX + aiRelPos[j, 0];
                NewPosition.iX = (NewPosition.iX + Map.WIDTH) % Map.WIDTH;
                NewPosition.iY = CurPosition.iY + aiRelPos[j, 1];
                if (NewPosition.iY < 0 || NewPosition.iY >= Map.HEIGHT) continue;
				Node NewNode = new()
				{
					Position = NewPosition,
					IsClosed = false,
					PreviousPosition = CurPosition
				};
				neighbors.Add(NewNode);
            }
        }

        /*  ******************************************************************************************************** */
        float GetMoveCost(int x, int y)
        {

            float fCost = 3;        // plain etc...

            if (x < 0 || x >= Map.WIDTH) return float.PositiveInfinity;
            if (y < 0 || y >= Map.HEIGHT) return float.PositiveInfinity;

            bool road = Map[x, y].Road;
            bool railRoad = Map[x, y].RailRoad;
            if (railRoad) return 0.1f; // (maybe the cost chould be 0 )
            if (road) return 1f;

            switch (Map[x, y].Type)
            {
                case Terrain.Forest: fCost = 6; break;
                case Terrain.Swamp: fCost = 6; break;
                case Terrain.Jungle: fCost = 6; break;
                case Terrain.Hills: fCost = 6; break;
                case Terrain.Mountains: fCost = 9; break;
                case Terrain.Arctic: fCost = 6; break;
                case Terrain.Ocean: fCost = float.PositiveInfinity; break;
            }
            return fCost;
        }

        /*  ******************************************************************************************************** */
        /// <summary>
        /// Return cost of making a step from Positition to NextPosition (which are neighbors).
        /// Cost equal to float.PositiveInfinity indicates that passage from Positition to NextPosition is impossible.
        /// 
        static Game Game => Game.Instance;

        float Cost(SPosition Positition, SPosition NextPosition, int iDistance)
        {
            float _cost = 1f;

            ITile _tile = Map[NextPosition.iX, NextPosition.iY];

            // Try Avoide nmys by doing a detour if close to goal
            if (iDistance <= 9 && (_unit.UnitCategory == UnitClass.Land || _unit.UnitCategory == UnitClass.Water))
            {
                if (CheckNmeNighbors(NextPosition)) _cost = 5.0f;// increase cost if close to nmy Land/sea unit
            }

            if (_unit.UnitCategory == UnitClass.Land)
            {
                float fNextCost = GetMoveCost(NextPosition.iX, NextPosition.iY);
                float fCost = GetMoveCost(Positition.iX, Positition.iY);

                // well.... this cost handeling could be a bit smarter...............:)
                if (fNextCost == 1f && fCost == 1f) return fCost;      // if going along a road 
                if (fNextCost == 0.1f && fCost == 0.1f) return fCost;      // if going along a railroad ( maybe the cost chould be 0 )
                                                                           //            if (fNextCost == 0.0f && fCost == 0.0f) return fCost;      
                else return Math.Max(Math.Max(fNextCost, fCost), _cost);               // if moving from terrain to road/railroad   (  dont know if this is correct  )
            }

            else if (_unit.UnitCategory == UnitClass.Water)
            {
                bool IsMyCity = _tile.City != null && _tile.City.CityOwnerPlayerIndex == _unit.Owner;

                if (_tile.Type != Terrain.Ocean && !IsMyCity) return float.PositiveInfinity;

                if (_unit.Type == UnitType.Trireme)
                {

                    int iMoves = 3;
                    Player? player = Game.GetPlayer(_unit.Owner);
                    if (player != null && (player.HasWonder<MagellansExpedition>() || (!Game.WonderObsolete<Lighthouse>() && player.HasWonder<Lighthouse>()))) iMoves = 4;

                    iDistance = (iDistance % iMoves) + 1;
                    byte _MovesLeft = _unit.MovesLeft;
                    int iDistanceToLand = DistanceToLand(NextPosition.iX, NextPosition.iY, iMoves);

                    // Code to make sure Trireme dont go too far from land and still able to cross gaps
                    // and that the Trireme is at land at end of turn
                    if (iMoves >= 4 && iDistanceToLand > 3) return float.PositiveInfinity;
                    if (iMoves == 3 && iDistanceToLand > 2) return float.PositiveInfinity;
                    if (_MovesLeft == iDistance && iDistanceToLand > 1) return float.PositiveInfinity;
                    if (_tile.City != null) return _cost * 2;      // avoid citys
                    if (iDistanceToLand >= 2) return _cost - 0.1f;        // 	Go offshore if safe
                    return 1.0f;

                }
                else if (_tile.City != null) return _cost * 2;      // avoid cities, you might lose remaning steps

                return _cost;
            }
            else if (_unit.Type == UnitType.Fighter || _unit.Type == UnitType.Bomber)
            {
                if (_tile.Units != null && _tile.Units.Any(u => u.Owner != _unit.Owner))
                    return float.PositiveInfinity;    // don't attack

                if (Math.Abs(Positition.iX - NextPosition.iX) + Math.Abs(Positition.iY - NextPosition.iY) == 1)
                    _cost += 1;              // Just to make a "nice" path

                if (_tile.City != null)
                    _cost += 3;              // avoide citys, unless needed for fuel
            }
            return _cost;      // nuke only
        }

        /*  ******************************************************************************************************** */

        int DistanceToLand(int iX, int iY, int iMoves)
        {
            for (int iYY = -1; iYY <= 1; iYY++)
                for (int iXX = -1; iXX <= 1; iXX++)
                {
                    int iXXX = (iXX + iX + Map.WIDTH) % Map.WIDTH;
                    if (Map[iXXX, iY + iYY].Type != Terrain.Ocean)
                        return 1;
                }

            for (int iYY = -2; iYY <= 2; iYY++)
                for (int iXX = -2; iXX <= 2; iXX++)
                {
                    int iXXX = (iXX + iX + Map.WIDTH) % Map.WIDTH;
                    if (Map[iXXX, iY + iYY].Type != Terrain.Ocean)
                        return 2;
                }
            if (iMoves == 4)          // dont bother if "standard" Trireme
            {
                for (int iYY = -3; iYY <= 3; iYY++)
                    for (int iXX = -3; iXX <= 3; iXX++)
                    {
                        int iXXX = (iXX + iX + Map.WIDTH) % Map.WIDTH;
                        if (Map[iXXX, iY + iYY].Type != Terrain.Ocean)
                            return 3;
                    }
            }
            return 10;
        }

        /*  ******************************************************************************************************** */
        /// Return an estimate of cost of moving from Positition to goal.
        /// Return 0 when Positition is goal.
        /// This is an estimate of sum of all costs along the path between Positition and the goal.
        float Heuristic(SPosition Positition)
        {
            // maybe we should't use the same value for land unit and air/sea units
            //        float fGoalF = 3.0f;
            float fGoalF = 1.0f;

            return Distance(Positition, _GoalPosition) * fGoalF;
        }

        /*  ******************************************************************************************************** */
        private int Distance(SPosition P1, SPosition P2)
        {
            return Common.Distance(P1.iX, P1.iY, P2.iX, P2.iY);
        }

        /*  ******************************************************************************************************** */
        private int Distance(City C, SPosition P2)
        {
            return Common.Distance(C.X, C.Y, P2.iX, P2.iY);
        }

        /*  ******************************************************************************************************** */
        private int Distance(IUnit U, SPosition P2)
        {
            return Common.Distance(U.X, U.Y, P2.iX, P2.iY);
        }

        /*  ******************************************************************************************************** */
        private SPosition Position(IUnit U)
        {
            SPosition position = new()
            {
                iX = U.X,
                iY = U.Y
            };
            return position;
        }

        /*  ******************************************************************************************************** */
        private SPosition Position(City C)
        {
            SPosition position = new()
            {
                iX = C.X,
                iY = C.Y
            };
            return position;
        }

        /*  ******************************************************************************************************** */
        private bool CheckNmeNighbors(SPosition position)
        {
            int iX, iY;
            byte _owner = _unit.Owner;

            iX = position.iX;
            iY = position.iY;

            for (int iYY = -1; iYY <= 1; iYY++)
                for (int iXX = -1; iXX <= 1; iXX++)
                {
                    int iXXX = (iXX + iX + Map.WIDTH) % Map.WIDTH;

                    ITile Nighbour = Map[iXXX, iYY + iY];
                    if (Nighbour == null) continue;           // Ever happens ??
                    if (Nighbour.Units.Any(u => u.Owner != _owner))
                        return true;        // enemy close
                }
            return false;
        }

        /*  ******************************************************************************************************** */

        /// </summary>Return the next position for a unit on its way to "goto"-Goal
        /// <param name="GoalPosition"></param>
        /// <param name="unit"></param>
        /// <returns></returns>

        public SPosition FindPath(SPosition GoalPosition, IUnit unit)
        {
            ArgumentNullException.ThrowIfNull(unit);

			SPosition NoPath = new()
			{
				iX = -1
			};
			int iCount;
			SPosition[] Positions = new SPosition[200];        // For full path	
            City[] _OwnCities;

            _unit = unit;
            _GoalPosition = GoalPosition;
            SPosition _position = new();

            if (_unit.Type == UnitType.Fighter || _unit.Type == UnitType.Bomber)
            {
                // this is to ease movement of figthers and bombers by checking for refuling stations enroute to targets

                IUnit? refuelCarrier = null;
                City? refuelCity = null;
                int _fuelLeft = ((BaseUnitAir)_unit).FuelLeft;
                _position.iX = _unit.X;
                _position.iY = _unit.Y;
                int _DistanceToGoal = Distance(_GoalPosition, _position);

                // Fuel left at goal
                int _fuelLeftAtGoal = _fuelLeft - _DistanceToGoal;

                int carrierDistance = int.MaxValue;
                int cityDistance = int.MaxValue;

                // Check for carriers   todo: check if carrier is full
                IUnit[] _OwnCarriers = [.. Game.GetUnits().Where(u => u.Owner == unit.Owner && u.Type == UnitType.Carrier)];
                bool ownCarrierExists = _OwnCarriers.Length > 0;
                if (ownCarrierExists)
                {
                    IUnit? nearestCarrierToGoal = _OwnCarriers.MinBy(c => Distance(c, GoalPosition));
                    if (nearestCarrierToGoal == null)
                    {
                        return NoPath;
                    }

                    carrierDistance = Distance(nearestCarrierToGoal, GoalPosition);
                }

                _OwnCities = [.. Game.GetCities().Where(c => _unit.Owner == c.CityOwnerPlayerIndex && c.Size > 0)];
                // Just in case
                if (_OwnCities.GetLength(0) == 0)
                    return NoPath;

                // Get a fuel station close to goal
                City? nearestCityToGoal = _OwnCities.MinBy(c => Distance(c, GoalPosition));
                if (nearestCityToGoal == null)
                {
                    return NoPath;
                }

                int _NearestFuelAtGoal = Math.Min(Distance(nearestCityToGoal, _GoalPosition), carrierDistance);

                // Enought fuel at goal ?
                if (_fuelLeftAtGoal < _NearestFuelAtGoal)
                {
                    // NO ! Refueling is needed enroute.  Find the city/carrier within fuel range nearest goal
                    // Check city for refuling
                    City[] _RefuelCitys = [.. _OwnCities.Where(c => Distance(c, _position) <= _fuelLeft)];
                    if (_RefuelCitys.Length > 0)
                    {
                        refuelCity = _RefuelCitys.MinBy(c => Distance(c, _GoalPosition));
                        if (refuelCity != null)
                        {
                            cityDistance = Distance(refuelCity, _GoalPosition);
                        }
                    }

                    // Check carrier for refuling
                    if (ownCarrierExists)
                    {
                        IUnit[] _RefuelCarriers = [.. _OwnCarriers.Where(c => Distance(c, _position) <= _fuelLeft)];
                        if (_RefuelCarriers.Length > 0)
                        {
                            refuelCarrier = _RefuelCarriers.MinBy(c => Distance(c, _GoalPosition));
                            if (refuelCarrier != null)
                            {
                                carrierDistance = Distance(refuelCarrier, _GoalPosition);
                            }
                        }
                    }

                    if (refuelCity != null && cityDistance < carrierDistance)
                    {
                        // Reroute for refuling to city
                        _GoalPosition = Position(refuelCity);
                    }
                    else
                    {
                        // Reroute for refuling to carrier
                        if (refuelCarrier == null)
                            return NoPath;          // something very wrong
                        _GoalPosition = Position(refuelCarrier);
                    }
                }
            }
            if (!FindPath(path, 3000))       // Find path using AStar algorithm
            {
                return NoPath;          // unable to find path
            }
            iCount = path.Count;
            if (iCount == 0)
                return NoPath;
            path.CopyTo(Positions, 0);
            return Positions[iCount - 1];       // Get next "goto" position
        }

    }
}