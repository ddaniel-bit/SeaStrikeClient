using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeaStrikeClient
{
    public partial class MainWindow : Window
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            const string serverIp = "127.0.0.1";
            const int port = 8080;
            btnJoin.Visibility = Visibility.Collapsed;

            try
            {
                // Kapcsolódás a szerverhez
                _client = new TcpClient(serverIp, port);
                _stream = _client.GetStream();

                // "join" üzenet küldése
                string message = "join";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);

                // Válaszok fogadása
                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (_client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Dispatcher.Invoke(() => MessageBox.Show("Kapcsolat megszakadt a szerverrel."));
                        break;
                    }

                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (response == "ok")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            lblWaiting.Visibility = Visibility.Visible;
                        });
                    }
                    else if (response == "start")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            LayoutEditortemp layoutEditor = new LayoutEditortemp(_client, _stream);
                            layoutEditor.Show();
                            this.Close(); // Bezárja a főablakot
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Hiba a válasz fogadása közben: {ex.Message}"));
            }
        }









        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}
