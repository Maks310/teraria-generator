using System.Collections.Generic;
using UnityEngine;

namespace TerariaGenerator.Planets
{
    public sealed class RiverNetwork
    {
        private readonly PlanetSettings settings;
        private readonly int faceResolution;
        private readonly float[,,] heights;
        private readonly bool[,,] riverMask;
        private readonly System.Random random;

        public RiverNetwork(PlanetSettings settings, int faceResolution, float[,,] heights)
        {
            this.settings = settings;
            this.faceResolution = faceResolution;
            this.heights = heights;
            riverMask = new bool[6, faceResolution, faceResolution];
            random = new System.Random(settings.seed ^ 0x5f3759df);
        }

        public bool[,,] Generate()
        {
            if (settings.riverCount <= 0)
            {
                return riverMask;
            }

            List<Cell> candidates = CollectSourceCandidates();
            int riversMade = 0;
            int attempts = 0;

            while (riversMade < settings.riverCount && candidates.Count > 0 && attempts < settings.riverCount * 12)
            {
                attempts++;
                int index = random.Next(candidates.Count);
                Cell source = candidates[index];
                candidates.RemoveAt(index);
                if (riverMask[source.face, source.x, source.y])
                {
                    continue;
                }

                if (TraceRiver(source))
                {
                    riversMade++;
                }
            }

            CarveRiverBeds();
            return riverMask;
        }

        private List<Cell> CollectSourceCandidates()
        {
            List<Cell> cells = new List<Cell>();
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int f = 0; f < 6; f++)
            for (int x = 1; x < faceResolution - 1; x++)
            for (int y = 1; y < faceResolution - 1; y++)
            {
                float h = heights[f, x, y];
                min = Mathf.Min(min, h);
                max = Mathf.Max(max, h);
            }

            float threshold = Mathf.Lerp(min, max, settings.riverSourceMinHeight01);
            for (int f = 0; f < 6; f++)
            for (int x = 1; x < faceResolution - 1; x++)
            for (int y = 1; y < faceResolution - 1; y++)
            {
                if (heights[f, x, y] > threshold && heights[f, x, y] > settings.oceanLevel + settings.riverDepth)
                {
                    cells.Add(new Cell(f, x, y));
                }
            }

            return cells;
        }

        private bool TraceRiver(Cell source)
        {
            List<Cell> path = new List<Cell>();
            HashSet<int> visited = new HashSet<int>();
            Cell current = source;
            Cell oceanTarget = FindNearestOcean(source);
            float channelHeight = heights[current.face, current.x, current.y];

            for (int step = 0; step < settings.maxRiverSteps; step++)
            {
                int key = current.Key(faceResolution);
                if (!visited.Add(key))
                {
                    break;
                }

                path.Add(current);
                float currentHeight = heights[current.face, current.x, current.y];
                if (currentHeight <= settings.oceanLevel || riverMask[current.face, current.x, current.y])
                {
                    CommitPath(path);
                    return path.Count > 6;
                }

                Cell next = FindDownhillNeighbor(current);
                float nextHeight = heights[next.face, next.x, next.y];
                if (next.Equals(current))
                {
                    next = StepTowardNearestOcean(current, oceanTarget);
                    nextHeight = heights[next.face, next.x, next.y];
                }

                channelHeight = Mathf.Min(channelHeight - settings.riverDepth * 0.08f, nextHeight);
                heights[next.face, next.x, next.y] = Mathf.Min(heights[next.face, next.x, next.y], channelHeight);
                current = next;
            }

            return false;
        }

        private Cell FindDownhillNeighbor(Cell cell)
        {
            Cell best = cell;
            float bestHeight = heights[cell.face, cell.x, cell.y];
            foreach (Cell neighbor in GetNeighbors(cell))
            {
                float h = heights[neighbor.face, neighbor.x, neighbor.y];
                if (h < bestHeight)
                {
                    best = neighbor;
                    bestHeight = h;
                }
            }
            return best;
        }

        private Cell FindNearestOcean(Cell source)
        {
            Cell best = source;
            int bestDistance = int.MaxValue;

            for (int x = 0; x < faceResolution; x++)
            for (int y = 0; y < faceResolution; y++)
            {
                if (heights[source.face, x, y] > settings.oceanLevel) continue;
                int dx = x - source.x;
                int dy = y - source.y;
                int distance = dx * dx + dy * dy;
                if (distance < bestDistance)
                {
                    best = new Cell(source.face, x, y);
                    bestDistance = distance;
                }
            }

            return bestDistance == int.MaxValue ? new Cell(source.face, source.x, 0) : best;
        }

        private Cell StepTowardNearestOcean(Cell cell, Cell oceanTarget)
        {
            int nx = cell.x + System.Math.Sign(oceanTarget.x - cell.x);
            int ny = cell.y + System.Math.Sign(oceanTarget.y - cell.y);
            nx = Mathf.Clamp(nx, 0, faceResolution - 1);
            ny = Mathf.Clamp(ny, 0, faceResolution - 1);
            return new Cell(cell.face, nx, ny);
        }

        private IEnumerable<Cell> GetNeighbors(Cell cell)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = Mathf.Clamp(cell.x + dx, 0, faceResolution - 1);
                int ny = Mathf.Clamp(cell.y + dy, 0, faceResolution - 1);
                yield return new Cell(cell.face, nx, ny);
            }
        }

        private void CommitPath(List<Cell> path)
        {
            int radius = Mathf.Max(1, Mathf.RoundToInt(settings.riverWidth * faceResolution));
            foreach (Cell cell in path)
            {
                for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int nx = Mathf.Clamp(cell.x + dx, 0, faceResolution - 1);
                    int ny = Mathf.Clamp(cell.y + dy, 0, faceResolution - 1);
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        riverMask[cell.face, nx, ny] = true;
                    }
                }
            }
        }

        private void CarveRiverBeds()
        {
            for (int f = 0; f < 6; f++)
            for (int x = 0; x < faceResolution; x++)
            for (int y = 0; y < faceResolution; y++)
            {
                if (!riverMask[f, x, y]) continue;
                float target = settings.oceanLevel - settings.riverDepth;
                heights[f, x, y] = Mathf.Min(heights[f, x, y], target);
            }
        }

        private readonly struct Cell
        {
            public readonly int face;
            public readonly int x;
            public readonly int y;

            public Cell(int face, int x, int y)
            {
                this.face = face;
                this.x = x;
                this.y = y;
            }

            public int Key(int resolution) => face * resolution * resolution + x * resolution + y;
        }
    }
}
