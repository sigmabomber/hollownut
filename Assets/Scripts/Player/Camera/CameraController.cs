using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;
    public float smoothSpeed = 0.2f;
    public Vector2 lookAhead = new Vector2(1.5f, 0f); 

    [Header("Settings")]
    public float verticalDeadZone = 1f; 
    public float verticalFollowThreshold = 2f; 
    public bool enableVerticalFollow = true; 

    [Header("Zone Settings")]
    public CameraZone currentZone;
    private Vector2 minBounds, maxBounds;


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

        currentTargetPos = CalculateTarget();

        Vector3 smoothedPos = Vector3.SmoothDamp(transform.position, currentTargetPos, ref velocity, smoothSpeed);
        smoothedPos.z = transform.position.z;

        transform.position = smoothedPos;

        
    }

    private Vector3 CalculateTarget()
    {
        Vector3 targetPos = transform.position; 

        float currentPlayerX = player.position.x;
        float xMovement = currentPlayerX - lastPlayerX;

        if (Mathf.Abs(xMovement) > 0.01f)
        {
            lookAheadDirection = Mathf.Sign(xMovement);
        }

        lastPlayerX = currentPlayerX;
        lookAheadTarget = lookAhead.x * lookAheadDirection;

        targetPos.x = player.position.x + lookAheadTarget;

        if (enableVerticalFollow)
        {
            float verticalDelta = player.position.y - transform.position.y;
            float absVerticalDelta = Mathf.Abs(verticalDelta);

            if (absVerticalDelta > verticalFollowThreshold)
            {
                targetPos.y = player.position.y;
            }
            else if (absVerticalDelta > verticalDeadZone)
            {
                float followFactor = (absVerticalDelta - verticalDeadZone) / (verticalFollowThreshold - verticalDeadZone);
                targetPos.y = Mathf.Lerp(transform.position.y, player.position.y, followFactor);
            }
        }
        else
        {
            targetPos.y = transform.position.y;
        }

        return ApplyBounds(targetPos);
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (Time.timeScale <= 0.1) break; 

            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
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

  
    public static void ToggleFollow(bool follow)
    {
        CameraController[] cameras = FindObjectsByType<CameraController>(FindObjectsSortMode.None);

        foreach (CameraController cam in cameras)
        {
            cam.enableVerticalFollow = follow;
        }
    }
    public void DarkenArena(float darknessAmount)
    {
    }

    public void SetZone(CameraZone zone)
    {
        ToggleFollow(true);
        currentZone = zone;
       
    }
}