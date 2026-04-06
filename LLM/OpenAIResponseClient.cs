using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

public class OpenAIResponseClient : MonoBehaviour
{
    [Header("Azure OpenAI")]
    [SerializeField] private string apiKey = "";
    [FormerlySerializedAs("endpoint")]
    [SerializeField] private string responsesEndpoint = "https://csci5629-group8-resource.openai.azure.com/openai/v1";
    [SerializeField] private string model = "gpt-5";
    [SerializeField] private int timeoutSeconds = 30;

    public void RequestResponse(string fullContext, Action<LLMActionResult> onSuccess, Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("Azure OpenAI apiKey is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(responsesEndpoint))
        {
            onError?.Invoke("Azure OpenAI responsesEndpoint is empty.");
            return;
        }

        StartCoroutine(SendRequest(fullContext, onSuccess, onError));
    }

    private IEnumerator SendRequest(string fullContext, Action<LLMActionResult> onSuccess, Action<string> onError)
    {
        string systemPrompt =
@"You are UMN Sprite, an intelligent campus companion.
Return ONLY valid JSON.
Use exactly these fields:
action, title, body

Allowed action values:
greet, explain, point, alert, idle

Keep title short.
Keep body under 30 words.";

        string json = BuildRequestBody(systemPrompt, fullContext);
        string requestUrl = BuildResponsesUrl();

        Debug.Log("[OpenAIResponseClient] Sending request to: " + requestUrl);

        using UnityWebRequest req = new UnityWebRequest(requestUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = timeoutSeconds;

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("api-key", apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error + "\n" + req.downloadHandler.text);
            yield break;
        }

        string raw = req.downloadHandler.text;
        Debug.Log("[OpenAIResponseClient] Raw response:\n" + raw);

        string outputText = ExtractFirstTextField(raw);

        if (string.IsNullOrEmpty(outputText))
        {
            onError?.Invoke("Could not extract model text from response.");
            yield break;
        }

        try
        {
            LLMActionResult result = JsonUtility.FromJson<LLMActionResult>(outputText);
            onSuccess?.Invoke(result);
        }
        catch (Exception e)
        {
            onError?.Invoke("JSON parse failed.\nExtracted text:\n" + outputText + "\n\n" + e.Message);
        }
    }

    private string BuildResponsesUrl()
    {
        string trimmed = responsesEndpoint.Trim();

        if (trimmed.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return trimmed.TrimEnd('/') + "/responses";
    }

    private string BuildRequestBody(string systemPrompt, string userPrompt)
    {
        string s = EscapeJson(systemPrompt);
        string u = EscapeJson(userPrompt);

        return
$@"{{
  ""model"": ""{model}"",
  ""input"": [
    {{
      ""role"": ""system"",
      ""content"": [
        {{ ""type"": ""input_text"", ""text"": ""{s}"" }}
      ]
    }},
    {{
      ""role"": ""user"",
      ""content"": [
        {{ ""type"": ""input_text"", ""text"": ""{u}"" }}
      ]
    }}
  ]
}}";
    }

    private string EscapeJson(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "");
    }

    private string ExtractFirstTextField(string raw)
    {
        Match m = Regex.Match(raw, "\"text\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
        if (!m.Success) return null;

        string text = m.Groups[1].Value;
        text = text.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\\\", "\\");
        return text;
    }
}
