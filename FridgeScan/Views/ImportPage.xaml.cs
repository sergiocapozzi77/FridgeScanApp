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
        if (services != null)
        {
            _vm = services.GetService<ImportViewModel>();
        }

        if (_vm == null)
        {
            // fallback (not ideal for real app)
            var emailService = services?.GetService<Services.EmailService>() ?? new Services.EmailService();
            var mainVm = services?.GetService<MainViewModel>() ?? new MainViewModel();
            _vm = new ImportViewModel(emailService, mainVm);
        }

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
