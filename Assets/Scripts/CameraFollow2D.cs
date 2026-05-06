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

    private Vector3 velocity;
    private float fixedZ;

    private void Awake()
    {
        fixedZ = transform.position.z;
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

        Vector3 desiredPosition = new Vector3(targetX, targetY, fixedZ);

        transform.position = Vector3.SmoothDamp(
            currentPosition,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }
}