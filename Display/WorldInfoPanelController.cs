using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldInfoPanelController : MonoBehaviour
{
    private enum VoiceFeedbackState
    {
        Idle,
        Listening,
        Processing,
        Error
    }

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image backgroundImage;

    [Header("Optional")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private bool faceCamera = true;
    [SerializeField] private float followSmoothing = 10f;

    [Header("Voice Feedback")]
    [SerializeField] private Color idleColor = new Color(0.6320754f, 0.37393963f, 0.09242612f, 0.4627451f);
    [SerializeField] private Color listeningColor = new Color(0.2f, 0.7f, 1f, 0.8f);
    [SerializeField] private Color processingColor = new Color(1f, 0.65f, 0.2f, 0.82f);
    [SerializeField] private Color errorColor = new Color(1f, 0.3f, 0.3f, 0.85f);

    private bool hasFollowOffset;
    private Vector3 followOffsetInYawSpace;
    private SpriteStateData lastState;
    private VoiceFeedbackState voiceFeedbackState;

    public void ApplyState(SpriteStateData state)
    {
        lastState = new SpriteStateData
        {
            Mode = state.Mode,
            Title = state.Title,
            Body = state.Body,
            ShowPanel = state.ShowPanel
        };

        Debug.Log("[WorldInfoPanelController] ApplyState called: " + state.Title);

        if (panelRoot != null)
        {
            panelRoot.SetActive(state.ShowPanel);
            Debug.Log("[WorldInfoPanelController] panelRoot active = " + state.ShowPanel);
        }
        else
        {
            Debug.LogWarning("[WorldInfoPanelController] panelRoot is NULL");
        }

        if (titleText != null)
        {
            titleText.text = state.Title;
            Debug.Log("[WorldInfoPanelController] titleText updated");
        }
        else
        {
            Debug.LogWarning("[WorldInfoPanelController] titleText is NULL");
        }

        if (bodyText != null)
        {
            bodyText.text = state.Body;
            Debug.Log("[WorldInfoPanelController] bodyText updated");
        }
        else
        {
            Debug.LogWarning("[WorldInfoPanelController] bodyText is NULL");
        }

        ApplyVoiceFeedbackVisuals();
    }

    private void LateUpdate()
    {
        if (cameraTransform == null)
        {
            TryResolveCameraTransform();
        }

        if (followPlayer && cameraTransform != null)
        {
            Quaternion yawRotation = GetYawRotation(cameraTransform.forward);

            if (!hasFollowOffset)
            {
                followOffsetInYawSpace = Quaternion.Inverse(yawRotation) * (transform.position - cameraTransform.position);
                hasFollowOffset = true;
            }

            Vector3 targetPosition = cameraTransform.position + yawRotation * followOffsetInYawSpace;
            float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        }

        if (!faceCamera || cameraTransform == null)
            return;

        Vector3 direction = transform.position - cameraTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void SetCameraTransform(Transform targetCamera)
    {
        cameraTransform = targetCamera;
        hasFollowOffset = false;
    }

    public void ShowListeningFeedback()
    {
        voiceFeedbackState = VoiceFeedbackState.Listening;
        ApplyVoiceFeedbackVisuals();
    }

    public void ShowProcessingFeedback()
    {
        voiceFeedbackState = VoiceFeedbackState.Processing;
        ApplyVoiceFeedbackVisuals();
    }

    public void ShowErrorFeedback(string errorMessage)
    {
        voiceFeedbackState = VoiceFeedbackState.Error;

        if (bodyText != null && !string.IsNullOrWhiteSpace(errorMessage))
        {
            bodyText.text = errorMessage;
        }

        ApplyVoiceFeedbackVisuals();
    }

    public void ClearVoiceFeedback()
    {
        voiceFeedbackState = VoiceFeedbackState.Idle;

        if (lastState != null)
        {
            ApplyState(lastState);
            return;
        }

        ApplyVoiceFeedbackVisuals();
    }

    private void TryResolveCameraTransform()
    {
        if (Camera.main != null)
        {
            SetCameraTransform(Camera.main.transform);
        }
    }

    private void ApplyVoiceFeedbackVisuals()
    {
        if (backgroundImage == null && panelRoot != null)
        {
            backgroundImage = panelRoot.GetComponent<Image>();
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = GetBackgroundColorForState(voiceFeedbackState);
        }

        if (titleText != null && lastState != null)
        {
            titleText.text = GetTitleForState(lastState.Title);
        }
    }

    private Color GetBackgroundColorForState(VoiceFeedbackState state)
    {
        return state switch
        {
            VoiceFeedbackState.Listening => listeningColor,
            VoiceFeedbackState.Processing => processingColor,
            VoiceFeedbackState.Error => errorColor,
            _ => idleColor
        };
    }

    private string GetTitleForState(string baseTitle)
    {
        string resolvedTitle = string.IsNullOrWhiteSpace(baseTitle) ? "UMN Sprite" : baseTitle;

        return voiceFeedbackState switch
        {
            VoiceFeedbackState.Listening => resolvedTitle + " [Listening]",
            VoiceFeedbackState.Processing => resolvedTitle + " [Processing]",
            VoiceFeedbackState.Error => resolvedTitle + " [Error]",
            _ => resolvedTitle
        };
    }

    private static Quaternion GetYawRotation(Vector3 forward)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }
}
