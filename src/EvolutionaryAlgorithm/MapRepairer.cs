using Random = System.Random;
using Constants = EvolAlgorithm.Utils.Constants;

namespace MapRepairer 
{
    public static class PathRepairer 
    {
        public struct Index2D
        {
            public int Row { get; set; }
            public int Column { get; set; }

            public Index2D(int row, int column)
            {
                Row = row;
                Column = column;
            }

            public override string ToString()
            {
                return $"({Row}, {Column})";
            }
        }

        private static List<T> GetAndPopRandomElements<T>(List<T> list, int count)
        {
            if (list == null || count < 1 || count > list.Count)
                throw new ArgumentException("Invalid list or count");

            Random random = new Random();
            List<T> randomElements = new List<T>();

            for (int i = 0; i < count; i++)
            {
                int randomIndex = random.Next(list.Count);

                randomElements.Add(list[randomIndex]);

                list.RemoveAt(randomIndex);
            }

            return randomElements;
        }

        //choose random border path tiles as start and stop, create if needed, another extra path tiles on borders are deleted
        private static List<Index2D> DeleteExtraPathTilesOnBorder(int[,] matrix)
        {
            var borderTiles = new List<Index2D>();

            int MapHeight = matrix.GetLength(0);
            int MapWidth = matrix.GetLength(1);

            // top border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[0, x] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(0, x));
                }
            }

