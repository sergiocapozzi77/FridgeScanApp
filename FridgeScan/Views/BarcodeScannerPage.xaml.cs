namespace FridgeScan.Views;


using Camera.MAUI;
using Camera.MAUI.ZXing;
using Camera.MAUI.ZXingHelper;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

public partial class BarcodeScannerPage : ContentPage
{
    public BarcodeScannerPage()
    {
        InitializeComponent();
        cameraView.CamerasLoaded += CameraView_CamerasLoaded;
        cameraView.BarcodeDetected += CameraView_BarcodeDetected;
        cameraView.BarCodeDecoder = new ZXingBarcodeDecoder();
        cameraView.BarCodeOptions = new BarcodeDecodeOptions
        {
            AutoRotate = true,
           
            PossibleFormats = { BarcodeFormat.All_1D },
            ReadMultipleCodes = false,
            TryHarder = true,
            TryInverted = true
        };
        cameraView.BarCodeDetectionFrameRate = 10;
        cameraView.BarCodeDetectionMaxThreads = 5;
        cameraView.ControlBarcodeResultDuplicate = true;
        cameraView.BarCodeDetectionEnabled = true;
    }


    private async void CameraView_BarcodeDetected(object sender, Camera.MAUI.ZXingHelper.BarcodeEventArgs args)
    {
        foreach (var result in args.Result)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Toast.Make(result.Text, ToastDuration.Long).Show();
            });
        }
    }

    private void CameraView_CamerasLoaded(object sender, EventArgs e)
    {
        if (cameraView.NumCamerasDetected > 0)
        {
            if (cameraView.NumMicrophonesDetected > 0)
                cameraView.Microphone = cameraView.Microphones.First();
            cameraView.Camera = cameraView.Cameras.First();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (await cameraView.StartCameraAsync() == CameraResult.Success)
                {
                   
                }
            });
        }
    }

    private async void ZxingCameraViewOnBarcodeDetected(object sender, BarcodeEventArgs args)
    {
     
    }
}
