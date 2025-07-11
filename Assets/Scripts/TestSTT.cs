using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestSTT : MonoBehaviour, ISpeechToTextListener
{
    // public Button StartSpeechToTextButton, StopSpeechToTextButton;
    // public Slider VoiceLevelSlider;
    public bool PreferOfflineRecognition;

    public Button RecordButton;
    public Image RecordButtonIcon;
    public TMP_InputField SpeechText;

    private float normalizedVoiceLevel;
    private bool isSpeaking = false;

    private void Awake()
    {
        //string lan = "en-US";
        string lan = "vi-VN";
        Debug.Log("Is Support: " + SpeechToText.IsLanguageSupported(lan).ToString());
        SpeechToText.Initialize(lan);

        //StartSpeechToTextButton.onClick.AddListener(StartSpeechToText);
        //StopSpeechToTextButton.onClick.AddListener(StopSpeechToText);
        RecordButton.onClick.AddListener(toggleSpeaking);
    }

    private void Update()
    {
        //StartSpeechToTextButton.interactable = SpeechToText.IsServiceAvailable(PreferOfflineRecognition) && !SpeechToText.IsBusy();
        //StopSpeechToTextButton.interactable = SpeechToText.IsBusy();

        // You may also apply some noise to the voice level for a more fluid animation (e.g. via Mathf.PerlinNoise)
        // VoiceLevelSlider.value = Mathf.Lerp(VoiceLevelSlider.value, normalizedVoiceLevel, 15f * Time.unscaledDeltaTime);
    }

    void toggleSpeaking()
    {
        if (isSpeaking && SpeechToText.IsBusy())
        {
            StopSpeechToText();
            RecordButtonIcon.color = Color.black;
            RecordButton.GetComponent<Image>().color = Color.white;
            isSpeaking = false;
        }
        else if (!isSpeaking && !SpeechToText.IsBusy())
        {
            StartSpeechToText();
            RecordButtonIcon.color = Color.white;
            RecordButton.GetComponent<Image>().color = Color.red;
            isSpeaking = true;
        }
    }

    public void ChangeLanguage(string preferredLanguage)
    {
        if (!SpeechToText.Initialize(preferredLanguage))
            SpeechText.text = "Couldn't initialize with language: " + preferredLanguage;
    }

    public void StartSpeechToText()
    {
        SpeechToText.RequestPermissionAsync((permission) =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                if (SpeechToText.Start(this, preferOfflineRecognition: PreferOfflineRecognition))
                    SpeechText.text = "";
                else
                    SpeechText.text = "Couldn't start speech recognition session!";
            }
            else
                SpeechText.text = "Permission is denied!";
        });
    }

    public void StopSpeechToText()
    {
        SpeechToText.ForceStop();
    }

    void ISpeechToTextListener.OnReadyForSpeech()
    {
        Debug.Log("OnReadyForSpeech");
    }

    void ISpeechToTextListener.OnBeginningOfSpeech()
    {
        Debug.Log("OnBeginningOfSpeech");
    }

    void ISpeechToTextListener.OnVoiceLevelChanged(float normalizedVoiceLevel)
    {
        // Note that On Android, voice detection starts with a beep sound and it can trigger this callback. You may want to ignore this callback for ~0.5s on Android.
        this.normalizedVoiceLevel = normalizedVoiceLevel;
    }

    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        Debug.Log("OnPartialResultReceived: " + spokenText);
        SpeechText.text = spokenText;
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        Debug.Log("OnResultReceived: " + spokenText + (errorCode.HasValue ? (" --- Error: " + errorCode) : ""));
        SpeechText.text = spokenText;
        normalizedVoiceLevel = 0f;

        // Recommended approach:
        // - If errorCode is 0, session was aborted via SpeechToText.Cancel. Handle the case appropriately.
        // - If errorCode is 9, notify the user that they must grant Microphone permission to the Google app and call SpeechToText.OpenGoogleAppSettings.
        // - If the speech session took shorter than 1 seconds (should be an error) or a null/empty spokenText is returned, prompt the user to try again (note that if
        //   errorCode is 6, then the user hasn't spoken and the session has timed out as expected).
    }
}