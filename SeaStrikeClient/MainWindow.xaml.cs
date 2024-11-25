using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SeaStrikeClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Kliens csatlakozása a szerverhez
                TcpClient client = new TcpClient("127.0.0.1", 5000); // IP-cím és port
                NetworkStream stream = client.GetStream();

                // Csatlakozási üzenet küldése
                byte[] data = Encoding.UTF8.GetBytes("join");
                await stream.WriteAsync(data, 0, data.Length);

                // Válasz fogadása
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Szerver válasza: {response}");
                });

                if (response == "ok")
                {
                    // Várakozás az "isactive" üzenetre
                    while (true)
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        if (response == "isactive")
                        {
                            // Aktív státusz visszaküldése
                            data = Encoding.UTF8.GetBytes("active");
                            await stream.WriteAsync(data, 0, data.Length);
                        }
                        else if (response == "start")
                        {
                            // Játék indítása, üzenet megjelenítése
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("A játék elkezdődött!");
                            });
                            break;
                        }
                    }
                }
                else if (response == "Server full")
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("A szerver tele van, nem tudsz csatlakozni.");
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Hiba történt: {ex.Message}");
                });
            }
        }
    }
}
