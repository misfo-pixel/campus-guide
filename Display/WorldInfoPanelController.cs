using TMPro;
using UnityEngine;

public class WorldInfoPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    [Header("Optional")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool faceCamera = true;

    public void ApplyState(SpriteStateData state)
    {
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
    }

    private void LateUpdate()
    {
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
    }
}
