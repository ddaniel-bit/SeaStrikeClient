using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeaStrikeClient
{
    public partial class LayoutEditor : Window
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        public LayoutEditor(TcpClient client, NetworkStream stream)
        {
            InitializeComponent();
            _client = client;
            _stream = stream;

            // Indítsd el a kommunikáció fogadását az új ablakban
            _ = Task.Run(ReceiveMessagesAsync);
        }

        private async Task ReceiveMessagesAsync()
        {
            try
            {
                byte[] buffer = new byte[1024];

                while (_client.Connected)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Dispatcher.Invoke(() => MessageBox.Show($"Válasz a szervertől: {response}"));
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Hiba a válasz fogadása közben: {ex.Message}"));
            }
        }

        private async void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string message = "example_message";
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length);
                MessageBox.Show("Üzenet elküldve a szervernek.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba üzenet küldése közben: {ex.Message}");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}
