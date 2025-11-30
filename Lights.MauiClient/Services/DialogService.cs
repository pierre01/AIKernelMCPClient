using Lights.MauiClient.Services.Interfaces;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;


namespace Lights.MauiClient.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowAlert(string title, string message, string accept, string cancel)
    {
        var page = Shell.Current.CurrentPage;
        bool answer = await page.DisplayAlertAsync(title, message, accept, cancel);
        return answer;
    }

    public async Task ShowAlert(string title, string message, string cancel)
    {
        var page = Shell.Current.CurrentPage;
        await page.DisplayAlertAsync(title, message, cancel, FlowDirection.MatchParent);
    }

    public async Task ShowToast(string message)
    {
#if WINDOWS
        var page = Shell.Current.CurrentPage;
        await page.DisplayAlertAsync("", message, "OK");
#else
        CancellationTokenSource cancellationTokenSource = new();
        ToastDuration duration = ToastDuration.Long;
        double fontSize = 18;
        var toast = Toast.Make(message, duration, fontSize);

        await toast.Show(cancellationTokenSource.Token);
#endif
    }

}
