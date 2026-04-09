using UnityEngine;

public class InputContextAssembler : MonoBehaviour
{
    [SerializeField] private MockLocationProvider locationProvider;
    [SerializeField] private CampusInfoDataBase campusInfoDatabase;
    [SerializeField] private UserQueryInput userQueryInput;

    private string currentZoneTitle = "Unknown Zone";
    private string currentZoneDescription = "No detection yet.";

    public void SetCurrentZone(string zoneTitle, string zoneDescription)
    {
        currentZoneTitle = zoneTitle;
        currentZoneDescription = zoneDescription;
    }

    public string BuildFullContext()
    {
        string userQuery = userQueryInput != null ? userQueryInput.GetCurrentQuery() : "No query";
        string location = locationProvider != null ? locationProvider.GetLocationSummary() : "No location";
        string buildingHint = locationProvider != null ? locationProvider.GetBuildingHint() : "";
        string campusInfo = campusInfoDatabase != null ? campusInfoDatabase.GetCampusInfoSummary(buildingHint) : "No campus info";

        string fullContext =
            "User Query: " + userQuery + "\n" +
            "Location: " + location + "\n" +
            "Detection Title: " + currentZoneTitle + "\n" +
            "Detection Description: " + currentZoneDescription + "\n" +
            "Campus Info: " + campusInfo;

        return fullContext;
    }

    public void PrintFullContext()
    {
        Debug.Log("[InputContextAssembler] Full Context:\n" + BuildFullContext());
    }
}