            // bottom border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[MapHeight - 1, x] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(MapHeight - 1, x));
                }
            }

            // left border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, 0] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, 0));
                }
            }

            // right border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, MapWidth - 1] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, MapWidth - 1));
                }
            }

            var startAndStop = new List<Index2D>();
            if(borderTiles.Count >= 2)
            {
                startAndStop = GetAndPopRandomElements(borderTiles, 2);
            }
            else
            {
                Random random = new Random();
                int randomIndex = random.Next(MapHeight);
                var start = new Index2D(0, randomIndex);
                randomIndex = random.Next(MapHeight);
                var stop = new Index2D(MapWidth - 1, randomIndex);
                startAndStop.Add(start);
                startAndStop.Add(stop);
            }
            
            //var start = startAndStop[0]
            //var stop = startAndStop[1]

            // delete border path tiles except of start and stop
            foreach (var item in borderTiles)
            {
                matrix[item.Row, item.Column] = Constants.UNAVAILABLE_GROUND_TILE;
            }
            return startAndStop;
        }

        private static int[,] DeleteSoloPathTiles(int[,] matrix)
        {
            int MapHeight = matrix.GetLength(0);
            int MapWidth = matrix.GetLength(1);
            int count = 0;
            bool shouldContinue = true;
            while(shouldContinue)
            {
                count = 0;
                for (int x = 1; x < MapHeight-1; x++)
                {
                    for (int y = 1; y < MapWidth-1; y++)
                    {
                        if (matrix[x, y] == Constants.PATH_TILE && CountPathTilesInNeighborhood(matrix, x, y) < 2)
                        {
                            matrix[x, y] = Constants.AVAILABLE_GROUND_TILE;
                            count++;
                        }
                    }
                }
                if(count == 0)
                {
                    shouldContinue = false;
                }
            }
                
            return matrix;
        }

        public static int CountPathTilesInNeighborhood(int[,] region, int x, int y)
        {
            int count = 0;
            foreach (var (dx, dy) in Constants.Directions)
            {
                int newX = x + dx;
                int newY = y + dy;
                if (newX >= 0 && newX < region.GetLength(1) && newY >= 0 && newY < region.GetLength(0))
                {
                    if (region[newY, newX] == 0)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public static int[,] RepairWhatever1(int[,] individual)
        {
            var startAndStop = DeleteExtraPathTilesOnBorder(individual);
            individual = DeleteSoloPathTiles(individual);
            return individual;
        }

        public static List<Index2D> FindStandalonePoints(int[,] matrix)
        {
            List<Index2D> standalonePoints = new List<Index2D>();

            int MapHeight = matrix.GetLength(0);
            int MapWidth = matrix.GetLength(1);

            for (int x = 0; x < MapHeight; x++)
            {
                for (int y = 0; y < MapWidth; y++)
                {
                    if (matrix[x, y] == Constants.PATH_TILE && CountPathTilesInNeighborhood(matrix, x, y) == 0)
                    {
                        standalonePoints.Add(new Index2D(x, y));
                    }
                }
            }

            return standalonePoints;
        } 

        public static int[,] ConnectStandAlonePoints(List<Index2D> standalonePoints, int[,] matrix)
        {
            int MapHeight = matrix.GetLength(0);
            int MapWidth = matrix.GetLength(1);

            foreach (var point in standalonePoints)
            {
                int x = point.Row;
                int y = point.Column;

                int newX = x;
                int newY = y;

                while (CountPathTilesInNeighborhood(matrix, newX, newY) == 0)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(Constants.Directions.Count);
                    var (dx, dy) = Constants.Directions[randomIndex];

                    newX = x + dx;
                    newY = y + dy;

                    if (newX >= 0 && newX < MapWidth && newY >= 0 && newY < MapHeight)
                    {
                        if (matrix[newX, newY] == Constants.PATH_TILE)
                        {
                            matrix[x, y] = Constants.PATH_TILE;
                            break;
                        }
                    }
                }
            }

            return matrix;
        }

        public static List<Index2D> FindPathTilesOnBorder(int[,] matrix)
        {
            List<Index2D> borderTiles = new List<Index2D>();

            int MapHeight = matrix.GetLength(0);
            int MapWidth = matrix.GetLength(1);

            // top border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[0, x] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(0, x));
                }
            }

            // bottom border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[MapHeight - 1, x] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(MapHeight - 1, x));
                }
            }

            // left border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, 0] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, 0));
                }
            }

            // right border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, MapWidth - 1] == Constants.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, MapWidth - 1));
                }
            }

            return borderTiles;
        }

        public static int[,] FixBorders(int[,] matrix)
        {
            var borderPathTiles = FindPathTilesOnBorder(matrix);

            if (borderPathTiles.Count == 2) return matrix;
            if (borderPathTiles.Count < 2) 
            {
                var random = new Random();
                List<Index2D> randomPoints = new List<Index2D> { new Index2D(0, random.Next(matrix.GetLength(1))), new Index2D(matrix.GetLength(0) - 1, random.Next(matrix.GetLength(1))) };
                while (borderPathTiles.Count < 2)
                {
                    var randomPoint = randomPoints[random.Next(randomPoints.Count)];
                    if (!borderPathTiles.Contains(randomPoint))
                    {
                        borderPathTiles.Add(randomPoint);
                    }
                }
            }
            else
            {
                while (borderPathTiles.Count > 2)
                {
                    var random = new Random();
                    borderPathTiles.RemoveAt(random.Next(borderPathTiles.Count));
                }
            }

            return matrix;
        }

        public static int[,] RepairWhatever2(int[,] individual)
        {
            var standalonePoints = FindStandalonePoints(individual);
            individual = ConnectStandAlonePoints(standalonePoints, individual);
            individual = FixBorders(individual);

            return individual;
        }

        public static int[,] TestRepair1()
        {
            int[,] array1 = new int[,]
            {
                { 3, 0, 0, 1, 1, 0, 1, 3, 0, 1 },
                { 2, 0, 0, 0, 0, 2, 1, 0, 1, 0 },
                { 0, 1, 0, 0, 0, 2, 0, 0, 0, 2 },
                { 3, 2, 2, 0, 0, 0, 1, 1, 2, 2 },
                { 2, 3, 0, 0, 0, 2, 0, 2, 0, 0 },
                { 1, 0, 0, 0, 0, 0, 0, 0, 0, 2 },
                { 0, 0, 1, 0, 0, 0, 3, 1, 0, 3 },
                { 1, 3, 1, 0, 0, 0, 0, 1, 0, 1 },
                { 2, 3, 0, 0, 2, 0, 0, 2, 3, 2 },
                { 0, 3, 1, 3, 2, 3, 2, 1, 3, 1 }
            };

            return RepairWhatever2(array1);
        } 
    }
    
}