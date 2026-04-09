using UnityEngine;

public class UMNContextManager : MonoBehaviour
{
    [Header("Display Targets")]
    [SerializeField] private UMNSpriteDisplayController spriteDisplayController;
    [SerializeField] private WorldInfoPanelController worldInfoPanelController;

    [Header("Initial State")]
    [SerializeField] private SpriteMode initialMode = SpriteMode.Greeting;
    [SerializeField] private string initialTitle = "UMN Sprite";
    [SerializeField][TextArea] private string initialBody = "Welcome. I am your campus companion.";
    [SerializeField] private bool initialShowPanel = true;

    private SpriteStateData currentState;

    private void Start()
    {
        currentState = new SpriteStateData
        {
            Mode = initialMode,
            Title = initialTitle,
            Body = initialBody,
            ShowPanel = initialShowPanel
        };

        ApplyCurrentState();

        if (Camera.main != null && worldInfoPanelController != null)
        {
            worldInfoPanelController.SetCameraTransform(Camera.main.transform);
        }
    }

    public void SetState(SpriteStateData newState)
    {
        currentState = new SpriteStateData
        {
            Mode = newState.Mode,
            Title = newState.Title,
            Body = newState.Body,
            ShowPanel = newState.ShowPanel
        };
        ApplyCurrentState();
    }

    public void SetSceneZone(SceneZoneData zoneData)
    {
        currentState = new SpriteStateData
        {
            Mode = SpriteMode.Info,
            Title = zoneData.title,
            Body = zoneData.description,
            ShowPanel = true
        };

        ApplyCurrentState();
    }

    protected void ApplyCurrentState()
    {
        Debug.Log("[UMNContextManager] ApplyCurrentState: " + currentState.Title);

        if (spriteDisplayController != null)
        {
            Debug.Log("[UMNContextManager] spriteDisplayController found");
            spriteDisplayController.ApplyState(currentState);
        }
        else
        {
            Debug.LogWarning("[UMNContextManager] spriteDisplayController is NULL");
        }

        if (worldInfoPanelController != null)
        {
            Debug.Log("[UMNContextManager] worldInfoPanelController found");
            worldInfoPanelController.ApplyState(currentState);
        }
        else
        {
            Debug.LogWarning("[UMNContextManager] worldInfoPanelController is NULL");
        }
    }
}
