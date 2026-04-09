using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

public class OfficialCampusFeedProvider : MonoBehaviour
{
    // Current status:
    // - `https://events.tc.umn.edu/all/feed` returns 200 in Unity.
    // - But the payload does not reliably deserialize into a normal RSS item list for this project.
    // - When we tried a loose fallback, it captured generic page copy instead of real events.
    //
    // So this provider is intentionally conservative right now:
    // - Only keep parsed items that look like real event titles.
    // - If parsing is unclear, report "temporarily unavailable" rather than feeding bad text to the LLM.
    //
    // Best next debugging step:
    // 1. Inspect the exact 200-response body from Unity for this endpoint.
    // 2. Confirm whether UMN exposes a more stable LiveWhale RSS/JSON endpoint for events.
    // 3. Replace this parser with a schema-specific parser once the response format is confirmed.
    private static readonly string[] GenericNonEventPhrases =
    {
        "Find events on the UMN Twin Cities Events Calendar",
        "Events Calendar",
        "Use search to filter by date, location, or category",
        "University of Minnesota Twin Cities"
    };

    [Header("Official UMN Feed")]
    [SerializeField] private string feedUrl = "https://events.tc.umn.edu/all/feed";
    [SerializeField] private int maxItems = 3;
    [SerializeField] private int timeoutSeconds = 15;

    private List<string> latestItems = new List<string>();
    private bool hasLoaded = false;

    public bool HasLoaded => hasLoaded;

    private void Start()
    {
        Debug.LogWarning("[OfficialCampusFeedProvider] Starting feed fetch: " + feedUrl);
        StartCoroutine(FetchFeed());
    }

    public IEnumerator FetchFeed()
    {
        string[] candidateUrls = BuildCandidateFeedUrls();
        string xmlText = null;
        bool loadedFromCandidate = false;

        for (int candidateIndex = 0; candidateIndex < candidateUrls.Length; candidateIndex++)
        {
            string candidateUrl = candidateUrls[candidateIndex];
            using UnityWebRequest req = UnityWebRequest.Get(candidateUrl);
            req.timeout = timeoutSeconds;
            yield return req.SendWebRequest();

            Debug.LogWarning(
                "[OfficialCampusFeedProvider] Feed request finished. " +
                "Url=" + candidateUrl +
                " Result=" + req.result +
                " Code=" + req.responseCode);

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "[OfficialCampusFeedProvider] Feed request failed. " +
                    "Url=" + candidateUrl +
                    " Result=" + req.result +
                    " Code=" + req.responseCode +
                    "\n" + req.error +
                    "\n" + req.downloadHandler.text);
                continue;
            }

            xmlText = req.downloadHandler.text;

            if (LooksLikeHtmlDocument(xmlText) && candidateIndex + 1 < candidateUrls.Length)
            {
                Debug.LogWarning("[OfficialCampusFeedProvider] Feed returned HTML for " + candidateUrl + ". Trying fallback URL.");
                continue;
            }

