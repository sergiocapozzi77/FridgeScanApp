using System.Reflection;

namespace FridgeScan;

public static class Secrets
{
    public static string SyncfusionLicenseKey =>
        GetValue(nameof(SyncfusionLicenseKey));

    public static string AppWriteApiKey =>
        GetValue(nameof(AppWriteApiKey));

    private static readonly JsonElement _config = LoadConfiguration("secrets.json");

    private static string GetValue(string key)
    {
        if (_config.TryGetProperty(key, out var value))
            return value.GetString() ?? throw new NullReferenceException(key);

        throw new NullReferenceException(key);
    }

    private static JsonElement LoadConfiguration(string filePath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? assemblyName = assembly.GetName().Name;

        using var stream = FileSystem.OpenAppPackageFileAsync("secrets.json").Result;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEndAsync().Result;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

}