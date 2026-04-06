using UnityEngine;

public class DemoContextProvider : MonoBehaviour
{
    [Header("Debug User Query")]
    [SerializeField] private VoiceTranscriptProvider voiceTranscriptProvider;
    [SerializeField][TextArea] private string fallbackUserQuery = "Where is my next class?";

    [Header("Mock Location")]
    [SerializeField] private string campusArea = "UMN East Bank";
    [SerializeField] private string buildingHint = "Keller Hall";

    [Header("Mock Campus Info")]
    [SerializeField] private LocalJsonDataProvider localJsonDataProvider;
    [SerializeField] private OfficialCampusFeedProvider officialCampusFeedProvider;
    [SerializeField] private string roomName = "3-180";
    [SerializeField] private string eventTitle = "CSCI Lecture";
    [SerializeField] private string eventTime = "2:00 PM";
    [SerializeField][TextArea] private string campusNote = "This room is often used for computer science classes.";

    [Header("Detection / Scene Context")]
    [SerializeField] private string detectionTitle = "Keller Hall";
    [SerializeField][TextArea] private string detectionDescription = "You are in the main Keller Hall corridor near several classrooms.";

    public bool IsOfficialFeedReady()
    {
        return officialCampusFeedProvider == null || officialCampusFeedProvider.HasLoaded;
    }

    public string BuildPrompt()
    {
        string query = fallbackUserQuery;

        if (voiceTranscriptProvider != null)
        {
            string transcript = voiceTranscriptProvider.GetTranscript();

            if (!string.IsNullOrWhiteSpace(transcript))
            {
                query = transcript;
            }
        }

        string buildingSummary = localJsonDataProvider != null
            ? localJsonDataProvider.GetBuildingSummary("Keller Hall")
            : "No building info.";

        string nextClassSummary = localJsonDataProvider != null
            ? localJsonDataProvider.GetNextClassSummary()
            : "No class info.";

        string taskSummary = localJsonDataProvider != null
            ? localJsonDataProvider.GetUpcomingTaskSummary()
            : "No task info.";

        string eventsSummary = officialCampusFeedProvider != null
            ? officialCampusFeedProvider.GetEventsSummary()
            : "No official campus events.";

        Debug.LogWarning("[DemoContextProvider] Official Events Summary = " + eventsSummary);

        return
            "User Query: " + query + "\n" +
            "Location: " + campusArea + " / " + buildingHint + "\n" +
            "Detection Title: " + detectionTitle + "\n" +
            "Detection Description: " + detectionDescription + "\n" +
            "Campus Info: " + eventTitle + " at " + eventTime + " in room " + roomName + ". " + campusNote +
            "Building Info: " + buildingSummary + "\n" +
            "Next Class: " + nextClassSummary + "\n" +
            "Upcoming Task: " + taskSummary + "\n" +
            "Official Events: " + eventsSummary;
    }
}
