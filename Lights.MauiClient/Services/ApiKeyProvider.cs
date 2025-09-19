
namespace Lights.MauiClient.Services;

/// <summary>
/// Provides the API key for accessing AI services.
/// <code>
/// Use Control Panel → System → Advanced system settings → Environment Variables → New
/// OR
/// Powershell (run as Administrator) if you want to ser at the machine level:
/// [System.Environment]::SetEnvironmentVariable("MY_AI_API_KEY", "your key goes here", "User")
/// </code> 
/// </summary>
public static class ApiKeyProvider
{
    public static async Task<string> GetApiKeyAsync()
    {
#if WINDOWS || MACCATALYST || LINUX
        return Environment.GetEnvironmentVariable("MY_AI_API_KEY", EnvironmentVariableTarget.User); // Or Machine
#else
        return await SecureStorage.GetAsync("MY_AI_API_KEY");
#endif
    }

    public static async Task<string> GetAiOrgId()
    {
#if WINDOWS || MACCATALYST || LINUX
        return Environment.GetEnvironmentVariable("MY_AI_ORG_KEY", EnvironmentVariableTarget.User); // Or Machine
#else
        return await SecureStorage.GetAsync("MY_AI_ORG_KEY");
#endif
    }
}
