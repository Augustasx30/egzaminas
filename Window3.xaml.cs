using System;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace egzaminas
{
    public partial class Window3 : Window
    {
        private string username;
        private string connectionString = "server=127.0.0.1;uid=root;pwd=;database=egz";
        private const string salt = "valuek"; 

        public Window3(string username)
        {
            InitializeComponent();
            this.username = username;
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = OldPasswordBox.Password;
            string newPassword = NewPasswordBox.Password;
            string repeatPassword = RepeatPasswordBox.Password;

            if (newPassword != repeatPassword)
            {
                MessageBox.Show("Naujas slaptažodis nesutampa.");
                return;
            }

            if (ValidateLogin(username, oldPassword))
            {
                UpdatePassword(username, newPassword);
                MessageBox.Show("Pakeista :)");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Senas slaptažodis netinka.");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private bool ValidateLogin(string username, string password)
        {
            bool isValid = false;
            string query = "SELECT password_hash FROM duo WHERE username = @username";

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);

                try
                {
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedHash = reader.GetString("password_hash");
                        string inputHash = HashPassword(password);

                        if (inputHash == storedHash)
                        {
                            isValid = true;
                        }
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            return isValid;
        }

        private void UpdatePassword(string username, string newPassword)
        {
            string query = "UPDATE duo SET password_hash = @password WHERE username = @username";
            string hashedPassword = HashPassword(newPassword);

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", hashedPassword);

                try
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string saltedPassword = password + salt;
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

 
    }
}
