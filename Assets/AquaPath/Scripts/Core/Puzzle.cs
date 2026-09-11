using System;
using System.Collections.Generic;

namespace AquaPath.Core
{
    public enum Shape
    {
        Straight,
        Elbow,
        Tee,
        Cross,
        Source,
        Target
    }

    [Serializable]
    public sealed class Cell
    {
        public Shape Shape;
        public int Rotation;
        public int Solution;
        public bool Locked;

        public Cell(Shape shape, int rotation, int solution, bool locked)
        {
            Shape = shape;
            Rotation = Directions.NormalizeRotation(rotation);
            Solution = Directions.NormalizeRotation(solution);
            Locked = locked;
        }

        internal Cell Copy()
        {
            return new Cell(Shape, Rotation, Solution, Locked);
        }
    }

    public static class Directions
    {
        public const int N = 1;
        public const int E = 2;
        public const int S = 4;
        public const int W = 8;

        public static int Mask(Shape shape, int rotation)
        {
            int mask;
            switch (shape)
            {
                case Shape.Straight: mask = N | S; break;
                case Shape.Elbow: mask = N | E; break;
                case Shape.Tee: mask = N | E | W; break;
                case Shape.Cross: mask = N | E | S | W; break;
                case Shape.Source:
                case Shape.Target: mask = N; break;
                default: throw new ArgumentOutOfRangeException("shape");
            }

            int turns = NormalizeRotation(rotation);
            for (int i = 0; i < turns; i++)
                mask = ((mask << 1) & 15) | ((mask >> 3) & 1);
            return mask;
        }

        public static int NormalizeRotation(int rotation)
        {
            int normalized = rotation % 4;
            return normalized < 0 ? normalized + 4 : normalized;
        }
    }

    public sealed class NetworkTraversal
    {
        public bool[] Connected { get; private set; }
        public int[] Distance { get; private set; }
        public int[] Entry { get; private set; }
        public bool ReachesTarget { get; private set; }

        internal NetworkTraversal(bool[] connected, int[] distance, int[] entry, bool reachesTarget)
        {
            Connected = connected;
            Distance = distance;
            Entry = entry;
            ReachesTarget = reachesTarget;
        }
    }

    public sealed class Puzzle
    {
        private readonly int[] _initialRotations;

        public int Level { get; private set; }
        public int Size { get; private set; }
        public Cell[] Cells { get; private set; }
        public int Source { get; private set; }
        public int Target { get; private set; }
        public int Moves { get; private set; }
        public int IdealMoves { get; private set; }
        public int[] Route { get; private set; }

        public bool Solved
        {
            get { return Network().ReachesTarget; }
        }

        public Puzzle(int size, Cell[] cells, int source, int target, int idealMoves, int level = 0, int[] route = null)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException("size");
            if (cells == null) throw new ArgumentNullException("cells");
            if (cells.Length != size * size) throw new ArgumentException("Cell count must equal size squared.", "cells");
            if (source < 0 || source >= cells.Length) throw new ArgumentOutOfRangeException("source");
            if (target < 0 || target >= cells.Length) throw new ArgumentOutOfRangeException("target");
            if (idealMoves < 0) throw new ArgumentOutOfRangeException("idealMoves");

