using System.Windows;
using ContentDeliveryStudio.App.ViewModels;

namespace ContentDeliveryStudio.App;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
