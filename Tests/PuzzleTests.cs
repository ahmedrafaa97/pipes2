using System;
using System.Collections.Generic;
using AquaPath.Core;

internal static class PuzzleTests
{
    private static int _passed;

    public static int Main()
    {
        Run("Direction masks rotate clockwise", DirectionMasksRotateClockwise);
        Run("Reciprocal BFS records distance and entry side", ReciprocalBfsRecordsDistanceAndEntrySide);
        Run("One-sided connections do not propagate", OneSidedConnectionsDoNotPropagate);
        Run("Rotate respects locks and Reset restores scramble", RotateAndResetBehaveCorrectly);
        Run("All thirty levels are deterministic, solvable, and initially unsolved", AllLevelsMeetGenerationContract);
        Run("Routes vary within each board size", RoutesVaryWithinEachBoardSize);
        Run("Later board groups have broadly longer routes", LaterBoardGroupsHaveLongerRoutes);
        Run("Route endpoints and route cells are valid", RouteEndpointsAndCellsAreValid);
        Run("Junction pieces unlock with difficulty", JunctionPiecesUnlockWithDifficulty);
        Run("Tutorial levels have only two or three incorrect route pieces", TutorialsHaveFewIncorrectPieces);
        Run("Ideal moves count shortest clockwise mask corrections on the route", IdealMovesCountShortestMaskCorrections);
        Run("Star thresholds include their boundaries", StarThresholdsIncludeBoundaries);

        Console.WriteLine("PASS: " + _passed + " puzzle model tests");
        return 0;
    }

    private static void DirectionMasksRotateClockwise()
    {
        Equal(5, Directions.Mask(Shape.Straight, 0), "vertical straight");
        Equal(10, Directions.Mask(Shape.Straight, 1), "horizontal straight");
        Equal(3, Directions.Mask(Shape.Elbow, 0), "north-east elbow");
        Equal(6, Directions.Mask(Shape.Elbow, 1), "east-south elbow");
        Equal(11, Directions.Mask(Shape.Tee, 0), "north-east-west tee");
        Equal(7, Directions.Mask(Shape.Tee, 1), "north-east-south tee");
        Equal(15, Directions.Mask(Shape.Cross, 3), "cross");
        Equal(2, Directions.Mask(Shape.Source, 1), "east source");
        Equal(8, Directions.Mask(Shape.Target, -1), "negative rotations normalize");

        foreach (Shape shape in Enum.GetValues(typeof(Shape)))
            Equal(Directions.Mask(shape, 0), Directions.Mask(shape, 4), shape + " repeats after four turns");
    }

    private static void ReciprocalBfsRecordsDistanceAndEntrySide()
    {
        Cell[] cells =
        {
            new Cell(Shape.Source, 1, 1, true),       // 0: E
            new Cell(Shape.Tee, 2, 2, false),         // 1: E,S,W
            new Cell(Shape.Straight, 1, 1, false),    // 2: E,W branch
            new Cell(Shape.Elbow, 2, 2, false),
            new Cell(Shape.Straight, 0, 0, false),    // 4: N,S
            new Cell(Shape.Elbow, 1, 1, false),
            new Cell(Shape.Straight, 1, 1, false),
            new Cell(Shape.Elbow, 0, 0, false),       // 7: N,E
            new Cell(Shape.Target, 3, 3, true)        // 8: W
        };
        Puzzle puzzle = new Puzzle(3, cells, 0, 8, 4);

        NetworkTraversal network = puzzle.Network();

        True(network.ReachesTarget, "fixture should reach target");
        True(network.Connected[0], "source connected");
        True(network.Connected[2], "tee branch connected");
        True(network.Connected[8], "target connected");
        Equal(0, network.Distance[0], "source distance");
        Equal(2, network.Distance[2], "branch distance");
        Equal(4, network.Distance[8], "target distance");
        Equal(-1, network.Entry[0], "source has no entry side");
        Equal(3, network.Entry[1], "cell 1 entered from west");
        Equal(0, network.Entry[4], "cell 4 entered from north");
        Equal(3, network.Entry[8], "target entered from west");
        True(puzzle.Solved, "Solved mirrors target reachability");
    }

