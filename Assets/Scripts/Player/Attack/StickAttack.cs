using System.Collections;
using UnityEngine;

public class StickAttack : MonoBehaviour
{
    public enum Direction { Up, Right, Left, Down }

    [Header("Preset Transforms (local)")]
    public Vector3 upRotation = new Vector3(331.752f, 16.407f, 112.33f);
    public Vector3 upPosition = new Vector3(-2.15799999f, 1.86300004f, 0.0260000005f);
    public Vector3 rightRotation = new Vector3(12.3462868f, 30.1126213f, 33.9791946f);
    public Vector3 rightPosition = new Vector3(-0.907000005f, -0.349999994f, 0.0260000005f);
    public Vector3 leftRotation = new Vector3(12.3462868f, 180.791306f, 33.9791946f);
    public Vector3 leftPosition = new Vector3(-4.21000004f, -0.300000012f, 0.0260000005f);
    public Vector3 downRotation = new Vector3(352.184174f, 170.380417f, 261.541473f);
    public Vector3 downPosition = new Vector3(-2.1400001f, -2.3499999f, 0.0260000005f);

    [Header("Position Adjustments")]
    public float horizontalDistance = 1.5f; 
    public float verticalDistance = 1.5f;   
    public float centerX = 0f;             

    [Header("References")]
    public ParticleSystem swingEffectPrefab;

    [Header("Swing Settings")]
    public Direction swingDirection = Direction.Right;
    public KeyCode swingKey = KeyCode.Mouse0;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode upKey = KeyCode.W;
    public KeyCode downKey = KeyCode.S;
    public float swingDuration = 0.25f;
    public bool resetToInitialPoseAfterSwing = true;

    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private bool isSwinging = false;
    private Coroutine currentSwingCoroutine;

    void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;

        Debug.Log("StickAttack initialized");
        ApplyDistanceAdjustments();
    }

    void Update()
    {
        UpdateSwingDirection();

        if (!isSwinging && Input.GetKeyDown(swingKey))
        {
            if (currentSwingCoroutine != null)
                StopCoroutine(currentSwingCoroutine);

            currentSwingCoroutine = StartCoroutine(DoSwing(swingDirection));
        }
    }

    private void UpdateSwingDirection()
    {
        if (!isSwinging)
        {
            if (Input.GetKey(leftKey))
            {
                swingDirection = Direction.Left;
            }
            else if (Input.GetKey(rightKey))
            {
                swingDirection = Direction.Right;
            }
            else if (Input.GetKey(upKey))
            {
                swingDirection = Direction.Up;
            }
            else if (Input.GetKey(downKey))
            {
                swingDirection = Direction.Down;
            }
        }
    }

    public IEnumerator DoSwing(Direction direction)
    {
        isSwinging = true;

        Debug.Log("Starting swing in direction: " + direction);

        (Vector3 localPos, Quaternion localRot) = GetPoseForDirection(direction);

        transform.localPosition = localPos;
        transform.localRotation = localRot;

        Debug.Log("Swing position - Local: " + localPos + ", World: " + transform.position);

        SpawnEffectAtLocalPose(localPos, localRot);

        yield return new WaitForSeconds(swingDuration);

        if (resetToInitialPoseAfterSwing)
        {
            transform.localPosition = initialLocalPos;
            transform.localRotation = initialLocalRot;
        }

        isSwinging = false;
        currentSwingCoroutine = null;
    }

    private (Vector3, Quaternion) GetPoseForDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up:
                return (upPosition, Quaternion.Euler(upRotation));
            case Direction.Right:
                return (rightPosition, Quaternion.Euler(rightRotation));
            case Direction.Left:
                return (leftPosition, Quaternion.Euler(leftRotation));
            case Direction.Down:
                return (downPosition, Quaternion.Euler(downRotation));
            default:
                return (initialLocalPos, initialLocalRot);
        }
    }

    private void SpawnEffectAtLocalPose(Vector3 localPos, Quaternion localRot)
    {
        if (swingEffectPrefab == null)
        {
            Debug.LogWarning("Swing effect prefab is not assigned!");
            return;
        }

        try
        {
            if (transform.parent == null)
            {
                Debug.LogError("Stick has no parent! Cannot calculate world position properly.");
                return;
            }

            Vector3 worldPos = transform.parent.position + localPos;

            Quaternion worldRot = transform.parent.rotation * localRot;

            ParticleSystem ps = Instantiate(swingEffectPrefab, worldPos, worldRot, transform.parent);

            ps.Play();

            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(ps.gameObject, lifetime);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error spawning particle effect: " + e.Message);
        }
    }

    private void ApplyDistanceAdjustments()
    { 
        rightPosition = new Vector3(horizontalDistance, rightPosition.y, rightPosition.z);
        leftPosition = new Vector3(-horizontalDistance, leftPosition.y, leftPosition.z);

        upPosition = new Vector3(centerX, verticalDistance, upPosition.z);
        downPosition = new Vector3(centerX, -verticalDistance, downPosition.z);
    }

    public void AutoAdjustPositions()
    {
        ApplyDistanceAdjustments();
        Debug.Log("Positions adjusted");
        PrintCurrentPositions();
    }

    [ContextMenu("Print Current Positions")]
    public void PrintCurrentPositions()
    {
        Debug.Log("=== CURRENT POSITIONS ===");
        Debug.Log("Right: " + rightPosition);
        Debug.Log("Left: " + leftPosition);
        Debug.Log("Up: " + upPosition);
        Debug.Log("Down: " + downPosition);
    }

    [ContextMenu("Center Up/Down Attacks")]
    public void CenterUpDownAttacks()
    {
        centerX = 0f;
        ApplyDistanceAdjustments();
        Debug.Log("Up/Down attacks centered on X-axis");
    }

    public void TriggerSwing(Direction direction = Direction.Right)
    {
        if (!isSwinging)
        {
            if (currentSwingCoroutine != null)
                StopCoroutine(currentSwingCoroutine);

            currentSwingCoroutine = StartCoroutine(DoSwing(direction));
        }
    }

    void OnDrawGizmosSelected()
    {
        if (transform.parent == null) return;

        if (!Application.isPlaying)
        {
            ApplyDistanceAdjustments();
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.parent.position + upPosition, 0.2f);
        Gizmos.DrawLine(transform.parent.position, transform.parent.position + upPosition);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.parent.position + rightPosition, 0.2f);
        Gizmos.DrawLine(transform.parent.position, transform.parent.position + rightPosition);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.parent.position + leftPosition, 0.2f);
        Gizmos.DrawLine(transform.parent.position, transform.parent.position + leftPosition);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.parent.position + downPosition, 0.2f);
        Gizmos.DrawLine(transform.parent.position, transform.parent.position + downPosition);
    }
}