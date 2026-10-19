
using System;
using System.Windows;
using CoffeeRuu.Data;

namespace CoffeeRuu
{
    public class DatabaseTest
    {
        public static void TestConnection()
        {
            try
            {
                using var db = App.CreateDbContext();

                bool connected = db.Database.CanConnect();

                if (connected)
                {
                    int roleCount = db.Roles.Count();

                    MessageBox.Show(
                        $"Ket noi database thanh cong!\nSo luong roles: {roleCount}",
                        "CoffeeRuu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Khong the ket noi database.",
                        "CoffeeRuu",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Loi ket noi database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
