using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Text;

public class ChatAPI : MonoBehaviour
{
    public TMP_InputField userInputField;
    public TextMeshProUGUI chatOutputText;
    public Button sendButton;
    public Button clearButton;

    private string profileApi = "https://openai.lpnserver.net/api/v1/profile/682081e7767b4c86b44e8591";
    private string chatApi = "https://openai.lpnserver.net/api/v1/chat/682081e7767b4c86b44e8591";

    private List<ChatMessage> messages = new List<ChatMessage>();
    private string currentPrompt = "";

    //public TextToSpeechReal tts;
    public TestTTS tts;

    [System.Serializable]
    public class ChatMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class MessageWrapper
    {
        public List<ChatMessage> messages;
    }

    void Start()
    {
        sendButton.onClick.AddListener(OnSendMessage);
        clearButton.onClick.AddListener(ClearHistory);
        userInputField.onSubmit.AddListener(delegate { OnSendMessage(); });

        StartCoroutine(LoadProfilePrompt());
        LoadChatHistory();
    }

    void LoadChatHistory()
    {
        if (PlayerPrefs.HasKey("chatHistory"))
        {
            string json = PlayerPrefs.GetString("chatHistory");
            MessageWrapper wrapper = JsonUtility.FromJson<MessageWrapper>(json);
            messages = wrapper.messages ?? new List<ChatMessage>();

            foreach (var m in messages)
                AppendMessage(m.content, m.role);
        }
    }

    IEnumerator LoadProfilePrompt()
    {
        // Create a custom certificate handler that accepts all certificates
        //var certHandler = new BypassCertificate();

        using (UnityWebRequest request = UnityWebRequest.Get(profileApi))
        {
            request.certificateHandler = new BypassCertificate();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var rawJson = request.downloadHandler.text;
                var data = JsonUtility.FromJson<ProfileResponse>(rawJson);
                currentPrompt = data.prompt;
                Debug.Log("Loaded system prompt: " + currentPrompt);
            }
            else
            {
                Debug.LogWarning("Failed to fetch profile prompt: " + request.error);
                AppendMessage("Connection error - please check your internet", "system");
            }
        }
    }

    // Add this class to bypass SSL verification
    public class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Always return true to accept all certificates
            return true;
        }
    }

    public void OnSendMessage()
    {
        string userInput = userInputField.text.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        // Clear field and show input
        userInputField.text = "";
        messages.Add(new ChatMessage { role = "user", content = userInput });
        AppendMessage(userInput, "user");

        StartCoroutine(SendMessageToChatbot());
    }

    IEnumerator SendMessageToChatbot()
    {
        // Create payload with system prompt and message history
        List<ChatMessage> payload = new List<ChatMessage>
    {
        new ChatMessage { role = "system", content = currentPrompt }
    };
        payload.AddRange(messages);

        // Prepare JSON payload
        var payloadWrapper = new MessageWrapper { messages = payload };
        string jsonPayload = JsonUtility.ToJson(payloadWrapper);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(chatApi, "POST"))
        {
            // Set up request handlers and headers
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            // Add certificate handler for Android SSL issues
            request.certificateHandler = new BypassCertificate();

            // Additional Android-specific settings
#if UNITY_ANDROID
            request.timeout = 30;
            request.SetRequestHeader("Accept-Encoding", "gzip, deflate");
#endif

            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                string reply = request.downloadHandler.text;

                // Clean and validate response
                reply = reply.Trim();
                if (string.IsNullOrWhiteSpace(reply))
                {
                    reply = "The assistant didn't provide a response.";
                }

                // Add to message history and UI
                messages.Add(new ChatMessage { role = "assistant", content = reply });
                AppendMessage(reply, "assistant");

                // Save chat history
                try
                {
                    var saveWrapper = new MessageWrapper { messages = messages };
                    PlayerPrefs.SetString("chatHistory", JsonUtility.ToJson(saveWrapper));
                    PlayerPrefs.Save();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to save chat history: " + e.Message);
                }

                // Play reply via TTS if available
                if (tts != null)
                {
                    try
                    {
                        tts.Speak(reply);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("TTS Error: " + e.Message);
                    }
                }
            }
            else
            {
                // Detailed error handling
                string errorMessage = "API error";

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    errorMessage = "Connection error - please check your internet";
                }
                else if (request.result == UnityWebRequest.Result.ProtocolError)
                {
                    errorMessage = "Server error - please try again later";
                }
                else if (request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    errorMessage = "Data processing error";
                }

                AppendMessage(errorMessage, "assistant");
                Debug.LogError($"Chat API error ({request.responseCode}): {request.error}\nURL: {chatApi}");
            }
        }
    }

    void AppendMessage(string content, string role)
    {
        string formatted = $"<b>[{role}]</b>: {content}\n";
        chatOutputText.text += formatted;
    }

    public void ClearHistory()
    {
        PlayerPrefs.DeleteKey("chatHistory");
        messages.Clear();
        chatOutputText.text = "";
    }

    [System.Serializable]
    public class ProfileResponse
    {
        public string prompt;
    }
}
