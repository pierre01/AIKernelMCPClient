namespace Lights.MauiClient.Services.Interfaces;

public interface IDialogService
{
    Task<bool> ShowAlert(string title, string message, string accept, string cancel);
    Task ShowAlert(string title, string message,  string cancel);
    Task ShowToast(string message);
}
