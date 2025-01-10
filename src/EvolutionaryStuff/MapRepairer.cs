using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using Random = System.Random;
using EvolutionaryAlgorithmUtils;

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

        private List<Index2D> GetPathTilesOnBorders(int[,] matrix)
        {
            var borderTiles = new List<Index2D>();

            // top border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[0, x] == MapUtils.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(0, x));
                }
            }

            // bottom border (+ corners)
            for (int x = 0; x < MapWidth; x++)
            {
                if (matrix[MapHeight - 1, x] == MapUtils.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(MapHeight - 1, x));
                }
            }

            // left border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, 0] == MapUtils.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, 0));
                }
            }

            // right border
            for (int y = 1; y < MapHeight - 1; y++)
            {
                if (matrix[y, MapWidth - 1] == MapUtils.PATH_TILE)
                {
                    borderTiles.Add(new Index2D(y, MapWidth - 1));
                }
            }
            
            var startAndStop = GetAndPopRandomElements(borderTiles, 2);
            //var start = startAndStop[0]
            //var stop = startAndStop[1]

            // delete border path tiles except of start and stop
            foreach (var item in borderTiles)
            {
                matrix[item.row, item.column] = 1
            }
            return startAndStop;
        }

        static List<T> GetAndPopRandomElements<T>(List<T> list, int count)
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

        public static int[,] RepairWhatever1(int[,] individual)
        {
            return individual;
        }

        public static int[,] RepairWhatever2(int[,] individual)
        {
            throw new NotImplementedException();
        }
    }
    
}