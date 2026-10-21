
using System;
using System.Configuration;
using CoffeeRuu.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeRuu
{
    public partial class App : System.Windows.Application
    {
        public static DbContextOptions<CoffeeRuuDbContext>
            CreateDbContextOptions()
        {
            string? connectionString =
                Environment.GetEnvironmentVariable(
                    "COFFEERUU_CONNECTION");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString =
                    ConfigurationManager
                        .ConnectionStrings["CoffeeRuuConnection"]
                        ?.ConnectionString;
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy connection string CoffeeRuuConnection. " +
                    "Hãy kiểm tra biến môi trường COFFEERUU_CONNECTION " +
                    "hoặc cấu hình ConnectionStrings trong App.config.");
            }

            var options =
                new DbContextOptionsBuilder<CoffeeRuuDbContext>();

            options.UseSqlServer(connectionString);

            return options.Options;
        }

        public static CoffeeRuuDbContext CreateDbContext()
        {
            return new CoffeeRuuDbContext(
                CreateDbContextOptions());
        }

        protected override void OnStartup(
            System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}
