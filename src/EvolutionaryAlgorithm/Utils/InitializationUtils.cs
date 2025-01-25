namespace EvolAlgorithm.Utils
{
    public static class InitializationUtils
    {
        public static (int startX, int startY, int endX, int endY) GetRandomStartEndCoordinates(int MapWidth, int MapHeight)
        {
            Random random = new();
            // Sides of the rectangle: 0 = top, 1 = bottom, 2 = left, 3 = right
            int startSide = random.Next(4);
            int endSide;

            do
            {
                endSide = random.Next(4);
            } while (endSide == startSide);

            Console.WriteLine("Start side: {0} | End side: {1}", startSide, endSide);

            int startX, startY;
            switch (startSide)
            {
                case 0: // Top
                    startX = random.Next(1, MapWidth - 2);
                    startY = 0;
                    break;
                case 1: // Bottom
                    startX = random.Next(1, MapWidth - 2);
                    startY = MapHeight - 1;
                    break;
                case 2: // Left
                    startX = 0;
                    startY = random.Next(1, MapHeight - 2);
                    break;
                case 3: // Right
                    startX = MapWidth - 1;
                    startY = random.Next(1, MapHeight - 2);
                    break;
                default:
                    throw new InvalidOperationException("Invalid start side");
            }

            int endX, endY;
            switch (endSide)
            {
                case 0: // Top
                    endX = random.Next(1, MapWidth - 2);
                    endY = 0;
                    break;
                case 1: // Bottom
                    endX = random.Next(MapWidth);
                    endY = MapHeight - 1;
                    break;
                case 2: // Left
                    endX = 0;
                    endY = random.Next(1, MapHeight - 2);
                    break;
                case 3: // Right
                    endX = MapWidth - 1;
                    endY = random.Next(1, MapHeight - 2);
                    break;
                default:
                    throw new InvalidOperationException("Invalid end side");
            }

            return (startX, startY, endX, endY);
        }

        public static List<(int, int)> GenerateTrail(int startX, int startY, int endX, int endY, int MapWidth, int MapHeight)
        {
            List<(int, int)> trail = new();
            Random random = new();

            int currentX = startX;
            int currentY = startY;

            trail.Add((currentX, currentY));

            //Console.WriteLine($"\nStart: ({currentX}, {currentY}), End: ({endX}, {endY})\n");

            while (currentX != endX || currentY != endY)
            {
                List<(int dx, int dy)> moves = new();
                if (currentX < endX) moves.Add((1, 0)); // Move right
                if (currentX > endX) moves.Add((-1, 0)); // Move left
                if (currentY < endY) moves.Add((0, 1)); // Move down
                if (currentY > endY) moves.Add((0, -1)); // Move up

                if (random.NextDouble() < 0.3)
                {
                    if (currentY > 1) moves.Add((0, -1)); // Move up
                    if (currentY < MapHeight - 2) moves.Add((0, 1)); // Move down
                    if (currentX > 1) moves.Add((-1, 0)); // Move left
                    if (currentX < MapWidth - 2) moves.Add((1, 0)); // Move right
                }

                if (moves.Count == 0)
                {
                    break;
                }

                var (dx, dy) = moves[random.Next(moves.Count)];

                currentX += dx;
                currentY += dy;

                currentX = Math.Clamp(currentX, 0, MapWidth - 1);
                currentY = Math.Clamp(currentY, 0, MapHeight - 1);

                if (!trail.Contains((currentX, currentY)))
                {
                    trail.Add((currentX, currentY));
                }
            }

            return trail;
        }

        public static int[,] InitializePools(int[,] matrix, int unavailableGroundPoolCount, int waterPoolCount, double unavailableGroundMinAreaPerc, double unavailableGroundMaxAreaPerc, double waterMinAreaPerc, double waterMaxAreaPerc)
        {
            int mapHeight = matrix.GetLength(0);
            int mapWidth = matrix.GetLength(1);
            int mapSize = mapHeight * mapWidth;

            int minWaterArea = (int)Math.Ceiling(mapSize * waterMinAreaPerc);
            int maxWaterArea = (int)Math.Floor(mapSize * waterMaxAreaPerc);
            int minUnavailableGroundArea = (int)Math.Ceiling(mapSize * unavailableGroundMinAreaPerc);
            int maxUnavailableGroundArea = (int)Math.Floor(mapSize * unavailableGroundMaxAreaPerc);

            PlacePoolsFlood(matrix, waterPoolCount, Constants.WATER_TILE, minWaterArea, maxWaterArea);
            PlacePoolsFlood(matrix, unavailableGroundPoolCount, Constants.UNAVAILABLE_GROUND_TILE, minUnavailableGroundArea, maxUnavailableGroundArea);

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    if (matrix[y, x] == -1)
                    {
                        matrix[y, x] = Constants.AVAILABLE_GROUND_TILE;
                    }
                }
            }

            return matrix;
        }

        private static void PlacePoolsFlood(int[,] map, int poolCount, int tileType, int minArea, int maxArea)
        {
            int mapHeight = map.GetLength(0);
            int mapWidth = map.GetLength(1);
            int totalSize = mapHeight * mapWidth;
            int totalPlacedArea = 0;

            Random random = new();

            for (int i = 0; i < poolCount; i++)
            {
                int remainingArea = maxArea - totalPlacedArea;
                if (remainingArea <= 0)
                {
                    break;
                }

                int poolTargetArea = random.Next(minArea, Math.Min(remainingArea, maxArea / (poolCount - i)));

                for (int attempts = 0; attempts < 10; attempts++)
                {
                    int startX = random.Next(0, mapWidth);
                    int startY = random.Next(0, mapHeight);

                    if (map[startY, startX] == -1)
                    {
                        int placedTiles = FloodFillPool(map, startX, startY, tileType, poolTargetArea);
                        totalPlacedArea += placedTiles;
                        break;
                    }

                }
            }
        }

        private static int FloodFillPool(int[,] map, int startX, int startY, int tileType, int targetArea)
        {
            int mapHeight = map.GetLength(0);
            int mapWidth = map.GetLength(1);

            Queue<(int x, int y)> queue = new();
            queue.Enqueue((startX, startY));

            int areaPlaced = 0;
            Random random = new();

            while (queue.Count > 0 && areaPlaced < targetArea)
            {
                (int x, int y) = queue.Dequeue();

                if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight || map[y, x] != -1)
                    continue;

                map[y, x] = tileType;
                areaPlaced++;

                var neighbors = Constants.Directions
                    .Select(dir => (x: x + dir.Item1, y: y + dir.Item2))
                    .Where(
                        neighbor =>
                            neighbor.x >= 0 &&
                            neighbor.x < mapWidth &&
                            neighbor.y >= 0 &&
                            neighbor.y < mapHeight &&
                            map[neighbor.y, neighbor.x] == -1
                    )
                    .OrderBy(_ => random.Next());

                foreach (var neighbor in neighbors)
                {
                    queue.Enqueue(neighbor);
                }
            }
            return areaPlaced;
        }

    }
}
