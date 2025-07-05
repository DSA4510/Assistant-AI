using UnityEngine;
using TMPro;
using UnityEngine.UI;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class TestTTS : MonoBehaviour
{
    //public TextMeshProUGUI textField;
    //public Button SpeakButton;

#if UNITY_ANDROID
    private AndroidJavaObject tts;
    private bool isInitialized = false;
    private AndroidJavaObject context;
    private AndroidJavaObject locale;

    void Start()
    {
        // Request microphone permission if needed
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        // Get Unity player activity
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        context = activity.Call<AndroidJavaObject>("getApplicationContext");

        // Create TTS object
        tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", context, new TTSInitListener(this));

        // Set up Vietnamese locale
        locale = new AndroidJavaObject("java.util.Locale", "vi", "VN");

        //SpeakButton.onClick.AddListener(Speak);
    }
    public void Speak(string text)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("TTS not initialized yet");
            return;
        }

        //string text = textField.text;
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("No text to speak");
            return;
        }

        // Create HashMap for parameters
        AndroidJavaObject paramsMap = new AndroidJavaObject("java.util.HashMap");
        paramsMap.Call<string>("put", "utteranceId", "unity-utterance");

        // Call speak using Android's method signature
        tts.Call<int>("speak", text, 1, paramsMap);
    }

    class TTSInitListener : AndroidJavaProxy
    {
        private TestTTS parent;

        public TTSInitListener(TestTTS parent) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.parent = parent;
        }

        public void onInit(int status)
        {
            if (status == 0) // SUCCESS
            {
                // Set language
                int result = parent.tts.Call<int>("setLanguage", parent.locale);

                if (result >= 0)
                {
                    parent.isInitialized = true;
                    Debug.Log("TTS initialized successfully with Vietnamese");
                }
                else
                {
                    Debug.LogError("Failed to set Vietnamese language");
                }
            }
            else
            {
                Debug.LogError("TTS initialization failed");
            }
        }
    }

    void OnDestroy()
    {
        if (tts != null)
        {
            tts.Call("shutdown");
        }
    }

#else
    void Start()
    {
        Debug.LogWarning("TTS only works on Android");
    }

    void Speak()
    {
        Debug.LogWarning("TTS only works on Android");
    }
#endif
}