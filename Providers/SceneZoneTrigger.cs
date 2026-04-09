using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SceneZoneTrigger : MonoBehaviour
{
    [Header("Target To Track")]
    [SerializeField] private Transform targetTransform;

    [Header("Zone Data")]
    [SerializeField] private string zoneId = "keller_hall_zone";
    [SerializeField] private string zoneTitle = "Keller Hall";

    [TextArea]
    [SerializeField] private string zoneDescription = "You are in the main Keller Hall corridor near several classrooms.";

    [Header("Collider Outline")]
    [SerializeField] private bool showColliderOutline = true;
    [SerializeField] private Color outlineColor = new Color(0.17f, 0.95f, 0.64f, 1f);
    [SerializeField] private float outlineWidth = 0.02f;

    private BoxCollider boxCollider;
    private bool wasInside = false;
    private LineRenderer outlineRenderer;
    private Material outlineMaterial;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        TryResolveTargetTransform();
        EnsureOutlineRenderer();
        UpdateOutlineRenderer();
    }

    private void Start()
    {
        Debug.Log("[SceneZoneTrigger] Bounds-check mode started on: " + gameObject.name);
    }

    private void Update()
    {
        if (targetTransform == null)
        {
            TryResolveTargetTransform();
        }

        if (targetTransform == null || boxCollider == null)
            return;

        UpdateOutlineRenderer();

        Bounds worldBounds = boxCollider.bounds;
        Vector3 targetPosition = targetTransform.position;
        bool isInside = worldBounds.Contains(targetPosition);

        if (isInside && !wasInside)
        {
            Debug.Log("[SceneZoneTrigger] Target entered zone: " + targetTransform.name);

            SceneZoneData zoneData = new SceneZoneData
            {
                zoneId = zoneId,
                title = zoneTitle,
                description = zoneDescription
            };

            UMNContextManager contextManager = FindFirstObjectByType<UMNContextManager>();
            if (contextManager != null)
            {
                contextManager.SetSceneZone(zoneData);
                Debug.Log("[SceneZoneTrigger] ContextManager updated with zone: " + zoneId);
            }
            else
            {
                Debug.LogWarning("[SceneZoneTrigger] No UMNContextManager found in scene.");
            }

            InputContextAssembler assembler = FindFirstObjectByType<InputContextAssembler>();
            if (assembler != null)
            {
                assembler.SetCurrentZone(zoneTitle, zoneDescription);
                Debug.Log("[SceneZoneTrigger] InputContextAssembler updated with zone: " + zoneId);
            }
            else
            {
                Debug.LogWarning("[SceneZoneTrigger] No InputContextAssembler found in scene.");
            }

        }

        if (!isInside && wasInside)
        {
            Debug.Log("[SceneZoneTrigger] Target left zone: " + targetTransform.name);
        }

        wasInside = isInside;
    }

    private void TryResolveTargetTransform()
    {
        if (targetTransform != null)
            return;

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        if (taggedPlayer != null)
        {
            targetTransform = taggedPlayer.transform;
            Debug.Log("[SceneZoneTrigger] Auto-assigned targetTransform from Player tag: " + targetTransform.name);
            return;
        }

        if (Camera.main != null)
        {
            targetTransform = Camera.main.transform;
            Debug.Log("[SceneZoneTrigger] Auto-assigned targetTransform from Main Camera: " + targetTransform.name);
        }
    }

    private void EnsureOutlineRenderer()
    {
        if (!showColliderOutline || boxCollider == null || outlineRenderer != null)
            return;

        GameObject outlineObject = new GameObject("ColliderOutline");
        outlineObject.transform.SetParent(transform, false);
        outlineRenderer = outlineObject.AddComponent<LineRenderer>();
        outlineRenderer.useWorldSpace = true;
        outlineRenderer.loop = false;
        outlineRenderer.positionCount = 16;
        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
        outlineRenderer.textureMode = LineTextureMode.Stretch;
        outlineRenderer.alignment = LineAlignment.View;

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (lineShader != null)
        {
            outlineMaterial = new Material(lineShader);
            outlineRenderer.material = outlineMaterial;
        }
    }

    private void UpdateOutlineRenderer()
    {
        if (!showColliderOutline || boxCollider == null)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = false;
            }

            return;
        }

        EnsureOutlineRenderer();

        if (outlineRenderer == null)
            return;

        outlineRenderer.enabled = true;
        outlineRenderer.startColor = outlineColor;
        outlineRenderer.endColor = outlineColor;
        outlineRenderer.startWidth = outlineWidth;
        outlineRenderer.endWidth = outlineWidth;

        Vector3 center = boxCollider.center;
        Vector3 extents = boxCollider.size * 0.5f;

        Vector3 lbf = transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, extents.z));
        Vector3 rbf = transform.TransformPoint(center + new Vector3(extents.x, -extents.y, extents.z));
        Vector3 rbb = transform.TransformPoint(center + new Vector3(extents.x, -extents.y, -extents.z));
        Vector3 lbb = transform.TransformPoint(center + new Vector3(-extents.x, -extents.y, -extents.z));
        Vector3 ltf = transform.TransformPoint(center + new Vector3(-extents.x, extents.y, extents.z));
        Vector3 rtf = transform.TransformPoint(center + new Vector3(extents.x, extents.y, extents.z));
        Vector3 rtb = transform.TransformPoint(center + new Vector3(extents.x, extents.y, -extents.z));
        Vector3 ltb = transform.TransformPoint(center + new Vector3(-extents.x, extents.y, -extents.z));

        outlineRenderer.SetPositions(new[]
        {
            lbf, rbf, rbb, lbb, lbf,
            ltf, rtf, rbf, rtf,
            rtb, rbb, rtb, ltb,
            lbb, ltb, ltf
        });
    }

    private void OnDestroy()
    {
        if (outlineMaterial != null)
        {
            Destroy(outlineMaterial);
        }
    }
}
