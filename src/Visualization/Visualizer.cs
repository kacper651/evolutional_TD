using EvolutionaryAlgorithmUtils;

namespace evolutional_TD;

public partial class Visualizer : Form
{
    private int[,] matrix;
    private int cellSize = 40;

    public Visualizer(int[,] inputMatrix)
    {
        this.matrix = inputMatrix;
        this.Text = "Matrix Visualizer";
        this.ClientSize = new Size(
            inputMatrix.GetLength(1) * cellSize, 
            inputMatrix.GetLength(0) * cellSize
        );
        this.Paint += new PaintEventHandler(DrawMatrix);
    }

    private void DrawMatrix(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        for (int row = 0; row < matrix.GetLength(0); row++)
        {
            for (int col = 0; col < matrix.GetLength(1); col++)
            {
                int value = matrix[row, col];
                Color cellColor = GetColorBasedOnValue(value);
                using (Brush brush = new SolidBrush(cellColor))
                {
                    int x = col * cellSize;
                    int y = row * cellSize;
                    g.FillRectangle(brush, x, y, cellSize, cellSize);
                    g.DrawRectangle(Pens.Black, x, y, cellSize, cellSize);
                }
            }
        }
    }

    private Color GetColorBasedOnValue(int value)
    {
        return value switch
        {
            MapUtils.PATH_TILE => Color.White,
            MapUtils.WATER_TILE => Color.Blue,
            MapUtils.AVAILABLE_GROUND_TILE => Color.Green,
            MapUtils.UNAVAILABLE_GROUND_TILE => Color.Red,
            4 => Color.Yellow,
            _ => Color.Gray,
        };
    }
}