            loadedFromCandidate = true;
            break;
        }

        if (!loadedFromCandidate || string.IsNullOrWhiteSpace(xmlText))
        {
            hasLoaded = true;
            latestItems.Clear();
            Debug.LogError("[OfficialCampusFeedProvider] Could not load a usable official events feed from any candidate URL.");
            yield break;
        }

        if (LooksLikeHtmlDocument(xmlText))
        {
            Debug.LogWarning("[OfficialCampusFeedProvider] Feed returned HTML instead of RSS/XML. Using fallback parser.");
            ParseHtmlFallback(xmlText);
        }
        else
        {
            try
            {
                // Preferred path: parse a real RSS payload with <item><title>...</title></item>.
                ParseRss(xmlText);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    "[OfficialCampusFeedProvider] RSS parse failed.\n" +
                    ex +
                    "\nResponse preview:\n" +
                    GetPreview(xmlText));

                // Last-resort fallback for inspection/debugging.
                // This is intentionally strict and may still produce zero items.
                // Zero items is safer than sending page boilerplate to the model.
                ParseRssFallback(xmlText);
            }
        }

        hasLoaded = true;
        Debug.LogWarning(
            "[OfficialCampusFeedProvider] Feed loaded successfully. " +
            "Items=" + latestItems.Count +
            " Summary=" + GetEventsSummary());
    }

    private string[] BuildCandidateFeedUrls()
    {
        List<string> candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(feedUrl))
        {
            candidates.Add(feedUrl.Trim());
        }

        if (!string.IsNullOrWhiteSpace(feedUrl) &&
            feedUrl.Contains("events.tc.umn.edu", System.StringComparison.OrdinalIgnoreCase))
        {
            string liveWhaleRssUrl = "https://events.tc.umn.edu/live/rss/events/";
            if (!candidates.Contains(liveWhaleRssUrl))
            {
                candidates.Add(liveWhaleRssUrl);
            }
        }

        return candidates.ToArray();
    }

    private void ParseRssFallback(string xmlText)
    {
        latestItems.Clear();

        // This fallback only tries to salvage obvious title-like strings from a malformed response.
        // It is not a trustworthy source of record for event data.
        MatchCollection titleMatches = Regex.Matches(
            xmlText,
            @"<title>\s*(.*?)\s*</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        for (int i = 0; i < titleMatches.Count && latestItems.Count < maxItems; i++)
        {
            string title = System.Net.WebUtility.HtmlDecode(titleMatches[i].Groups[1].Value).Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            if (LooksLikeGenericPageCopy(title) ||
                title.Equals("RSS", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            latestItems.Add(title);
        }
    }

    private void ParseHtmlFallback(string htmlText)
    {
        latestItems.Clear();

        MatchCollection headingMatches = Regex.Matches(
            htmlText,
            @"<(h1|h2|h3)[^>]*>\s*(.*?)\s*</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        for (int i = 0; i < headingMatches.Count && latestItems.Count < maxItems; i++)
        {
            string title = StripHtml(headingMatches[i].Groups[2].Value);
            title = System.Net.WebUtility.HtmlDecode(title).Trim();

            if (string.IsNullOrWhiteSpace(title) || LooksLikeGenericPageCopy(title))
            {
                continue;
            }

            if (title.Length < 8)
            {
                continue;
            }

            latestItems.Add(title);
        }

        if (latestItems.Count > 0)
        {
            return;
        }

        ParseRssFallback(htmlText);
    }

    private string GetPreview(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "<empty>";
        }

        const int maxPreviewLength = 500;
        return text.Length <= maxPreviewLength ? text : text.Substring(0, maxPreviewLength);
    }

    private void ParseRss(string xmlText)
    {
        latestItems.Clear();

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlText);

        // Assumes a standard RSS shape. If UMN's endpoint is HTML or a nonstandard feed,
        // this will either return zero <item> nodes or throw before we get here.
        XmlNodeList itemNodes = doc.GetElementsByTagName("item");

        int count = Mathf.Min(maxItems, itemNodes.Count);
        for (int i = 0; i < count; i++)
        {
            XmlNode item = itemNodes[i];

            string title = GetChildInnerText(item, "title");
            string pubDate = GetChildInnerText(item, "pubDate");

            if (!string.IsNullOrWhiteSpace(title))
            {
                if (LooksLikeGenericPageCopy(title))
                {
                    continue;
                }

                string line = title;
                if (!string.IsNullOrWhiteSpace(pubDate))
                {
                    line += " (" + pubDate + ")";
                }

                latestItems.Add(line);
            }
        }
    }

    private string GetChildInnerText(XmlNode parent, string childName)
    {
        if (parent == null) return "";

        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child.Name == childName)
            {
                return child.InnerText.Trim();
            }
        }

        return "";
    }

    public string GetEventsSummary()
    {
        if (!hasLoaded)
        {
            return "Official campus events are still loading.";
        }

        if (latestItems.Count == 0)
        {
            // Important: avoid hallucination-by-ingestion.
            // If we cannot verify concrete event items, tell the caller the official feed is unavailable.
            return "Official campus events are temporarily unavailable.";
        }

        return "Official Campus Events: " + string.Join(" | ", latestItems);
    }

    private bool LooksLikeGenericPageCopy(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        for (int i = 0; i < GenericNonEventPhrases.Length; i++)
        {
            if (text.IndexOf(GenericNonEventPhrases[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool LooksLikeHtmlDocument(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string trimmed = text.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE html", System.StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", System.StringComparison.OrdinalIgnoreCase);
    }

    private string StripHtml(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return Regex.Replace(text, "<.*?>", string.Empty);
    }
}
