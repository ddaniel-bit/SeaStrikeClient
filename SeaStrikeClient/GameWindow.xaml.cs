using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeaStrikeClient
{
    public partial class GameWindow : Window
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private int[,] _gridMatrix;
        private Thread _listenThread;  // Hozzáadtunk egy szálat a szerver figyeléséhez

        public GameWindow(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            _client = client;
            _stream = stream;

            // Indítsunk el egy külön szálat a szerver figyelésére
            _listenThread = new Thread(ListenToServer);
            _listenThread.IsBackground = true; // Hogy ha az ablak bezárul, a szál is leálljon
            _listenThread.Start();
            SendTextMessage();
        }
        public async void SendTextMessage()
        {
            string message = "alive";
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
        }
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

                        if (message == "yourturn")
                        {
                            Dispatcher.Invoke(() => // A UI szálon történő hívás szükséges
                            {
                                MessageBox.Show("en");
                            });
                        } else if (message == "test")
                        {
                            MessageBox.Show("test");
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
