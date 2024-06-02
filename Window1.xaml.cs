using System;
using System.Collections.ObjectModel;
using System.Windows;
using MySql.Data.MySqlClient;

namespace egzaminas
{
    public partial class Window1 : Window
    {
        private string connectionString = "server=127.0.0.1;uid=root;pwd=;database=egz";
        public ObservableCollection<WebsiteEntry> WebsiteEntries { get; set; }

        public Window1(string username)
        {
            InitializeComponent();
            WebsiteEntries = new ObservableCollection<WebsiteEntry>();
            WebsiteListView.ItemsSource = WebsiteEntries;
            UsernameLabel.Content = username;
            LoadWebsiteEntries(username);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string website = WebsiteTextBox.Text;
            string password = PasswordTextBox.Text;
            string username = UsernameLabel.Content.ToString();

            if (!string.IsNullOrEmpty(website) && !string.IsNullOrEmpty(password))
            {
                WebsiteEntry newEntry = new WebsiteEntry { Website = website, Password = password };
                WebsiteEntries.Add(newEntry);
                WebsiteTextBox.Clear();
                PasswordTextBox.Clear();

                if (SaveToDatabase(newEntry, username))
                {
                    MessageBox.Show("Isaugota.");
                }
                else
                {
                    MessageBox.Show("Neisaugota.");
                }
            }
            else
            {
                MessageBox.Show("Iveskite svetainės pavadinima ir slaptažodį.");
            }
        }

        private bool SaveToDatabase(WebsiteEntry entry, string username)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO lentele (svetaines_pavadinimas, slaptazodis, vartotojo_vardas) VALUES (@website, @password, @username)";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@website", entry.Website);
                    cmd.Parameters.AddWithValue("@password", entry.Password);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving to database: " + ex.Message);
                return false;
            }
        }

        private void LoadWebsiteEntries(string username)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT svetaines_pavadinimas, slaptazodis FROM lentele WHERE vartotojo_vardas = @username";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        WebsiteEntries.Add(new WebsiteEntry
                        {
                            Website = reader.GetString("svetaines_pavadinimas"),
                            Password = reader.GetString("slaptazodis")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading website entries: " + ex.Message);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }

    public class WebsiteEntry
    {
        public string Website { get; set; }
        public string Password { get; set; }
    }
}