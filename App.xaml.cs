using HillsCafeManagement.Services;
using System.Configuration;
using System.Data;
using System.Windows;

namespace HillsCafeManagement;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static DatabaseService DatabaseServices { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize the database service here
        DatabaseServices = new DatabaseService();
    }

}

