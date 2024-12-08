using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;

namespace SeaStrikeClient
{
    public partial class LayoutEditor : Window
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private int[,] _gridMatrix;
        private Thread _listenThread;  // Hozzáadtunk egy szálat a szerver figyeléséhez

        public LayoutEditor(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            _client = client;
            _stream = stream;

            InitializeMatrix();

            // Indítsunk el egy külön szálat a szerver figyelésére
            _listenThread = new Thread(ListenToServer);
            _listenThread.IsBackground = true; // Hogy ha az ablak bezárul, a szál is leálljon
            _listenThread.Start();
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
            if (HajokSzama(_gridMatrix) == 20)
            {
                // Mátrix egy sorba rendezése
                string formattedMatrix = EgySorbaRendezes(_gridMatrix);

                // Mátrix elküldése a szervernek
                byte[] data = System.Text.Encoding.UTF8.GetBytes(formattedMatrix);
                _stream.Write(data, 0, data.Length);
                _stream.Flush();

                //asd
                btnStart.Visibility = Visibility.Collapsed;
                lblWaiting.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("A hajók száma nem megfelelő! Pontosan 20 hajót kell elhelyezni.");
            }
        }

        // Mátrix egy sorba rendezése
        private string EgySorbaRendezes(int[,] matrix)
        {
            string sor = "";
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                sor += "{";
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    sor += matrix[i, j];
                    if (j < matrix.GetLength(1) - 1) sor += ","; // Elemszétválasztó vessző
                }
                sor += "}";
                if (i < matrix.GetLength(0) - 1) sor += ","; // Sorok közötti vessző
            }
            return sor;
        }

        // Segédmetódus a hajók számának kiszámításához
        private int HajokSzama(int[,] matrix)
        {
            int ossz = 0;
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    ossz += matrix[i, j];
                }
            }
            return ossz;
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

        // Szerver üzenetek figyelése külön szálban
        private void ListenToServer()
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        // Ha "gamestart" üzenetet kaptunk, jelenítsük meg a MessageBox-ot
                        if (message == "gamestart")
                        {
                            Dispatcher.Invoke(() => // A UI szálon történő hívás szükséges
                            {
                                var gameWindow = new GameWindow(_client, _stream);
                                gameWindow.Show();
                                this.Close(); // A LayoutEditor ablakot bezárjuk
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Hiba történt a szerver üzenetének fogadása közben: {ex.Message}");
                    });
                    break;
                }
            }
        }
    }
}
