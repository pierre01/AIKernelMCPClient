using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Media;
using System.Globalization;

namespace AIKernelClient
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

    private readonly ISpeechToText _speechToText;
    private bool _isListening = false;
    private bool _isSpeechEnabled = false;

    public MainPage(ISpeechToText speechToText)
    {
        InitializeComponent();
        this._speechToText = speechToText;
    }

    public async void ListenOrPause(object sender, EventArgs args)
    {
        CancellationToken cancellationToken = default;
        await StartListening(cancellationToken);
    }


    private async Task StartListening(CancellationToken cancellationToken)
    {
        if(_isListening)
        {
            await StopListening(cancellationToken);
            _isListening = false;
            return;
        }
        if(_isSpeechEnabled == false)
        {

            var isGranted = await _speechToText.RequestPermissions(cancellationToken);
            if (!isGranted)
            {
                await Toast.Make("Permission not granted").Show(CancellationToken.None);
                return;
            }
        }
        _speechToText.RecognitionResultUpdated += OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted += OnRecognitionTextCompleted;
        await _speechToText.StartListenAsync(new SpeechToTextOptions { Culture = CultureInfo.CurrentCulture, ShouldReportPartialResults = true }, CancellationToken.None);
        _isSpeechEnabled = true;


    }

    async Task StopListening(CancellationToken cancellationToken)
    {
        await _speechToText.StopListenAsync(CancellationToken.None);
        _speechToText.RecognitionResultUpdated -= OnRecognitionTextUpdated;
        _speechToText.RecognitionResultCompleted -= OnRecognitionTextCompleted;
    }

    void OnRecognitionTextUpdated(object sender, SpeechToTextRecognitionResultUpdatedEventArgs args)
    {
        //RecognitionText += args.RecognitionResult;
    }

    void OnRecognitionTextCompleted(object sender, SpeechToTextRecognitionResultCompletedEventArgs args)
    {
        //RecognitionText = args.RecognitionResult;
    }


        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }

}
