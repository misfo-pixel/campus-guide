using UnityEngine;

public class UMNSpriteDisplayController : MonoBehaviour
{
    [Header("Optional Visual References")]
    [SerializeField] private GameObject visualRoot;
    
    [Header("Follow Player")]
    [SerializeField] private Transform followTargetTransform;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private float followSmoothing = 10f;

    private SpriteStateData currentState;
    private bool hasFollowOffset;
    private Vector3 followOffsetInYawSpace;

    public void ApplyState(SpriteStateData state)
    {
        currentState = state;

        if (visualRoot != null)
        {
            visualRoot.SetActive(true);
        }

        // Future expansion:
        // - switch animation by state.Mode
        // - change glow/material
        // - rotate toward target
        // - play speaking/listening visual feedback
    }

    private void LateUpdate()
    {
        if (!followPlayer)
            return;

        if (followTargetTransform == null)
        {
            TryResolveFollowTarget();
        }

        if (followTargetTransform == null)
            return;

        Quaternion yawRotation = GetYawRotation(followTargetTransform.forward);

        if (!hasFollowOffset)
        {
            followOffsetInYawSpace = Quaternion.Inverse(yawRotation) * (transform.position - followTargetTransform.position);
            hasFollowOffset = true;
        }

        Vector3 targetPosition = followTargetTransform.position + yawRotation * followOffsetInYawSpace;
        float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);

        if (!facePlayer)
            return;

        Vector3 direction = followTargetTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    public void SetFollowTarget(Transform targetTransform)
    {
        followTargetTransform = targetTransform;
        hasFollowOffset = false;
    }

    private void TryResolveFollowTarget()
    {
        if (Camera.main != null)
        {
            followTargetTransform = Camera.main.transform;
            hasFollowOffset = false;
        }
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