    private static void OneSidedConnectionsDoNotPropagate()
    {
        Cell[] cells =
        {
            new Cell(Shape.Source, 1, 1, true),
            new Cell(Shape.Straight, 0, 0, false),
            new Cell(Shape.Target, 3, 3, true),
            new Cell(Shape.Cross, 0, 0, false)
        };
        Puzzle puzzle = new Puzzle(2, cells, 0, 2, 0);

        NetworkTraversal network = puzzle.Network();

        True(network.Connected[0], "source remains connected to itself");
        False(network.Connected[1], "neighbor lacking west opening stays dry");
        False(network.ReachesTarget, "target is unreachable");
        Equal(-1, network.Distance[1], "unreachable distance");
        Equal(-1, network.Entry[1], "unreachable entry");
    }

    private static void RotateAndResetBehaveCorrectly()
    {
        Cell[] cells =
        {
            new Cell(Shape.Source, 1, 1, true),
            new Cell(Shape.Elbow, 3, 1, false),
            new Cell(Shape.Straight, 0, 0, true),
            new Cell(Shape.Target, 3, 3, true)
        };
        Puzzle puzzle = new Puzzle(2, cells, 0, 3, 2);

        False(puzzle.Rotate(0), "locked source cannot rotate");
        Equal(0, puzzle.Moves, "locked rotation costs no move");
        True(puzzle.Rotate(1), "unlocked cell rotates");
        Equal(0, puzzle.Cells[1].Rotation, "rotation wraps from three to zero");
        Equal(1, puzzle.Moves, "successful rotation increments moves");
        False(puzzle.Rotate(-1), "negative index rejected");
        False(puzzle.Rotate(99), "large index rejected");
        Equal(1, puzzle.Moves, "invalid rotations cost no move");

        puzzle.Reset();
        Equal(3, puzzle.Cells[1].Rotation, "reset restores initial orientation");
        Equal(0, puzzle.Moves, "reset clears moves");
    }

    private static void AllLevelsMeetGenerationContract()
    {
        for (int level = 1; level <= 30; level++)
        {
            Puzzle first = LevelFactory.Create(level);
            Puzzle second = LevelFactory.Create(level);
            int expectedSize = level <= 7 ? 4 : level <= 18 ? 5 : level <= 24 ? 6 : 7;

            Equal(level, first.Level, "level number " + level);
            Equal(expectedSize, first.Size, "board size level " + level);
            Equal(expectedSize * expectedSize, first.Cells.Length, "cell count level " + level);
            Equal(0, first.Source, "source index level " + level);
            Equal(first.Cells.Length - 1, first.Target, "target index level " + level);
            Equal(2, Directions.Mask(first.Cells[first.Source].Shape, first.Cells[first.Source].Solution), "source points inward level " + level);
            int targetMask = Directions.Mask(first.Cells[first.Target].Shape, first.Cells[first.Target].Solution);
            True(targetMask == Directions.N || targetMask == Directions.W, "target points inward level " + level);
            False(first.Solved, "scramble is unsolved level " + level);
            True(first.IdealMoves > 0, "positive ideal moves level " + level);
            True(Contains(first, Shape.Elbow), "elbow included level " + level);

            for (int i = 0; i < first.Cells.Length; i++)
            {
                Equal(first.Cells[i].Shape, second.Cells[i].Shape, "deterministic shape level " + level + " cell " + i);
                Equal(first.Cells[i].Rotation, second.Cells[i].Rotation, "deterministic rotation level " + level + " cell " + i);
                Equal(first.Cells[i].Solution, second.Cells[i].Solution, "deterministic solution level " + level + " cell " + i);
            }
            Equal(first.Route.Length, second.Route.Length, "deterministic route length level " + level);
            for (int i = 0; i < first.Route.Length; i++)
                Equal(first.Route[i], second.Route[i], "deterministic route level " + level + " position " + i);

            for (int i = 1; i < first.Route.Length; i++)
                True(AreReciprocalInSolution(first, first.Route[i - 1], first.Route[i]), "solution route link level " + level + " segment " + i);

            int[] scramble = new int[first.Cells.Length];
            for (int i = 0; i < scramble.Length; i++)
            {
                scramble[i] = first.Cells[i].Rotation;
                first.Cells[i].Rotation = first.Cells[i].Solution;
            }
            True(first.Solved, "stored solution reaches target level " + level);

            first.Reset();
            for (int i = 0; i < scramble.Length; i++)
                Equal(scramble[i], first.Cells[i].Rotation, "reset exact scramble level " + level + " cell " + i);
        }
    }

