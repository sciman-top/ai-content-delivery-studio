namespace ImageSeriesStudio.Application.Delivery;

public sealed class DeliveryApplicationService
{
    private readonly IDeliveryPackageWriter? _deliveryPackageWriter;

    public DeliveryApplicationService(IDeliveryPackageWriter? deliveryPackageWriter)
    {
        _deliveryPackageWriter = deliveryPackageWriter;
    }

    public async Task<DeliveryExportResult> ExportDeliveryPackageAsync(
        DeliveryExportRequest request,
        CancellationToken cancellationToken)
    {
        if (_deliveryPackageWriter is null)
        {
            throw new InvalidOperationException("Delivery package writer is not registered.");
        }

        return await _deliveryPackageWriter.WriteAsync(request, cancellationToken);
    }
}
