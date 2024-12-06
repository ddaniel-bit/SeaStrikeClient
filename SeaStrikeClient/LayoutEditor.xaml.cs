using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SeaStrikeClient
{
    public partial class LayoutEditor : Window
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private int[,] _gridMatrix;

        public LayoutEditor(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            _client = client;
            _stream = stream;

            InitializeMatrix();
        }

        // Mátrix inicializálása
        private void InitializeMatrix()
        {
            int rows = 10; // A gombok elrendezésének sorai
            int cols = 10; // A gombok elrendezésének oszlopai
            _gridMatrix = new int[rows, cols];
        }

        // Gombkattintás eseménykezelő
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // Gomb színének váltása
                if (clickedButton.Background == Brushes.White)
                {
                    clickedButton.Background = Brushes.Gray; // Hajó elhelyezése
                }
                else
                {
                    clickedButton.Background = Brushes.White; // Hajó eltávolítása
                }

                // Pozíció meghatározása
                var parentGrid = VisualTreeHelper.GetParent(clickedButton) as UniformGrid;
                if (parentGrid != null)
                {
                    int index = parentGrid.Children.IndexOf(clickedButton);
                    int row = index / parentGrid.Columns; // Sor index
                    int col = index % parentGrid.Columns; // Oszlop index

                    // Mátrix frissítése
                    _gridMatrix[row, col] = clickedButton.Background == Brushes.Gray ? 1 : 0;
                }
            }
        }

        // Mátrix exportálása a "Befejezés" gombnál
        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            // Mátrix állapotának ellenőrzése
            MessageBox.Show("2D Mátrix állapota:\n" + MatrixToString(_gridMatrix));
        }

        // Segédmetódus a mátrix karakterláncként való megjelenítéséhez
        private string MatrixToString(int[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            string result = "";

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result += matrix[i, j] + " ";
                }
                result += "\n";
            }

            return result;
        }
    }
}
