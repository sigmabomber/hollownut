using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;
    public float smoothSpeed = 0.2f;
    public Vector2 lookAhead = new Vector2(1.5f, 0f); // Remove vertical look-ahead

    [Header("Hollow Knight Style Settings")]
    public float verticalDeadZone = 1f; // Center dead zone where camera doesn't move vertically
    public float verticalFollowThreshold = 2f; // Distance from center where camera starts following
    public bool enableVerticalFollow = true; // Toggle vertical follow entirely

    [Header("Zone Settings")]
    public CameraZone currentZone;
    private Vector2 minBounds, maxBounds;

    [Header("Debug")]
    public bool enableDebug = true;
    public bool showGizmos = true;

    private Vector3 velocity = Vector3.zero;
    private float lastPlayerX;
    private float lookAheadDirection;
    private float lookAheadTarget;
    private Vector3 currentTargetPos;

    private void Start()
    {
        if (player != null)
        {
            lastPlayerX = player.position.x;
            lookAheadDirection = 1f;
            lookAheadTarget = lookAhead.x;
            // Start at player position but clamped to zone
            Vector3 startPos = player.position;
            startPos.z = transform.position.z;
            transform.position = ApplyBounds(startPos);
            currentTargetPos = transform.position;
        }
    }

    private void LateUpdate()
    {
        if (player == null || currentZone == null) return;

        minBounds = currentZone.minBounds;
        maxBounds = currentZone.maxBounds;

        // Calculate target position with Hollow Knight-style behavior
        currentTargetPos = CalculateHollowKnightTarget();

        // Smoothly follow towards target
        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, currentTargetPos, ref velocity, smoothSpeed);
        smoothedPos.z = transform.position.z;

        transform.position = smoothedPos;

        if (enableDebug)
        {
            Debug.Log($"CameraController: Player Y: {player.position.y}, Target Y: {currentTargetPos.y}, Current Y: {transform.position.y}");
        }
    }

    private Vector3 CalculateHollowKnightTarget()
    {
        Vector3 targetPos = transform.position; // Start from current camera position

        // Always follow horizontally with look-ahead
        float currentPlayerX = player.position.x;
        float xMovement = currentPlayerX - lastPlayerX;

        if (Mathf.Abs(xMovement) > 0.01f)
        {
            lookAheadDirection = Mathf.Sign(xMovement);
        }

        lastPlayerX = currentPlayerX;
        lookAheadTarget = lookAhead.x * lookAheadDirection;

        targetPos.x = player.position.x + lookAheadTarget;

        // Hollow Knight vertical behavior
        if (enableVerticalFollow)
        {
            float verticalDelta = player.position.y - transform.position.y;
            float absVerticalDelta = Mathf.Abs(verticalDelta);

            if (absVerticalDelta > verticalFollowThreshold)
            {
                // Player is far from camera center - follow directly
                targetPos.y = player.position.y;
            }
            else if (absVerticalDelta > verticalDeadZone)
            {
                // Player is outside dead zone but within threshold - partial follow
                // This creates the "soft" follow effect
                float followFactor = (absVerticalDelta - verticalDeadZone) / (verticalFollowThreshold - verticalDeadZone);
                targetPos.y = Mathf.Lerp(transform.position.y, player.position.y, followFactor);
            }
            // Else: Player is within dead zone - don't move vertically
        }
        else
        {
            // No vertical follow - keep current Y
            targetPos.y = transform.position.y;
        }

        return ApplyBounds(targetPos);
    }

    private Vector3 ApplyBounds(Vector3 position)
    {
        Vector3 boundedPos = position;

        if (minBounds.x < maxBounds.x)
        {
            boundedPos.x = Mathf.Clamp(boundedPos.x, minBounds.x, maxBounds.x);
        }

        if (minBounds.y < maxBounds.y)
        {
            boundedPos.y = Mathf.Clamp(boundedPos.y, minBounds.y, maxBounds.y);
        }

        return boundedPos;
    }

    // Draw debug gizmos
    private void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        // Draw camera bounds
        if (currentZone != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (minBounds.x + maxBounds.x) * 0.5f,
                (minBounds.y + maxBounds.y) * 0.5f,
                transform.position.z
            );
            Vector3 size = new Vector3(
                maxBounds.x - minBounds.x,
                maxBounds.y - minBounds.y,
                0.1f
            );
            Gizmos.DrawWireCube(center, size);
        }

        // Draw vertical thresholds
        if (enableVerticalFollow)
        {
            Vector3 camPos = transform.position;

            // Dead zone (green)
            Gizmos.color = Color.green;
            float deadZoneTop = camPos.y + verticalDeadZone;
            float deadZoneBottom = camPos.y - verticalDeadZone;
            Gizmos.DrawLine(new Vector3(camPos.x - 5, deadZoneTop, camPos.z), new Vector3(camPos.x + 5, deadZoneTop, camPos.z));
            Gizmos.DrawLine(new Vector3(camPos.x - 5, deadZoneBottom, camPos.z), new Vector3(camPos.x + 5, deadZoneBottom, camPos.z));

            // Follow threshold (red)
            Gizmos.color = Color.red;
            float thresholdTop = camPos.y + verticalFollowThreshold;
            float thresholdBottom = camPos.y - verticalFollowThreshold;
            Gizmos.DrawLine(new Vector3(camPos.x - 5, thresholdTop, camPos.z), new Vector3(camPos.x + 5, thresholdTop, camPos.z));
            Gizmos.DrawLine(new Vector3(camPos.x - 5, thresholdBottom, camPos.z), new Vector3(camPos.x + 5, thresholdBottom, camPos.z));

            // Draw current target
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(new Vector3(currentTargetPos.x, currentTargetPos.y, camPos.z), new Vector3(1f, 1f, 0.1f));
        }
    }

    public static void ToggleFollow(bool follow)
    {
        CameraController[] cameras = FindObjectsOfType<CameraController>();
        foreach (CameraController cam in cameras)
        {
            cam.enableVerticalFollow = follow;
        }
    }

    public void SetZone(CameraZone zone)
    {
        currentZone = zone;
        if (enableDebug)
        {
            Debug.Log($"CameraController: Zone changed to {zone?.name}");
        }
    }
}