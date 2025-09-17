using AIKernelClient.Services.Interfaces;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsAPICommon;
using System.Globalization;


namespace AIKernelClient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{

    private int _promptIndex = -1;

    private readonly string[] _userPrompts = House.CustomPrompts;

    //Voice Handling
    private readonly ISpeechToText _speechToText;
    private readonly IDialogService _dialogService;
    private readonly ISemanticKernelService _semanticKernelService;

    private bool _isListening = false;
    private bool _isSpeechEnabled = false;

    public MainPageViewModel(ISpeechToText speechToText, IDialogService dialogService, ISemanticKernelService semanticKernelService)
    {
        _speechToText = speechToText;
        _dialogService = dialogService;
        _semanticKernelService = semanticKernelService;

        _semanticKernelService.InitializeKernelAndPluginAsync().ConfigureAwait(true);
    }


    [ObservableProperty]
    public partial string CallTextInput { get; set; }

    [ObservableProperty]
    public partial string CallTextResult { get; set; }

    [ObservableProperty]
    public partial int TotalTokens { get; set; }

    [ObservableProperty]
    public partial int RequestTokens { get; set; }

    [ObservableProperty]
    public partial int InputTokens { get; set; }
    
    [ObservableProperty]
    public partial bool IsProgressVisible { get; set; }

    [ObservableProperty]
    public partial int OutputTokens { get; set; }

    [ObservableProperty]
    public partial string ButtonImage { get; set; } = "microphone_off.png";


    [RelayCommand]
    private async Task SendRequest()
    {
        IsProgressVisible = true;
        await GetResponseAsync(CallTextInput);
        IsProgressVisible = false;
    }

    [RelayCommand]
    private void NextPrompt()
    {
        _promptIndex++;
        if (_promptIndex >= _userPrompts.Length)
        {
            _promptIndex = 0;
        }
        CallTextInput = _userPrompts[_promptIndex];
    }

    [RelayCommand]
    private void PreviousPrompt()
    {
        _promptIndex--;
        if (_promptIndex < 0)
        {
            _promptIndex = _userPrompts.Length - 1;
        }
        CallTextInput = _userPrompts[_promptIndex];
    }

    #region Voice Handling
    [RelayCommand]
    private async Task ListenOrPause()
    {
        CancellationToken cancellationToken = default;
        await StartListening(cancellationToken);
    }


    private async Task StartListening(CancellationToken cancellationToken)
    {
        if (_isListening)
        {
            await StopListening();
            _isListening = false;
            ButtonImage = "microphone_off.png";
            return;
        }
        if (_isSpeechEnabled == false)
        {
            var isGranted = await _speechToText.RequestPermissions(cancellationToken);
            if (!isGranted)
            {
                await _dialogService.ShowToast("Permission not granted");
                return;
            }
        }
        _isSpeechEnabled = true;
        _isListening = true;
        CallTextInput ="";
        _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;
        ButtonImage = "microphone.png";
        await _speechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true }, CancellationToken.None);

    }

    async Task StopListening()
    {
        await _speechToText.StopListenAsync(CancellationToken.None);
        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    }

    void OnRecognitionTextUpdated(object sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    {
        CallTextInput += args.RecognitionResult;
    }

    void OnRecognitionTextCompleted(object sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        CallTextInput = args.RecognitionResult.Text;
    }
    #endregion

    public async Task GetResponseAsync(string prompt)
    {
        var result = await _semanticKernelService.GetResponseAsync(prompt);
        if (result.IsSuccess)
        {
            CallTextResult = result.Result;
            TotalTokens = result.TotalTokens;
            RequestTokens = result.RequestTokens;
            InputTokens = result.InputTokens;
            OutputTokens = result.OutputTokens;
        }
        else
        {
            CallTextResult = result.Result;
        }
    }
}


