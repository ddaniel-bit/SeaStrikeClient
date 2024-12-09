using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SeaStrikeClient
{
    /// <summary>
    /// Interaction logic for LayoutEditortemp.xaml
    /// </summary>
    /// 
    public class Ship
    {
        public int StartColumn { get; set; }
        public int StartRow { get; set; }
        public int Length { get; set; }
        public bool IsHorizontal { get; set; }

        public Ship(int startColumn, int startRow, int length, bool isHorizontal)
        {
            StartColumn = startColumn;
            StartRow = startRow;
            Length = length;
            IsHorizontal = isHorizontal;
        }
    }
    public partial class LayoutEditortemp : Window
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private int[,] _gridMatrix;
        private Thread _listenThread;

        private Rectangle _draggedRectangle;
        private Point _dragStartPoint;
        private bool _isHorizontal = true;

        private int[,] gridMatrix;
        private const int GridSize = 10;
        private List<Ship> placedShips = new List<Ship>();

        private int totalShipsPlaced = 0;
        private int[,] gridState;
        public LayoutEditortemp(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            InitializeGridMatrix();
            InitializeGrid();
            gridState = new int[GridSize, GridSize];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    gridState[i, j] = 0;
                }
            }
            _client = client;
            _stream = stream;

            InitializeMatrix();

            // Indítsunk el egy külön szálat a szerver figyelésére
            _listenThread = new Thread(ListenToServer);
            _listenThread.IsBackground = true; // Hogy ha az ablak bezárul, a szál is leálljon
            _listenThread.Start();
        }

        private void InitializeMatrix()
        {
            int rows = 10; // A gombok elrendezésének sorai
            int cols = 10; // A gombok elrendezésének oszlopai
            _gridMatrix = new int[rows, cols];
        }
        private void InitializeGrid()
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    var rect = new Rectangle
                    {
                        Fill = Brushes.LightGray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };



                    Grid.SetRow(rect, row);
                    Grid.SetColumn(rect, col);
                    MatrixGrid.Children.Add(rect);
                }
            }
        }

        private void InitializeGridMatrix()
        {
            gridMatrix = new int[GridSize, GridSize];

            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    gridMatrix[i, j] = 0;
                }
            }
        }

        private void MarkOccupiedArea(int startColumn, int startRow, int shipLength, bool isHorizontal)
        {
            for (int i = 0; i < shipLength; i++)
            {
                if (isHorizontal)
                {
                    gridMatrix[startColumn + i, startRow] = 1;
                }
                else
                {
                    gridMatrix[startColumn, startRow + i] = 1;
                }
            }

            for (int i = -1; i <= shipLength; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (isHorizontal)
                    {
                        if (startRow + j >= 0 && startRow + j < GridSize &&
                            startColumn + i >= 0 && startColumn + i < GridSize)
                        {
                            if (gridMatrix[startColumn + i, startRow + j] != 1)
                                gridMatrix[startColumn + i, startRow + j] = -1;
                        }
                    }
                    else
                    {
                        if (startRow + i >= 0 && startRow + i < GridSize &&
                            startColumn + j >= 0 && startColumn + j < GridSize)
                        {
                            if (gridMatrix[startColumn + j, startRow + i] != 1)
                                gridMatrix[startColumn + j, startRow + i] = -1;
                        }
                    }
                }
            }
        }

        private void PlaceShip(int startColumn, int startRow, int shipLength, bool isHorizontal)
        {
            if (!CanPlaceShip(startColumn, startRow, shipLength, isHorizontal))
            {
                MessageBox.Show("Nem helyezhető el itt a hajó!");
                return;
            }

            MarkOccupiedArea(startColumn, startRow, shipLength, isHorizontal);
            placedShips.Add(new Ship(startColumn, startRow, shipLength, isHorizontal));


            for (int i = 0; i < shipLength; i++)
            {
                if (isHorizontal)
                {
                    gridState[startRow, startColumn + i] = 1;
                }
                else
                {
                    gridState[startRow + i, startColumn] = 1;
                }
            }

            totalShipsPlaced++;
            CheckIfAllShipsPlaced();
        }

        private void CheckIfAllShipsPlaced()
        {
            if (totalShipsPlaced == 5)
            {
                FinishButtontemp.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void FinishButtontemp_Click(object sender, RoutedEventArgs e)
        {
            //SaveGridToFile();
            //string sor = "";
            //for (int i = 0; i < 10; i++)
            //{
            //    sor = "";
            //    for (global::System.Int32 j = 0; j < 10; j++)
            //    {
            //        sor += gridState[i, j];
            //    }
            //    MessageBox.Show(sor);
            //}


            string formattedMatrix = EgySorbaRendezes(gridState);

            // Mátrix elküldése a szervernek
            byte[] data = System.Text.Encoding.UTF8.GetBytes(formattedMatrix);
            _stream.Write(data, 0, data.Length);
            _stream.Flush();

            //asd
            MessageBox.Show("várakozás");
        }

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

        // Mentés a fájlba
        //private void SaveGridToFile()
        //{
        //    string filePath = "grid_state.txt"; // A fájl neve
        //    using (StreamWriter writer = new StreamWriter(filePath))
        //    {
        //        for (int row = 0; row < GridSize; row++)
        //        {
        //            for (int col = 0; col < GridSize; col++)
        //            {
        //                // 1, ha hajó van a mezőn, különben 0
        //                writer.Write(gridState[row, col] ? "1" : "0");
        //            }
        //            writer.WriteLine(); // Minden sor után új sor
        //        }
        //    }

        //    MessageBox.Show("A rács állapota elmentve: grid_state.txt", "Mentés kész");
        //}


        private bool CanPlaceShip(int startColumn, int startRow, int shipLength, bool isHorizontal)
        {
            if (isHorizontal)
            {
                if (startColumn + shipLength > GridSize) return false;
            }
            else
            {
                if (startRow + shipLength > GridSize) return false;
            }

            for (int i = 0; i < shipLength; i++)
            {
                if (isHorizontal)
                {
                    if (gridMatrix[startColumn + i, startRow] != 0) return false;
                }
                else
                {
                    if (gridMatrix[startColumn, startRow + i] != 0) return false;
                }
            }

            return true;
        }

        private bool IsCollision(int column, int row, int shipCells, bool isHorizontal)
        {
            foreach (var ship in placedShips)
            {
                if (isHorizontal)
                {
                    for (int i = 0; i < shipCells; i++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            for (int offsetY = -1; offsetY <= 1; offsetY++)
                            {
                                int checkColumn = column + i + offsetX;
                                int checkRow = row + offsetY;
                                if (checkColumn >= 0 && checkColumn < MatrixGrid.ColumnDefinitions.Count &&
                                    checkRow >= 0 && checkRow < MatrixGrid.RowDefinitions.Count)
                                {
                                    foreach (var placedShip in placedShips)
                                    {
                                        if ((placedShip.StartColumn == checkColumn && placedShip.StartRow == checkRow) ||
                                            (isHorizontal && placedShip.StartRow == row && placedShip.StartColumn + placedShip.Length - 1 == checkColumn) ||
                                            (!isHorizontal && placedShip.StartColumn == column && placedShip.StartRow + placedShip.Length - 1 == checkRow))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < shipCells; i++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            for (int offsetY = -1; offsetY <= 1; offsetY++)
                            {
                                int checkColumn = column + offsetX;
                                int checkRow = row + i + offsetY;

                                if (checkColumn >= 0 && checkColumn < MatrixGrid.ColumnDefinitions.Count &&
                                    checkRow >= 0 && checkRow < MatrixGrid.RowDefinitions.Count)
                                {
                                    foreach (var placedShip in placedShips)
                                    {
                                        if ((placedShip.StartColumn == checkColumn && placedShip.StartRow == checkRow) ||
                                            (isHorizontal && placedShip.StartRow == row && placedShip.StartColumn + placedShip.Length - 1 == checkColumn) ||
                                            (!isHorizontal && placedShip.StartColumn == column && placedShip.StartRow + placedShip.Length - 1 == checkRow))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (column > 0)
                    {
                        for (int i = 0; i < shipCells; i++)
                        {
                            int checkColumn = column - 1;
                            int checkRow = row + i;

                            if (checkColumn >= 0 && checkColumn < MatrixGrid.ColumnDefinitions.Count &&
                                checkRow >= 0 && checkRow < MatrixGrid.RowDefinitions.Count)
                            {
                                foreach (var placedShip in placedShips)
                                {
                                    if (placedShip.StartColumn == checkColumn && placedShip.StartRow == checkRow)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R)
            {
                _isHorizontal = !_isHorizontal;

                double centerX = DraggableGroup.ActualWidth / 2;
                double centerY = DraggableGroup.ActualHeight / 2;

                RotateTransform.CenterX = centerX;
                RotateTransform.CenterY = centerY;

                double newAngle = _isHorizontal ? 0.0 : 90.0;

                var rotateAnimation = new DoubleAnimation
                {
                    From = RotateTransform.Angle,
                    To = newAngle,
                    Duration = TimeSpan.FromSeconds(0.5)
                };

                RotateTransform.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);

                UpdateCanvasMargin(newAngle);
            }
        }

        private void UpdateCanvasMargin(double angle)
        {
            if (angle == 0.0)
            {
                DraggableGroup.Margin = new Thickness(0, 10, 400, 75);
            }
            else
            {
                DraggableGroup.Margin = new Thickness(-90, 10, 400, 75);
            }
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rectangle)
            {
                _draggedRectangle = rectangle;
                _dragStartPoint = e.GetPosition(DraggableGroup);

                DragDrop.DoDragDrop(rectangle, rectangle, DragDropEffects.Move);
            }
        }

        private void MatrixGrid_Drop(object sender, DragEventArgs e)
        {
            if (_draggedRectangle != null && e.Data.GetData(typeof(Rectangle)) is Rectangle rectangle)
            {
                // Pozíció a célrácson belül
                var dropPosition = e.GetPosition(MatrixGrid);

                int cellSizeX = (int)(MatrixGrid.ActualWidth / MatrixGrid.ColumnDefinitions.Count);
                int cellSizeY = (int)(MatrixGrid.ActualHeight / MatrixGrid.RowDefinitions.Count);

                int column = (int)(dropPosition.X / cellSizeX);
                int row = (int)(dropPosition.Y / cellSizeY);

                int shipCells = (int)Math.Round(rectangle.Width / 50.0);

                if (_isHorizontal)
                {
                    if (column + shipCells > MatrixGrid.ColumnDefinitions.Count || row >= MatrixGrid.RowDefinitions.Count)
                    {
                        MessageBox.Show("A hajó nem fér el a rácsban!");
                        return;
                    }
                }
                else
                {
                    if (row + shipCells > MatrixGrid.RowDefinitions.Count || column >= MatrixGrid.ColumnDefinitions.Count)
                    {
                        MessageBox.Show("A hajó nem fér el a rácsban!");
                        return;
                    }
                }

                // Ellenőrizzük, hogy elhelyezhető-e a hajó
                if (CanPlaceShip(column, row, shipCells, _isHorizontal))
                {
                    PlaceShip(column, row, shipCells, _isHorizontal);

                    for (int i = 0; i < shipCells; i++)
                    {
                        Border shipCell = new Border
                        {
                            Background = Brushes.Blue,
                            BorderBrush = Brushes.Black,
                            BorderThickness = new Thickness(0.5)
                        };

                        if (_isHorizontal)
                        {
                            Grid.SetColumn(shipCell, column + i);
                            Grid.SetRow(shipCell, row);
                        }
                        else
                        {
                            Grid.SetColumn(shipCell, column);
                            Grid.SetRow(shipCell, row + i);
                        }

                        MatrixGrid.Children.Add(shipCell);
                    }

                    DraggableGroup.Children.Remove(rectangle);
                    _draggedRectangle = null;
                }
                else
                {
                    MessageBox.Show("A hajó nem helyezhető el itt!");
                }
            }
        }
    }
}
