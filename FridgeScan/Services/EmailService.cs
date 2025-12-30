using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace FridgeScan.Services;

public class EmailService
{
    // NOTE: You must register an app in Azure AD and set these values.
    // For testing, set ClientId and TenantId to your test app registration.
    private const string ClientId = "f6c0b2ba-e930-44c0-97e3-00ca28a3cdf3";
    private const string TenantId = "e10cba06-a229-45c6-ac9b-8c9dd8c87267"; // or your tenant id
    private readonly string[] Scopes = new[] { "User.Read", "Mail.Read" };

    private IPublicClientApplication? _pca;

    public EmailService()
    {
#if DEBUG
        // Enable verbose MSAL logging in DEBUG to help diagnose interactive flow issues
        _pca = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "common")
            .WithLogging((level, message, containsPii) => Debug.WriteLine($"MSAL:{level} {message} PII:{containsPii}"), Microsoft.Identity.Client.LogLevel.Verbose)
#else
        _pca = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, "common")
#endif
#if ANDROID
      .WithParentActivityOrWindow(() => Platform.CurrentActivity) // This is needed for Android
            .WithRedirectUri("msalf6c0b2ba-e930-44c0-97e3-00ca28a3cdf3://auth")
#else
            .WithRedirectUri("http://localhost")
#endif
            .Build();
    }

    public record EmailMessage(string Subject, string Body);

    public async Task<IList<EmailMessage>> FetchPurchaseEmailsAsync(CancellationToken ct = default)
    {
      
        var list = new List<EmailMessage>();
      

        return list;
    }
}
