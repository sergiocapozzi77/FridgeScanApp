using BarcodeScanning;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using FridgeScan.Models;

namespace FridgeScan.Views;

public partial class BarcodeScannerPage : ContentPage
{

    private readonly BarcodeDrawable _drawable = new();
    private string lastBarcode = "";

    public BarcodeScannerPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        await Methods.AskForRequiredPermissionAsync();
        base.OnAppearing();

        Barcode.CameraEnabled = true;
        Graphics.Drawable = _drawable;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Barcode.CameraEnabled = false;
    }

    private async void CameraView_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
    {
        _drawable.barcodeResults = e.BarcodeResults;
        Graphics.Invalidate();
        if(e.BarcodeResults.Count > 0)
        {
            var barcode = e.BarcodeResults.First().DisplayValue;
            if(barcode != lastBarcode)
            {
                lastBarcode = barcode;

                var service = new OpenFoodFactsService();
                var product = await service.GetProductAsync(barcode);

                if (product == null)
                {
                    await Toast.Make("Product not found").Show();
                    return;
                }

                await ShowProductPanel(product);

            }
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnFlipCameraClicked(object sender, EventArgs e)
    {
        if (Barcode.CameraFacing == CameraFacing.Back)
            Barcode.CameraFacing = CameraFacing.Front;
        else
            Barcode.CameraFacing = CameraFacing.Back;
    }

    private void OnTorchClicked(object sender, EventArgs e)
    {
        if (Barcode.TorchOn)
            Barcode.TorchOn = false;
        else
            Barcode.TorchOn = true;
    }

    private class BarcodeDrawable : IDrawable
    {
        public IReadOnlySet<BarcodeResult>? barcodeResults;
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (barcodeResults is not null && barcodeResults.Count > 0)
            {
                canvas.StrokeSize = 15;
                canvas.StrokeColor = Colors.Red;
                var scale = 1 / canvas.DisplayScale;
                canvas.Scale(scale, scale);

                foreach (var barcode in barcodeResults)
                {
                    canvas.DrawRectangle(barcode.PreviewBoundingBox);
                }
            }
        }
    }

    ProductInfo currentProduct;

    private async Task ShowProductPanel(ProductInfo product)
    {
        ProductName.Text = product.Name;
        ProductImage.Source = product.ImageUrl;
        currentProduct = product;

        ProductPanel.IsVisible = true;

        // Fade + slide up
        await Task.WhenAll(
            ProductPanel.FadeTo(1, 250),
            ProductPanel.TranslateTo(0, 0, 250)
        );
    }

    private async Task HideProductPanel()
    {
        await Task.WhenAll(
            ProductPanel.FadeTo(0, 200),
            ProductPanel.TranslateTo(0, 20, 200)
        );

        ProductPanel.IsVisible = false;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        lastBarcode = "";
        await HideProductPanel();
    }

    private async void OnExpiryDateSelected(object sender, EventArgs e)
    {
        // Get selected date from the calendar
        DateTime? selectedDate = null;

        if (e is Syncfusion.Maui.Calendar.CalendarSelectionChangedEventArgs args)
        {
            if (args.NewValue is DateTime date)
                selectedDate = date;
            else if (args.NewValue is System.Collections.IList list && list.Count > 0)
                selectedDate = list[0] as DateTime?;
        }

        if (selectedDate.HasValue)
        {
            currentProduct.ExpiryDate = selectedDate.Value;
            WeakReferenceMessenger.Default.Send(new ProductMessage(currentProduct));
            lastBarcode = "";
            await HideProductPanel();
            await Toast.Make($"{currentProduct.Name} saved with expiry {selectedDate.Value:dd MMM yyyy}").Show();
        }
    }

    private async void OnSaveWithoutExpiryClicked(object sender, EventArgs e)
    {
        currentProduct.ExpiryDate = null;
        WeakReferenceMessenger.Default.Send(new ProductMessage(currentProduct));
        lastBarcode = "";
        await HideProductPanel();
        await Toast.Make($"{currentProduct.Name} saved without expiry").Show();
    }
}

public class ProductMessage : ValueChangedMessage<ProductInfo>
{
    public ProductMessage(ProductInfo value) : base(value)
    {
    }
}

