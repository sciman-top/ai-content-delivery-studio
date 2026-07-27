using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ContentDeliveryStudio.App.ViewModels;

namespace ContentDeliveryStudio.App.Views;

public partial class ScientificRenderReviewWorkspaceView : UserControl
{
    private ScientificRenderReviewViewModel? _viewModel;

    public ScientificRenderReviewWorkspaceView()
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
        var next = (dataContext as WorkbenchTabViewModel)?.ScientificRenderReview;
        if (ReferenceEquals(next, _viewModel))
        {
            return;
        }

        DetachViewModel();
        _viewModel = next;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            RenderSvg();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ScientificRenderReviewViewModel.ZoomScale))
        {
            RenderSvg();
        }
    }

    private void RenderSvg()
    {
        if (_viewModel is null)
        {
            return;
        }

        var zoomPercent = (_viewModel.ZoomScale * 100)
            .ToString("0", CultureInfo.InvariantCulture);
        var html = $$"""
            <!doctype html>
            <html>
            <head>
              <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'">
              <style>html,body{margin:0;padding:0;background:#fff;overflow:auto}#canvas{zoom:{{zoomPercent}}%}</style>
            </head>
            <body><div id="canvas">{{_viewModel.SvgDocument}}</div></body>
            </html>
            """;
        SvgBrowser.NavigateToString(html);
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