    private static void RoutesVaryWithinEachBoardSize()
    {
        True(UniqueRouteCount(1, 7) >= 4, "4x4 levels should expose at least four routes");
        True(UniqueRouteCount(8, 18) >= 6, "5x5 levels should expose at least six routes");
        True(UniqueRouteCount(19, 24) >= 4, "6x6 levels should expose at least four routes");
        True(UniqueRouteCount(25, 30) >= 4, "7x7 levels should expose at least four routes");
    }

    private static void LaterBoardGroupsHaveLongerRoutes()
    {
        double fourAverage = AverageRouteLength(1, 7);
        double fiveAverage = AverageRouteLength(8, 18);
        double sixAverage = AverageRouteLength(19, 24);
        double sevenAverage = AverageRouteLength(25, 30);

        True(fiveAverage > fourAverage + 3, "5x5 route average should exceed 4x4");
        True(sixAverage > fiveAverage + 3, "6x6 route average should exceed 5x5");
        True(sevenAverage > sixAverage + 3, "7x7 route average should exceed 6x6");
        True(LevelFactory.Create(30).Route.Length >= 29, "level 30 should use a long route");
    }

    private static void RouteEndpointsAndCellsAreValid()
    {
        for (int level = 1; level <= 30; level++)
        {
            Puzzle puzzle = LevelFactory.Create(level);
            Equal(0, puzzle.Route[0], "route source level " + level);
            Equal(1, puzzle.Route[1], "first route step is east level " + level);
            Equal(puzzle.Cells.Length - 1, puzzle.Route[puzzle.Route.Length - 1], "route target level " + level);
            Equal(Directions.E, Directions.Mask(puzzle.Cells[puzzle.Source].Shape, puzzle.Cells[puzzle.Source].Solution), "source solution faces east level " + level);
            False(puzzle.Cells[puzzle.Route[1]].Locked, "first route tile is mutable level " + level);
            Equal(0, Directions.Mask(puzzle.Cells[puzzle.Route[1]].Shape, puzzle.Cells[puzzle.Route[1]].Rotation) & Directions.W, "scrambled first tile excludes west level " + level);

            bool[] seen = new bool[puzzle.Cells.Length];
            for (int i = 0; i < puzzle.Route.Length; i++)
            {
                int index = puzzle.Route[i];
                True(index >= 0 && index < puzzle.Cells.Length, "route index in range level " + level);
                False(seen[index], "route is self avoiding level " + level + " at " + index);
                seen[index] = true;
            }

            int targetPrevious = puzzle.Route[puzzle.Route.Length - 2];
            int expectedTargetMask = targetPrevious == puzzle.Target - 1 ? Directions.W : Directions.N;
            Equal(expectedTargetMask, Directions.Mask(puzzle.Cells[puzzle.Target].Shape, puzzle.Cells[puzzle.Target].Solution), "target faces previous route cell level " + level);
        }
    }

    private static void JunctionPiecesUnlockWithDifficulty()
    {
        for (int level = 1; level <= 7; level++)
        {
            False(Contains(LevelFactory.Create(level), Shape.Tee), "tee appears too early at level " + level);
            False(Contains(LevelFactory.Create(level), Shape.Cross), "cross appears too early at level " + level);
        }
        for (int level = 8; level <= 18; level++)
        {
            True(Contains(LevelFactory.Create(level), Shape.Tee), "tee missing at level " + level);
            False(Contains(LevelFactory.Create(level), Shape.Cross), "cross appears before level 19 at level " + level);
        }
        for (int level = 19; level <= 30; level++)
        {
            True(Contains(LevelFactory.Create(level), Shape.Tee), "tee missing at level " + level);
            True(Contains(LevelFactory.Create(level), Shape.Cross), "cross missing at level " + level);
        }
    }

