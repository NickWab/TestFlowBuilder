using System.Windows;
using ATECore.TestFlowBuilder.Services;
using ATECore.TestFlowBuilder.ViewModels;
using ATECore.TestFlowBuilder.Views;

namespace ATECore.TestFlowBuilder;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var options = AppOptions.Parse(e.Args);
        new MainWindow { DataContext = new MainViewModel(options) }.Show();
    }
}
