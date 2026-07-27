using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ContentDeliveryStudio.App.ViewModels;

namespace ContentDeliveryStudio.App.Views;

public partial class ScientificDeliveryWorkspaceView : UserControl
{
    private ScientificDeliveryViewModel? _viewModel;

    public ScientificDeliveryWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachViewModel(e.NewValue);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext);
    }

    private void AttachViewModel(object? dataContext)
    {
        var next = (dataContext as WorkbenchTabViewModel)?.ScientificDelivery;
        if (ReferenceEquals(next, _viewModel))
        {
            return;
        }

        DetachViewModel();
        _viewModel = next;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RenderSelectedPreview();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScientificDeliveryViewModel.SelectedArtifact))
        {
            RenderSelectedPreview();
        }
    }

    private void RenderSelectedPreview()
    {
        var artifact = _viewModel?.SelectedArtifact;
        if (artifact?.IsPng != true)
        {
            PngPreview.Source = null;
            return;
        }

        using var stream = new MemoryStream(artifact.GetBytes(), writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        PngPreview.Source = image;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachViewModel();
    }

    private void DetachViewModel()
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }
}
