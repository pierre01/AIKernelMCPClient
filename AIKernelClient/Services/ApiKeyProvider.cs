using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIKernelClient.Services
{
    /// <summary>
    /// Provides the API key for accessing AI services.
    /// <code>
    /// Use Control Panel → System → Advanced system settings → Environment Variables → New
    /// OR
    /// Powershell (run as Administrator):
    /// [System.Environment]::SetEnvironmentVariable("MY_AI_API_KEY", "your key goes here", "User")
    /// </code> 
    /// </summary>
    public static class ApiKeyProvider
    {
        public static async Task<string?> GetApiKeyAsync()
        {
#if WINDOWS || MACCATALYST || LINUX
            return Environment.GetEnvironmentVariable("MY_AI_API_KEY", EnvironmentVariableTarget.User);
#else
        return await SecureStorage.GetAsync("MY_AI_API_KEY");
#endif
        }
    }
}
