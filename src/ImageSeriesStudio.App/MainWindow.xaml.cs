using System.Windows;
using ImageSeriesStudio.App.ViewModels;

namespace ImageSeriesStudio.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
