using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIKernelClient.Services.Interfaces;

public  interface ISemanticKernelService
{
    Task InitializeKernelAndPluginAsync();
    Task<KernelPluginResult> GetResponseAsync(string prompt);
}

public class KernelPluginResult
{
    public bool IsSuccess { get; set; } = true;
    public string Result { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
    public int RequestTokens { get; set; }
}
