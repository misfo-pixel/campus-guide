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
        if (panelRoot != null)
        {
            panelRoot.SetActive(state.ShowPanel);
        }

        if (titleText != null)
        {
            titleText.text = state.Title;
        }

        if (bodyText != null)
        {
            bodyText.text = state.Body;
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