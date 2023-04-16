using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Data;

namespace LittleBasket
{
    /// <summary>
    /// Логика взаимодействия для Catalogue.xaml
    /// </summary>
    public partial class Catalogue : Window
    {
        private MySqlConnection sqlConnection = null;

        private List<string[]> rows = null;
        private List<string[]> filtredList = null;

        public Catalogue()
        {
            InitializeComponent();
            sqlConnection = new MySqlConnection(ConfigurationManager.ConnectionStrings["connection"].ConnectionString);
            sqlConnection.Open();
        }

        private void Back(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Add(object sender, RoutedEventArgs e)
        {
            if(!rows.Exists(x => x[0] == textBoxGood.Text))
            {
                MySqlCommand addCommand = new MySqlCommand(
                $"INSERT INTO goods(name, is_shown) VALUES(@name, false)",
                sqlConnection);

                addCommand.Parameters.AddWithValue("name", textBoxGood.Text);
                addCommand.ExecuteNonQuery();
            }
            else {
                MessageBox.Show("Такой продукт уже есть");
            }
            LoadListGoods();
        }

        private void ListGoodsLoaded(object sender, RoutedEventArgs e)
        {
            LoadListGoods();
        }

        private void RefreshList(List<string[]> list)
        {
            listGoods.Items.Clear();
            foreach (string[] product  in list)
            {
                listGoods.Items.Add(new ListViewItem()
                {
                    Content = product
                });
            }
        }

        private void TextBoxFiltredTextChanged(object sender, TextChangedEventArgs e)
        {
            filtredList = rows.Where((x) =>
            x[0].ToLower().Contains(textBoxFiltred.Text.ToLower())).ToList();

            RefreshList(filtredList);
        }

        private void LoadListGoods()
        {
            MySqlDataReader reader = null;
            listGoods.Items.Clear();

            rows = new List<string[]>();

            try
            {
                MySqlCommand mySqlCommand = new MySqlCommand("SELECT name, is_shown FROM goods",
                    sqlConnection);
                reader = mySqlCommand.ExecuteReader();

                ListViewItem item = null;

                while (reader.Read())
                {
                    string[] row = new string[]
                    {
                        Convert.ToString(reader["name"]),
                        Convert.ToString(reader["is_shown"])
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

        private void UpdateProductShown(bool isShown, CheckBox checkBox)
        {
            var data = (string[])checkBox.DataContext;
            MySqlCommand mySqlCommand = new MySqlCommand($"UPDATE goods SET is_shown = @is_shown WHERE name = @name",
            sqlConnection);

            mySqlCommand.Parameters.AddWithValue("name", data[0]);
            mySqlCommand.Parameters.AddWithValue("is_shown", isShown);
            mySqlCommand.ExecuteNonQuery();

            ((MainWindow)Owner).LoadGoodsInStock();
        }

        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            UpdateProductShown(false, (CheckBox)sender);
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            UpdateProductShown(true, (CheckBox)sender);
        }
    }
}
