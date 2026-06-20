using System.Globalization;
using System.Windows.Data;

namespace ContentDeliveryStudio.App.Services;

public sealed class GalleryThumbnailPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string assetPath || string.IsNullOrWhiteSpace(assetPath))
        {
            return Binding.DoNothing;
        }

        return GalleryThumbnailCache.GetOrCreate(assetPath);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
