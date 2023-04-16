using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Configuration;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text.RegularExpressions;

namespace LittleBasket
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MySqlConnection sqlConnection = null;
        private List<Product> rows = null;
        private int historyId = 0;

        public MainWindow()
        {
            InitializeComponent();
            sqlConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["connection"].ConnectionString);
            sqlConnection.Open();
            CreateTables();
        }

        private void CreateTables()
        {
            MySqlCommand mySqlCommand = new MySqlCommand(
                "CREATE TABLE IF NOT EXISTS goods (id int(11) NOT NULL AUTO_INCREMENT, name varchar(50) DEFAULT NULL, is_shown tinyint(1) DEFAULT NULL,  PRIMARY KEY (id));",
                sqlConnection);
            mySqlCommand.ExecuteNonQuery();

            mySqlCommand = new MySqlCommand(
                "CREATE TABLE IF NOT EXISTS history (id int(11) NOT NULL AUTO_INCREMENT, purchase_date date DEFAULT NULL, PRIMARY KEY (id));",
                sqlConnection);
            mySqlCommand.ExecuteNonQuery();

            mySqlCommand = new MySqlCommand(
                "CREATE TABLE IF NOT EXISTS history_info (id int(11) NOT NULL AUTO_INCREMENT, history_id int(11) DEFAULT NULL, product_id int(11) DEFAULT NULL, price int(11) DEFAULT NULL, count int(11) DEFAULT NULL, PRIMARY KEY (id))",
                sqlConnection);
            mySqlCommand.ExecuteNonQuery();
                
        }

        private void OpenWindow(object sender, RoutedEventArgs e)
        {
            Catalogue catalogue = new Catalogue();

            catalogue.Owner = this;
            catalogue.Show();           
        }

        private void RefreshList(List<Product> list)
        {
            listGoodsInStock.Items.Clear();
            foreach (Product product in list)
            {
                listGoodsInStock.Items.Add(new ListBoxItem()
                {
                    Content = product
                });
            }
        }

        private void ListGoodsInStockLoaded(object sender, RoutedEventArgs e)
        {
            LoadGoodsInStock();
        }

        public void LoadGoodsInStock()
        {
            MySqlDataReader reader = null;
            listGoodsInStock.Items.Clear();

            rows = new List<Product>();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand("SELECT id, name FROM goods WHERE is_shown = true",
                    sqlConnection);
                reader = mySqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    Product row = new Product()
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = Convert.ToString(reader["name"])
                    };

                    rows.Add(row);
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            finally
            {
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }
            }
            RefreshList(rows);
        }

        public void LimitNumber(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]+");
            e.Handled = re.IsMatch(e.Text);
        }

        public void ChangeTotalPrice(object sender, TextChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            Grid grid = (Grid)tb.Parent;

            var label = (Label)grid.Children.Cast<UIElement>().First(x => x.Uid == "sum");
            var textBoxCount = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "count");
            var textBoxPrice = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "price");

            label.Content = $"общее {(textBoxCount.Text == String.Empty || textBoxPrice.Text == String.Empty ? 0 : Convert.ToInt32(textBoxCount.Text) * Convert.ToInt32(textBoxPrice.Text) )}";
        }

        private void ClickToAddBuy(object sender, RoutedEventArgs e)
        {
            if (border.IsEnabled)
            {
                ListBoxItem item = (ListBoxItem)listGoodsInStock.SelectedItem;
                AddGroceryItem((Product)item.Content);
            }
        }

        private void ClickToReset(object sender, RoutedEventArgs e)
        {
            if (border.IsEnabled)
            {
                border.IsEnabled = false;
            }
            listGrocery.Items.Clear();
        }

        private void ClickToSave(object sender, RoutedEventArgs e)
        {
            if (historyId == 0)
            {
                if (listGrocery.Items.Count == 0)
                {
                    MessageBox.Show("Не выбраны товары");
                    return;
                }

                if (dateHistory.Text == string.Empty)
                {
                    MessageBox.Show("Не выбрана дата");
                    return;
                }

                MySqlCommand addDate = new MySqlCommand(
                    $"INSERT INTO history(purchase_date) VALUES(@purchase_date)",
                    sqlConnection);
                addDate.Parameters.AddWithValue("purchase_date", Convert.ToDateTime(dateHistory.Text));
                addDate.ExecuteNonQuery();

                MySqlCommand getHistoryId = new MySqlCommand(
                    $"SELECT MAX(id) FROM history",
                    sqlConnection);
                int historyId_ = (int)getHistoryId.ExecuteScalar();

                foreach (ListBoxItem item in listGrocery.Items)
                {
                    Grid grid = (Grid)((StackPanel)item.Content).Children[0];
                    var textBoxCount = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "count");
                    var textBoxPrice = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "price");
                    var product = (Product)((ContentControl)grid.Children.Cast<UIElement>().First(x => x.Uid == "product")).Content;

                    MySqlCommand addToHistory = new MySqlCommand(
                    $"INSERT INTO history_info(history_id, product_id, price, count) VALUES(@history_id, @product_id, @price, @count)",
                    sqlConnection);
                    addToHistory.Parameters.AddWithValue("history_id", historyId_);
                    addToHistory.Parameters.AddWithValue("product_id", product.Id);
                    addToHistory.Parameters.AddWithValue("price", Convert.ToInt32(textBoxPrice.Text == string.Empty ? 0 : textBoxPrice.Text));
                    addToHistory.Parameters.AddWithValue("count", Convert.ToInt32(textBoxCount.Text == string.Empty ? 0 : textBoxCount.Text));
                    addToHistory.ExecuteNonQuery();
                }
            }
            else
            {
                foreach (ListBoxItem item in listGrocery.Items)
                {
                    Grid grid = (Grid)((StackPanel)item.Content).Children[0];
                    var textBoxCount = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "count");
                    var textBoxPrice = (TextBox)grid.Children.Cast<UIElement>().First(x => x.Uid == "price");
                    var product = (Product)((ContentControl)grid.Children.Cast<UIElement>().First(x => x.Uid == "product")).Content;

                    MySqlCommand addToHistory = new MySqlCommand(
                    $"UPDATE history_info SET price = @price, count = @count WHERE history_id = @history_id AND product_id = @product_id",
                    sqlConnection);
                    addToHistory.Parameters.AddWithValue("price", Convert.ToInt32(textBoxPrice.Text == string.Empty ? 0 : textBoxPrice.Text));
                    addToHistory.Parameters.AddWithValue("count", Convert.ToInt32(textBoxCount.Text == string.Empty ? 0 : textBoxCount.Text));
                    addToHistory.Parameters.AddWithValue("history_id", historyId);
                    addToHistory.Parameters.AddWithValue("product_id", product.Id);
                    addToHistory.ExecuteNonQuery();
                }
            }

            LoadHistory();
            listGrocery.Items.Clear();
            border.IsEnabled = false;
            historyId = 0;
        }

        private void ClickToCreateNewHistory(object sender, RoutedEventArgs e)
        {
            if (!border.IsEnabled)
            {
                border.IsEnabled = true;
            }

            listGrocery.Items.Clear();
            historyId = 0;
        }

        private void RefreshList(List<Purchase> list)
        {
            listHistory.Items.Clear();
            foreach (Purchase product in list)
            {
                listHistory.Items.Add(new ListBoxItem()
                {
                    Content = product
                });
            }
        }

        public void LoadHistory()
        {
            MySqlDataReader reader = null;
            List<Purchase> historyRows = new List<Purchase>();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand("SELECT id, purchase_date FROM history ORDER BY 2",
                    sqlConnection);

                reader = mySqlCommand.ExecuteReader();

                while (reader.Read())
                {
                    Purchase row = new Purchase()
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Date = Convert.ToDateTime(reader["purchase_date"])
                    };

                    historyRows.Add(row);
                }
                reader.Close();

                foreach (Purchase purchase in historyRows)
                {
                    MySqlCommand getCommand = new MySqlCommand("SELECT h.id, product_id, g.name, price, count FROM history_info h JOIN goods g ON h.product_id = g.id WHERE history_id = @id",
                    sqlConnection);
                    getCommand.Parameters.AddWithValue("id", purchase.Id);

                    reader = getCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        ProductInBasket row = new ProductInBasket()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Product = new Product()
                            {
                                Id = Convert.ToInt32(reader["product_id"]),
                                Name = Convert.ToString(reader["name"])
                            },
                            Count = Convert.ToInt32(reader["count"]),
                            Price = Convert.ToInt32(reader["price"])
                        };

                        purchase.Products.Add(row);
                    }
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            finally
            {
                if (reader != null && !reader.IsClosed)
                {
                    reader.Close();
                }
            }
            RefreshList(historyRows);
        }

        private void ListHistoryLoaded(object sender, RoutedEventArgs e)
        {
            LoadHistory();
        }

        private void ListHistorySelected(object sender, SelectionChangedEventArgs e)
        {
            historyId = e.AddedItems.Count > 0 ? ((Purchase)((ListBoxItem)e.AddedItems[0])?.Content).Id : 0;
            border.IsEnabled = true;
            listGrocery.Items.Clear();

            if (historyId > 0)
            {
                List<ProductInBasket> products = new List<ProductInBasket>();
                MySqlDataReader reader = null;
                try
                {
                    MySqlCommand mySqlCommand = new MySqlCommand("SELECT h.id, product_id, g.name, price, count FROM history_info h JOIN goods g ON h.product_id = g.id WHERE history_id = @id",
                        sqlConnection);
                    mySqlCommand.Parameters.AddWithValue("id", historyId);

                    reader = mySqlCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        ProductInBasket product = new ProductInBasket()
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Product = new Product()
                            {
                                Id = Convert.ToInt32(reader["product_id"]),
                                Name = Convert.ToString(reader["name"])
                            },
                            Count = Convert.ToInt32(reader["count"]),
                            Price = Convert.ToInt32(reader["price"])
                        };

                        products.Add(product);
                    }

                    foreach (var product in products)
                    {
                        AddGroceryItem(product.Product, product.Count, product.Price);
                    }
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                finally
                {
                    if (reader != null && !reader.IsClosed)
                    {
                        reader.Close();
                    }
                }
            }
        }

        private void AddGroceryItem(Product product_, int count = 0, int price = 0)
        {
            Grid grid = new Grid();

            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            grid.ColumnDefinitions.Add(colDef1);
            grid.ColumnDefinitions.Add(colDef2);

            RowDefinition rowDef1 = new RowDefinition();
            RowDefinition rowDef2 = new RowDefinition();
            grid.RowDefinitions.Add(rowDef1);
            grid.RowDefinitions.Add(rowDef2);

            ContentControl product = new ContentControl() { Uid = "product", Content = product_, VerticalAlignment = VerticalAlignment.Center };
            TextBox textBoxCount = new TextBox() { Uid = "count", FontSize = 12, MaxLength = 2, HorizontalAlignment = HorizontalAlignment.Center, Width = 40, Height = 20, TextAlignment = TextAlignment.Center, Text = count.ToString() };
            textBoxCount.PreviewTextInput += LimitNumber;
            textBoxCount.TextChanged += ChangeTotalPrice;
            TextBox textBoxPrice = new TextBox() { Uid = "price", FontSize = 12, MaxLength = 4, Margin = new Thickness(35, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left, Width = 40, Height = 20, TextAlignment = TextAlignment.Center, Text = price.ToString() };
            textBoxPrice.PreviewTextInput += LimitNumber;
            textBoxPrice.TextChanged += ChangeTotalPrice;
            Label labelCount = new Label() { Content = "шт", Margin = new Thickness(55, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            Label labelPrice = new Label() { Content = "цена:", HorizontalAlignment = HorizontalAlignment.Left };
            Label labelRub = new Label() { Content = "р", Margin = new Thickness(70, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            Label labelSum = new Label() { Uid = "sum", Content = "общее", HorizontalAlignment = HorizontalAlignment.Right };

            Grid.SetColumnSpan(product, 1);
            Grid.SetRow(product, 0);
            Grid.SetColumnSpan(textBoxCount, 2);
            Grid.SetRow(textBoxCount, 0);
            Grid.SetColumnSpan(labelCount, 2);
            Grid.SetRow(labelCount, 0);
            Grid.SetColumnSpan(labelPrice, 1);
            Grid.SetRow(labelPrice, 1);
            Grid.SetColumnSpan(textBoxPrice, 1);
            Grid.SetRow(textBoxPrice, 1);
            Grid.SetColumnSpan(labelRub, 1);
            Grid.SetRow(labelRub, 1);
            Grid.SetColumnSpan(labelSum, 2);
            Grid.SetRow(labelSum, 1);

            grid.Children.Add(product);
            grid.Children.Add(textBoxCount);
            grid.Children.Add(labelCount);
            grid.Children.Add(labelPrice);
            grid.Children.Add(textBoxPrice);
            grid.Children.Add(labelRub);
            grid.Children.Add(labelSum);

            StackPanel content = new StackPanel() { Width = 220 };
            content.Children.Add(grid);

            listGrocery.Items.Add(new ListBoxItem()
            {
                Content = content
            });
        }
    }
}