    private static void TutorialsHaveFewIncorrectPieces()
    {
        for (int level = 1; level <= 3; level++)
        {
            Puzzle puzzle = LevelFactory.Create(level);
            int wrong = 0;
            for (int i = 0; i < puzzle.Route.Length; i++)
            {
                Cell cell = puzzle.Cells[puzzle.Route[i]];
                if (Directions.Mask(cell.Shape, cell.Rotation) != Directions.Mask(cell.Shape, cell.Solution))
                    wrong++;
            }
            True(wrong == 2 || wrong == 3, "tutorial level " + level + " wrong route-piece count was " + wrong);
        }
    }

    private static void IdealMovesCountShortestMaskCorrections()
    {
        for (int level = 1; level <= 30; level++)
        {
            Puzzle puzzle = LevelFactory.Create(level);
            int expected = 0;
            for (int i = 1; i < puzzle.Route.Length - 1; i++)
            {
                Cell cell = puzzle.Cells[puzzle.Route[i]];
                int solutionMask = Directions.Mask(cell.Shape, cell.Solution);
                for (int clicks = 0; clicks < 4; clicks++)
                {
                    if (Directions.Mask(cell.Shape, cell.Rotation + clicks) == solutionMask)
                    {
                        expected += clicks;
                        break;
                    }
                }
            }
            Equal(expected, puzzle.IdealMoves, "symmetry-aware ideal moves level " + level);
        }
    }

    private static void StarThresholdsIncludeBoundaries()
    {
        Equal(3, ProgressRating.Stars(8, 8), "ideal boundary");
        Equal(2, ProgressRating.Stars(9, 8), "one over ideal");
        Equal(2, ProgressRating.Stars(12, 8), "two-star upper boundary");
        Equal(1, ProgressRating.Stars(13, 8), "past two-star boundary");
        Equal(2, ProgressRating.Stars(5, 2), "minimum allowance boundary");
        Equal(1, ProgressRating.Stars(6, 2), "past minimum allowance");
    }

    private static bool Contains(Puzzle puzzle, Shape shape)
    {
        for (int i = 0; i < puzzle.Cells.Length; i++)
            if (puzzle.Cells[i].Shape == shape)
                return true;
        return false;
    }

    private static int UniqueRouteCount(int firstLevel, int lastLevel)
    {
        HashSet<string> routes = new HashSet<string>();
        for (int level = firstLevel; level <= lastLevel; level++)
            routes.Add(string.Join(",", LevelFactory.Create(level).Route));
        return routes.Count;
    }

    private static double AverageRouteLength(int firstLevel, int lastLevel)
    {
        int total = 0;
        for (int level = firstLevel; level <= lastLevel; level++)
            total += LevelFactory.Create(level).Route.Length;
        return (double)total / (lastLevel - firstLevel + 1);
    }

    private static bool AreReciprocalInSolution(Puzzle puzzle, int from, int to)
    {
        int rowDelta = to / puzzle.Size - from / puzzle.Size;
        int columnDelta = to % puzzle.Size - from % puzzle.Size;
        int outBit;
        int inBit;
        if (rowDelta == -1 && columnDelta == 0) { outBit = Directions.N; inBit = Directions.S; }
        else if (rowDelta == 0 && columnDelta == 1) { outBit = Directions.E; inBit = Directions.W; }
        else if (rowDelta == 1 && columnDelta == 0) { outBit = Directions.S; inBit = Directions.N; }
        else if (rowDelta == 0 && columnDelta == -1) { outBit = Directions.W; inBit = Directions.E; }
        else throw new Exception("Route contains non-adjacent cells");

        int fromMask = Directions.Mask(puzzle.Cells[from].Shape, puzzle.Cells[from].Solution);
        int toMask = Directions.Mask(puzzle.Cells[to].Shape, puzzle.Cells[to].Solution);
        return (fromMask & outBit) != 0 && (toMask & inBit) != 0;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine("ok " + _passed + " - " + name);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FAIL - " + name + ": " + exception.Message);
            Environment.Exit(1);
        }
    }

    private static void True(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void False(bool value, string message)
    {
        if (value) throw new Exception(message);
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!object.Equals(expected, actual))
            throw new Exception(message + ": expected " + expected + ", got " + actual);
    }
}
