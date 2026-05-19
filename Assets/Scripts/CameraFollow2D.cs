using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Follow Axis")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Bounds")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Top Lock")]
    [SerializeField] private bool lockAtTopLimit = false;
    [SerializeField] private Renderer topLimitRenderer;
    [SerializeField] private string topLimitObjectName = "BlueLight";
    [SerializeField] private float topLimitOffset = 0f;

    private Vector3 velocity;
    private float fixedZ;
    private Camera followCamera;

    private void Awake()
    {
        fixedZ = transform.position.z;
        followCamera = GetComponent<Camera>();
        ResolveTopLimitRenderer();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;

        float targetX = followX ? target.position.x + offset.x : currentPosition.x;
        float targetY = followY ? target.position.y + offset.y : currentPosition.y;

        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minBounds.x, maxBounds.x);
            targetY = Mathf.Clamp(targetY, minBounds.y, maxBounds.y);
        }

        if (lockAtTopLimit && TryGetTopLimitY(out float topLimitY))
        {
            targetY = Mathf.Min(targetY, topLimitY);
        }

        Vector3 desiredPosition = new Vector3(targetX, targetY, fixedZ);

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }

    private void ResolveTopLimitRenderer()
    {
        if (topLimitRenderer != null || string.IsNullOrWhiteSpace(topLimitObjectName))
        {
            return;
        }

        GameObject limitObject = GameObject.Find(topLimitObjectName);
        if (limitObject != null)
        {
            topLimitRenderer = limitObject.GetComponent<Renderer>();
        }
    }

    private bool TryGetTopLimitY(out float topLimitY)
    {
        topLimitY = 0f;

        if (topLimitRenderer == null)
        {
            ResolveTopLimitRenderer();
        }

        if (topLimitRenderer == null)
        {
            return false;
        }

        float visibleHalfHeight = followCamera != null && followCamera.orthographic
            ? followCamera.orthographicSize
            : 0f;

        topLimitY = topLimitRenderer.bounds.max.y - visibleHalfHeight + topLimitOffset;
        return true;
    }
}
