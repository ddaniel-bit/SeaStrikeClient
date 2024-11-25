using System;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace SeaStrikeClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = "127.0.0.1"; // Szerver IP-címe
            int port = 12345; // Szerver portja

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.Connect(serverIp, port); // Kapcsolódás a szerverhez
                    NetworkStream stream = client.GetStream();

                    // Üzenet küldése
                    string message = "Hello, Server! This is the client.";
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    stream.Write(data, 0, data.Length);
                    MessageBox.Show("Kapcsolódás sikeres és üzenet elküldve a szervernek!");

                    // Válasz fogadása
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    MessageBox.Show($"Válasz a szervertől: {response}");
                }
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Nem sikerült csatlakozni a szerverhez: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba történt: {ex.Message}");
            }
        }
    }
}
