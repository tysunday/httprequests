using System;
using System.Data.SQLite;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using HtmlAgilityPack;
using Microsoft.Data.Sqlite;

namespace httprequests
{
    public partial class MainWindow : Window
    {
        private const string DatabaseName = "search_results.db";

        public MainWindow()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SQLiteConnection($"Data Source={DatabaseName};Version=3;"))
            {
                conn.Open();
                string sql = @"
                CREATE TABLE IF NOT EXISTS results (
                    id INTEGER PRIMARY KEY,
                    url TEXT,
                    snippet TEXT,
                    content TEXT
                )";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                StatusTextBlock.Text = "Please enter search text.";
                return;
            }

            string[] pagesToSearch = { "catalog/mototekhnika/kross_i_enduro_1/", "catalog/mototekhnika/s_probegom/" }; // Update with real paths
            string baseUrl = "https://chelyabinsk.drivemotors.ru/"; // Update with the actual base URL

            StatusTextBlock.Text = "Searching...";
            ResultsListBox.Items.Clear();

            foreach (var page in pagesToSearch)
            {
                string url = baseUrl + page;
                await SearchInPage(url, searchText);
            }

            StatusTextBlock.Text = "Search completed.";
        }

        private async Task SearchInPage(string url, string searchText)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetStringAsync(url);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(response);

                    var nodes = doc.DocumentNode.SelectNodes($"//*[contains(text(), '{searchText}')]");
                    if (nodes != null)
                    {
                        foreach (var node in nodes)
                        {
                            string snippet = node.InnerText.Length > 200 ? node.InnerText.Substring(0, 200) : node.InnerText;
                            SaveResult(url, snippet, response);
                            ResultsListBox.Items.Add($"Found on {url}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResultsListBox.Items.Add($"Error searching {url}: {ex.Message}");
            }
        }

        private void SaveResult(string url, string snippet, string content)
        {
            using (var conn = new SQLiteConnection($"Data Source={DatabaseName};Version=3;"))
            {
                conn.Open();
                string sql = "INSERT INTO results (url, snippet, content) VALUES (@url, @snippet, @content)";
                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.Parameters.AddWithValue("@snippet", snippet);
                    cmd.Parameters.AddWithValue("@content", content);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}


//База данных:

//    Её можно создать с помощью этого запроса и запустить программу.


//    CREATE TABLE search_results (
//    id INT PRIMARY KEY IDENTITY(1,1),
//    url NVARCHAR(2083) NOT NULL,
//    snippet NVARCHAR(MAX) NOT NULL,
//    content NVARCHAR(MAX) NOT NULL
//);
