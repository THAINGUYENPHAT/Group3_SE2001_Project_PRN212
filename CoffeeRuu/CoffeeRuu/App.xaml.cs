
using System;
using System.Windows;
using CoffeeRuu.Data;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace CoffeeRuu
{
    public partial class App : Application
    {
        public static CoffeeRuuDbContext CreateDbContext()
        {
            var connectionString =
                ConfigurationManager.ConnectionStrings["CoffeeRuuConnection"]
                    .ConnectionString;

            var options = new DbContextOptionsBuilder<CoffeeRuuDbContext>();
            options.UseSqlServer(connectionString);

            return new CoffeeRuuDbContext(options.Options);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

        }
    }
}
