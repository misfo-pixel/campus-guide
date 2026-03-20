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

    private void ApplyCurrentState()
    {
        if (spriteDisplayController != null)
        {
            spriteDisplayController.ApplyState(currentState);
        }

        if (worldInfoPanelController != null)
        {
            worldInfoPanelController.ApplyState(currentState);
        }
    }
}