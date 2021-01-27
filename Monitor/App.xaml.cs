using Monitor.ViewModels;
using Monitor.Views;
using System.Windows;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var viewModel = new MainViewModel();
            var view = new MainWindow(viewModel);

            view.ShowDialog();
        }
    }
}
