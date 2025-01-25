namespace EvolAlgorithm.Utils
{
    public static class Utils
    {
        public static (MapParameters, StopConditionParameters, AlgorithmParameters, MethodSelectionParameters, PoolingInitializationParameters) GetParameters()
        {
            return (new MapParameters { Width = 10, Height = 10, KernelSize = 3 },
                    new StopConditionParameters { MaxGenerations = 10, TimeLimit = 6 },
                    new AlgorithmParameters { PopulationSize = 500, MutationRate = 0.2, TournamentSize = 2, ElitesPercentage = 0.05 },
                    new MethodSelectionParameters
                    {
                        InitializationMethod = InitializationMethodType.Pools,
                        SelectionMethod = SelectionMethodType.Tournament,
                        CrossoverMethod = CrossoverMethodType.OrderedCrossover,
                        MutationMethod = MutationMethodType.Swap,
                        RepairMethod = RepairMethodType.Whatever1,
                        CreatorMethod = CreatingNewGenerationType.Elitism
                    },
                    new PoolingInitializationParameters
                    {
                        unavailableGroundPoolCount = 2,
                        waterPoolCount = 2,
                        unavailableGroundMinArea = 0.1,
                        unavailableGroundMaxArea = 0.3,
                        waterMinArea = 0.1,
                        waterMaxArea = 0.25,
                        pathMinArea = 0.05,
                        pathMaxArea = 0.2
                    }
                );
        }

        public static bool IsBetweenZeroAndOne(double value)
        {
            return value >= 0.0 && value <= 1.0;
        }

        public static string GetTileDescription(int tileIndex)
        {
            if (tileIndex < 0 || tileIndex >= Constants.TileDescriptions.Length)
                throw new ArgumentOutOfRangeException(nameof(tileIndex), "Invalid tile index");

            return Constants.TileDescriptions[tileIndex];
        }

        public static void PrintMap(int[,] map)
        {
            for (int i = 0; i < map.GetLength(0); i++)
            {
                for (int j = 0; j < map.GetLength(1); j++)
                {
                    //string description = GetTileDescription(map[i, j]);
                    string description = map[i, j].ToString();
                    Console.Write(description.PadRight(5));
                }
                Console.WriteLine();
            }
        }

        public static (int, int) FindValuePosition(int[,] matrix, int value)
        {
            for (int y = 0; y < matrix.GetLength(0); y++)
            {
                for (int x = 0; x < matrix.GetLength(1); x++)
                {
                    if (matrix[y, x] == value)
                    {
                        return (y, x);
                    }
                }
            }
            throw new InvalidOperationException("Value not found in the matrix.");
        }

        public static int[,] ExtractKernelRegion(int[,] matrix, int anchorX, int anchorY, int kernelSize)
        {
            int offset = kernelSize / 2;
            int[,] kernelRegion = new int[kernelSize, kernelSize];

            for (int ky = -offset; ky <= offset; ky++)
            {
                for (int kx = -offset; kx <= offset; kx++)
                {
                    int x = anchorX + kx;
                    int y = anchorY + ky;

                    if (x < 0 || x >= matrix.GetLength(1) || y < 0 || y >= matrix.GetLength(0))
                    {
                        kernelRegion[ky + offset, kx + offset] = -1;
                    }
                    else
                    {
                        kernelRegion[ky + offset, kx + offset] = matrix[y, x];
                    }
                }
            }

            return kernelRegion;
        }

        public static bool IsSoloCell(int[,] region, int x, int y, int value)
        {
            foreach (var (dx, dy) in Constants.Directions)
            {
                int newX = x + dx;
                int newY = y + dy;

                if (newX >= 0 && newX < region.GetLength(1) && newY >= 0 && newY < region.GetLength(0))
                {
                    if (region[newY, newX] == value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static int CountTilesOfTypeInRegion(int[,] region, int wantedValue)
        {
            int count = 0;
            for (int y = 0; y < region.GetLength(0); y++)
            {
                for (int x = 0; x < region.GetLength(1); x++)
                {
                    if (region[y, x] == wantedValue)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static ((int x1, int y1), (int x2, int y2)) SelectTwoWeightedRandom(double[,] fitnessMatrix)
        {
            Random random = new();
            List<(int x, int y, double weight)> weightedElements = new();
            double totalWeight = 0.0;

            for (int y = 0; y < fitnessMatrix.GetLength(0); y++)
            {
                for (int x = 0; x < fitnessMatrix.GetLength(1); x++)
                {
                    double fitness = fitnessMatrix[y, x];
                    double weight = fitness <= 0 ? 0.0 : 1.0 / fitness;
                    weightedElements.Add((x, y, weight));
                    totalWeight += weight;
                }
            }

            (int x, int y) SelectRandomElement()
            {
                double randomValue = random.NextDouble() * totalWeight;
                double currentWeight = 0.0;

                foreach (var element in weightedElements)
                {
                    currentWeight += element.weight;
                    if (currentWeight >= randomValue)
                    {
                        return (element.x, element.y);
                    }
                }
                return (weightedElements.Last().x, weightedElements.Last().y);
            }

            var firstElement = SelectRandomElement();
            var secondElement = SelectRandomElement();

            return (firstElement, secondElement);
        }

        public static void InsertKernelRegion(int[,] map, int centerX, int centerY, int[,] kernelRegion)
        {
            int kernelSize = kernelRegion.GetLength(0);
            int offset = kernelSize / 2;

            for (int ky = -offset; ky <= offset; ky++)
            {
                for (int kx = -offset; kx <= offset; kx++)
                {
                    int x = centerX + kx;
                    int y = centerY + ky;

                    if (x >= 0 && x < map.GetLength(1) && y >= 0 && y < map.GetLength(0) && kernelRegion[ky + offset, kx + offset] != -1)
                    {
                        map[y, x] = kernelRegion[ky + offset, kx + offset];
                    }
                }
            }
        }

        public static (int x, int y) SelectIndexWeightedPromoteLowFitness(double[,] fitnessMatrix)
        {
            Random random = new();
            var flatFitness = fitnessMatrix.Cast<double>().ToArray();
            double totalFitness = flatFitness.Sum();
            var probabilities = flatFitness.Select(fitness => 1.0 - fitness / totalFitness).ToArray();
            double randomValue = random.NextDouble() * probabilities.Sum();

            double cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    int x = i % fitnessMatrix.GetLength(1);
                    int y = i / fitnessMatrix.GetLength(0);
                    return (x, y);
                }
            }
            return (0, 0); // Fallback, should not reach here
        }

        public static (int x, int y) SelectIndexWeightedPromoteHighFitness(double[,] fitnessMatrix)
        {
            Random random = new();
            var flatFitness = fitnessMatrix.Cast<double>().ToArray();
            double totalFitness = flatFitness.Sum();
            var probabilities = flatFitness.Select(fitness => fitness / totalFitness).ToArray();
            double randomValue = random.NextDouble() * probabilities.Sum();

            double cumulativeProbability = 0;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulativeProbability += probabilities[i];
                if (randomValue <= cumulativeProbability)
                {
                    int x = i % fitnessMatrix.GetLength(1);
                    int y = i / fitnessMatrix.GetLength(0);
                    return (x, y);
                }
            }
            return (0, 0); // Fallback, should not reach here
        }

        public static List<(int x, int y)> ExtractPool(int[,] map, int startX, int startY, int tileType)
        {
            int mapHeight = map.GetLength(0);
            int mapWidth = map.GetLength(1);
            List<(int x, int y)> pool = new();
            Queue<(int x, int y)> queue = new();
            HashSet<(int x, int y)> visited = new();

            queue.Enqueue((startX, startY));
            visited.Add((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                pool.Add((x, y));

                foreach (var (dx, dy) in Constants.Directions)
                {
                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < mapWidth && ny >= 0 && ny < mapHeight &&
                        map[ny, nx] == tileType && !visited.Contains((nx, ny)))
                    {
                        queue.Enqueue((nx, ny));
                        visited.Add((nx, ny));
                    }
                }
            }

            return pool;
        }

        public static List<List<(int x, int y)>> ExtractAllPools(int[,] map, int tileType)
        {
            int mapHeight = map.GetLength(0);
            int mapWidth = map.GetLength(1);
            bool[,] visited = new bool[mapHeight, mapWidth];
            List<List<(int x, int y)>> pools = new();

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    if (!visited[y, x] && map[y, x] == tileType)
                    {
                        List<(int x, int y)> pool = ExtractPool(map, x, y, tileType);
                        pools.Add(pool);

                        // Mark all pool tiles as visited
                        foreach (var (px, py) in pool)
                        {
                            visited[py, px] = true;
                        }
                    }
                }
            }

            return pools;
        }

    }
}