            Size = size;
            Source = source;
            Target = target;
            IdealMoves = idealMoves;
            Level = level;
            Cells = new Cell[cells.Length];
            _initialRotations = new int[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] == null) throw new ArgumentException("Cells cannot contain null.", "cells");
                Cells[i] = cells[i].Copy();
                _initialRotations[i] = Cells[i].Rotation;
            }

            Route = route == null ? new int[0] : (int[])route.Clone();
        }

        public bool Rotate(int index)
        {
            if (index < 0 || index >= Cells.Length || Cells[index].Locked)
                return false;

            Cells[index].Rotation = (Cells[index].Rotation + 1) & 3;
            Moves++;
            return true;
        }

        public void Reset()
        {
            for (int i = 0; i < Cells.Length; i++)
                Cells[i].Rotation = _initialRotations[i];
            Moves = 0;
        }

        public NetworkTraversal Network()
        {
            bool[] connected = new bool[Cells.Length];
            int[] distance = new int[Cells.Length];
            int[] entry = new int[Cells.Length];
            for (int i = 0; i < Cells.Length; i++)
            {
                distance[i] = -1;
                entry[i] = -1;
            }

            int[] queue = new int[Cells.Length];
            int head = 0;
            int tail = 0;
            connected[Source] = true;
            distance[Source] = 0;
            queue[tail++] = Source;

            int[] rowDelta = { -1, 0, 1, 0 };
            int[] columnDelta = { 0, 1, 0, -1 };
            int[] bits = { Directions.N, Directions.E, Directions.S, Directions.W };

            while (head < tail)
            {
                int current = queue[head++];
                int row = current / Size;
                int column = current % Size;
                int mask = Directions.Mask(Cells[current].Shape, Cells[current].Rotation);

                for (int direction = 0; direction < 4; direction++)
                {
                    if ((mask & bits[direction]) == 0) continue;
                    int nextRow = row + rowDelta[direction];
                    int nextColumn = column + columnDelta[direction];
                    if (nextRow < 0 || nextRow >= Size || nextColumn < 0 || nextColumn >= Size) continue;

                    int next = nextRow * Size + nextColumn;
                    if (connected[next]) continue;
                    int opposite = (direction + 2) & 3;
                    int nextMask = Directions.Mask(Cells[next].Shape, Cells[next].Rotation);
                    if ((nextMask & bits[opposite]) == 0) continue;

                    connected[next] = true;
                    distance[next] = distance[current] + 1;
                    entry[next] = opposite;
                    queue[tail++] = next;
                }
            }

            return new NetworkTraversal(connected, distance, entry, connected[Target]);
        }
    }

    public static class LevelFactory
    {
        private struct Detour
        {
            public int Edge;
            public int First;
            public int Second;

            public Detour(int edge, int first, int second)
            {
                Edge = edge;
                First = first;
                Second = second;
            }
        }

        public static Puzzle Create(int level)
        {
            if (level < 1 || level > 30) throw new ArgumentOutOfRangeException("level");

            int size = level <= 7 ? 4 : level <= 18 ? 5 : level <= 24 ? 6 : 7;
            int count = size * size;
            Cell[] cells = new Cell[count];

            for (int index = 0; index < count; index++)
            {
                int value = Mix(level, index);
                Shape shape = DecoyShape(level, value);
                int solution = (value >> 4) & 3;
                cells[index] = new Cell(shape, solution, solution, false);
            }

            int[] route = BuildRoute(size, level);
            int crossPosition = route.Length / 2;
            if (crossPosition == 2) crossPosition++;
            for (int position = 0; position < route.Length; position++)
            {
                int index = route[position];
                if (position == 0)
                {
                    cells[index] = new Cell(Shape.Source, 1, 1, true);
                    continue;
                }
                if (position == route.Length - 1)
                {
                    int targetSolution = FindExactRotation(Shape.Target, BitFromTo(index, route[position - 1], size));
                    cells[index] = new Cell(Shape.Target, targetSolution, targetSolution, true);
                    continue;
                }

                int required = BitFromTo(index, route[position - 1], size) |
                               BitFromTo(index, route[position + 1], size);
                Shape routeShape = AreOpposite(required) ? Shape.Straight : Shape.Elbow;
                if (level >= 8 && position == 2) routeShape = Shape.Tee;
                if (level >= 19 && position == crossPosition) routeShape = Shape.Cross;
                int solution = FindRotationContaining(routeShape, required, level + position);
                cells[index] = new Cell(routeShape, solution, solution, false);
            }

            int desiredWrong = level <= 3 ? 2 + level / 2 : Math.Min(route.Length - 2, 3 + level / 3);
            int wrong = 0;
            int idealMoves = 0;
            for (int position = 1; position < route.Length - 1 && wrong < desiredWrong; position++)
            {
                Cell cell = cells[route[position]];
                if (cell.Shape == Shape.Cross) continue;

                cell.Rotation = FindScrambleRotation(cell.Shape, cell.Solution, position == 1, Mix(level, position + 97));
                int clockwiseTurns = ClockwiseClicksToMask(cell.Shape, cell.Rotation, cell.Solution);
                idealMoves += clockwiseTurns;
                wrong++;
            }

            return new Puzzle(size, cells, route[0], route[route.Length - 1], idealMoves, level, route);
        }

        private static Shape DecoyShape(int level, int value)
        {
            if (level < 8)
                return (value & 1) == 0 ? Shape.Straight : Shape.Elbow;
            if (level < 19)
            {
                int choice = value % 3;
                return choice == 0 ? Shape.Straight : choice == 1 ? Shape.Elbow : Shape.Tee;
            }
            switch (value & 3)
            {
                case 0: return Shape.Straight;
                case 1: return Shape.Elbow;
                case 2: return Shape.Tee;
                default: return Shape.Cross;
            }
        }

        private static int[] BuildRoute(int size, int level)
        {
            List<int> route = new List<int>();
            for (int column = 0; column < size; column++)
                route.Add(column);
            for (int row = 1; row < size; row++)
                route.Add(row * size + size - 1);

            int desiredLength = DesiredRouteLength(size, level);
            bool[] used = new bool[size * size];
            for (int i = 0; i < route.Count; i++)
                used[route[i]] = true;

            int iteration = 0;
            while (route.Count + 2 <= desiredLength)
            {
                List<Detour> choices = FindDetours(route, used, size);
                if (choices.Count == 0) break;
                int choiceIndex = Mix(level, 401 + iteration * 37 + route.Count) % choices.Count;
                Detour choice = choices[choiceIndex];
                route.Insert(choice.Edge + 1, choice.First);
                route.Insert(choice.Edge + 2, choice.Second);
                used[choice.First] = true;
                used[choice.Second] = true;
                iteration++;
            }

            return route.ToArray();
        }

        private static List<Detour> FindDetours(List<int> route, bool[] used, int size)
        {
            List<Detour> result = new List<Detour>();
            for (int edge = 1; edge < route.Count - 1; edge++)
            {
                int from = route[edge];
                int to = route[edge + 1];
                int fromRow = from / size;
                int fromColumn = from % size;
                int toRow = to / size;
                int toColumn = to % size;

                if (fromRow == toRow)
                {
                    AddDetour(result, used, size, edge, fromRow - 1, fromColumn, toRow - 1, toColumn);
                    AddDetour(result, used, size, edge, fromRow + 1, fromColumn, toRow + 1, toColumn);
                }
                else
                {
                    AddDetour(result, used, size, edge, fromRow, fromColumn - 1, toRow, toColumn - 1);
                    AddDetour(result, used, size, edge, fromRow, fromColumn + 1, toRow, toColumn + 1);
                }
            }
            return result;
        }

        private static void AddDetour(List<Detour> result, bool[] used, int size, int edge, int firstRow, int firstColumn, int secondRow, int secondColumn)
        {
            if (firstRow < 0 || firstRow >= size || firstColumn < 0 || firstColumn >= size) return;
            if (secondRow < 0 || secondRow >= size || secondColumn < 0 || secondColumn >= size) return;
            int first = firstRow * size + firstColumn;
            int second = secondRow * size + secondColumn;
            if (!used[first] && !used[second])
                result.Add(new Detour(edge, first, second));
        }

        private static int DesiredRouteLength(int size, int level)
        {
            if (size == 4)
            {
                int[] lengths = { 7, 9, 9, 11, 11, 13, 13 };
                return lengths[level - 1];
            }
            if (size == 5) return 13 + 2 * ((level - 8) / 2);
            if (size == 6) return 21 + 2 * (level - 19);
            return 29 + 2 * (level - 25);
        }

        private static int BitFromTo(int from, int to, int size)
        {
            int rowDelta = to / size - from / size;
            int columnDelta = to % size - from % size;
            if (rowDelta == -1 && columnDelta == 0) return Directions.N;
            if (rowDelta == 0 && columnDelta == 1) return Directions.E;
            if (rowDelta == 1 && columnDelta == 0) return Directions.S;
            if (rowDelta == 0 && columnDelta == -1) return Directions.W;
            throw new ArgumentException("Route cells must be orthogonally adjacent.");
        }

        private static int FindRotationContaining(Shape shape, int required, int selector)
        {
            int start = Directions.NormalizeRotation(selector);
            for (int offset = 0; offset < 4; offset++)
            {
                int rotation = (start + offset) & 3;
                if ((Directions.Mask(shape, rotation) & required) == required)
                    return rotation;
            }
            throw new InvalidOperationException("Shape cannot contain the required route connections.");
        }

        private static int FindExactRotation(Shape shape, int required)
        {
            for (int rotation = 0; rotation < 4; rotation++)
                if (Directions.Mask(shape, rotation) == required)
                    return rotation;
            throw new InvalidOperationException("Shape cannot exactly match the required connection.");
        }

        private static bool AreOpposite(int mask)
        {
            return mask == (Directions.N | Directions.S) || mask == (Directions.E | Directions.W);
        }

        private static int FindScrambleRotation(Shape shape, int solution, bool excludeWest, int selector)
        {
            int solutionMask = Directions.Mask(shape, solution);
            int[] candidates = new int[4];
            int count = 0;
            for (int rotation = 0; rotation < 4; rotation++)
            {
                int mask = Directions.Mask(shape, rotation);
                if (mask == solutionMask) continue;
                if (excludeWest && (mask & Directions.W) != 0) continue;
                candidates[count++] = rotation;
            }
            if (count == 0) throw new InvalidOperationException("No usable scramble rotation exists.");
            return candidates[selector % count];
        }

        private static int ClockwiseClicksToMask(Shape shape, int rotation, int solution)
        {
            int solutionMask = Directions.Mask(shape, solution);
            for (int clicks = 0; clicks < 4; clicks++)
                if (Directions.Mask(shape, rotation + clicks) == solutionMask)
                    return clicks;
            throw new InvalidOperationException("Solution mask is unreachable by rotation.");
        }

        private static int Mix(int level, int index)
        {
            unchecked
            {
                uint value = (uint)(level * 747796405) + (uint)(index * 2891336453L) + 277803737U;
                value ^= value >> 16;
                value *= 2246822519U;
                value ^= value >> 13;
                return (int)(value & 0x7fffffffU);
            }
        }
    }

    public static class ProgressRating
    {
        public static int Stars(int moves, int ideal)
        {
            if (moves <= ideal) return 3;
            int allowance = Math.Max(3, ideal / 2);
            return moves <= ideal + allowance ? 2 : 1;
        }
    }
}
