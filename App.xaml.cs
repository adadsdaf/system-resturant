using Microsoft.Extensions.Configuration;
using RestaurantMS.Desktop.Models;
using RestaurantMS.Desktop.Services;
using RestaurantMS.Desktop.Views;
using RestaurantMS.Desktop.Views.Owner;
using System.IO;
using System.Windows;

namespace RestaurantMS.Desktop;

public partial class App : Application
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static string ConnectionString { get; private set; } = "";
    public static CurrentUser? CurrentUser { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Configuration = builder.Build();
        ConnectionString = Configuration.GetConnectionString("DefaultConnection")
                           ?? throw new InvalidOperationException("Connection string not found!");

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show($"خطأ غير متوقع:\n{ex.Exception.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        if (OwnerCredentialsManager.IsFirstRun())
        {
            var ownerWin = new OwnerPortalWindow(isFirstRun: true);
            ownerWin.Show();
        }
        else
        {
            var login = new LoginWindow();
            login.Show();
        }
    }
}
