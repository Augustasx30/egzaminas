using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using MySql.Data.MySqlClient;

namespace egzaminas
{
    public partial class MainWindow : Window
    {
        private string connectionString = "server=127.0.0.1;uid=root;pwd=;database=egz";
        private const string charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int maxPasswordLength = 5;
        private volatile bool passwordFound = false;
        private string result = null;
        private object lockObject = new object();
        private Stopwatch stopwatch = new Stopwatch();
        private const string salt = "valuek";
       
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            string password = PasswordBox.Password;

            if (ValidateLogin(username, password))
            {
                Window1 window1 = new Window1(username);
                window1.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Neteisingas pavadinimas arba slaptažodis.");
            }
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
                        if (storedHash == inputHash)
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

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Window2 window2 = new Window2();
            window2.Show();
            this.Close();
        }

        private async void BruteForceButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Įveskite username");
                return;
            }

            string hash = GetStoredPasswordHash(username);
            if (hash == null)
            {
                MessageBox.Show("Vardas nerastas.");
                return;
            }

            if (!int.TryParse(ThreadsTextBox.Text, out int threadCount) || threadCount <= 0)
            {
                MessageBox.Show("Įveskite tinkamą thread skaičių.");
                return;
            }

            passwordFound = false;
            result = null;

            stopwatch.Reset();
            stopwatch.Start();

            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentAttemptTextBlock.Text = "Brute force pradėta: " + DateTime.Now.ToString("HH:mm:ss");
            });

            await Task.Run(() => BruteForce(hash, threadCount));

            stopwatch.Stop();

            if (result != null)
            {
                MessageBox.Show($"Slaptažodis rastas: {result}\nUžtruko: {stopwatch.Elapsed}");
            }
            else
            {
                MessageBox.Show($"Slaptažodis nerastas.\nUžtruko: {stopwatch.Elapsed}");
            }
        }

        private string GetStoredPasswordHash(string username)
        {
            string hash = null;
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
                        hash = reader.GetString("password_hash");
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            return hash;
        }

        private void BruteForce(string targetHash, int threadCount)
        {
            Parallel.For(0, threadCount, new ParallelOptions { MaxDegreeOfParallelism = threadCount }, i =>
            {
                int totalChars = charset.Length;
                int segmentSize = (totalChars + threadCount - 1) / threadCount; 
                int start = i * segmentSize;
                int end = Math.Min(start + segmentSize, totalChars);

                BruteForceSegment(targetHash, start, end);
            });
        }

        private void BruteForceSegment(string targetHash, int start, int end)
        {
            for (int length = 1; length <= maxPasswordLength; length++)
            {
                if (passwordFound) return;

                BruteForceRecursive("", targetHash, length, start, end);
            }
        }

        private void BruteForceRecursive(string current, string targetHash, int maxLength, int start, int end)
        {
            if (passwordFound) return;

            if (current.Length == maxLength)
            {
                string hashed = HashPassword(current);
                if (hashed == targetHash)
                {
                    lock (lockObject)
                    {
                        if (!passwordFound)
                        {
                            result = current;
                            passwordFound = true;
                        }
                    }
                }
                return;
            }

            for (int i = start; i < end; i++)
            {
                if (passwordFound) break;

                string attempt = current + charset[i];

                if (current.Length == maxLength - 1)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentAttemptTextBlock.Text = "Bandymas: " + attempt;
                    }, DispatcherPriority.Background);
                }

                BruteForceRecursive(attempt, targetHash, maxLength, start, end);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text;

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Įveskite username.");
                return;
            }

            Window3 window3 = new Window3(username);
            window3.Show();
            this.Close();
        }
    }
}
