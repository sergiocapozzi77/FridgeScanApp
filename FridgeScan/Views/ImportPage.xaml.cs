using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeScan.Views;

public partial class ImportPage : ContentPage
{
    private ImportViewModel? _vm;

    public ImportPage()
    {
        InitializeComponent();

        // Try to resolve viewmodel from the MAUI service provider
        var services = Application.Current?.Handler?.MauiContext?.Services;
        _vm = services.GetService<ImportViewModel>();

        BindingContext = _vm;
    }

    private async void OnImportClicked(object sender, EventArgs e)
    {
        try
        {
            var statusLabel = this.FindByName<Label>("StatusLabel");
            if (statusLabel != null) statusLabel.Text = "Importing...";
            if (_vm != null)
                await _vm.ImportFromEmailsAsync();
            if (statusLabel != null) statusLabel.Text = "Import completed.";
        }
        catch (Exception ex)
        {
            var statusLabel = this.FindByName<Label>("StatusLabel");
            if (statusLabel != null) statusLabel.Text = "Import failed: " + ex.Message;
        }
    }
}
