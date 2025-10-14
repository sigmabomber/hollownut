using System.Collections.Generic;
using UnityEngine;

public class RollingBouncingBall : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rollingSpeed = 5f;
    public float bounceHeight = 5f;
    public float bounceDuration = 3f;

    [Header("Physics Settings")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public PhysicsMaterial2D bounceMaterial;
    public PhysicsMaterial2D rollMaterial;

    [Header("Visual Settings")]
    public float rollingRotationSpeed = 360f;
    public float bouncingRotationSpeed = 720f;

    [Header("Raycast Settings")]
    public float wallCheckDistance = 0.2f;
    public int horizontalRays = 3;
    public float raySpacing = 0.1f;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private SpriteRenderer spriteRenderer;
    private HealthModule healthModule;
    private bool movingRight = true;
    private bool isGrounded = false;
    private bool canFlip = true;
    private bool isBouncing = false;
    private float bounceTimer = 0f;
    public bool bounce = false;

    public List<Sprite> acornStates = new();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        healthModule = GetComponent<HealthModule>();
        rb.gravityScale = 2f;

        if (bounceMaterial == null)
        {
            bounceMaterial = new PhysicsMaterial2D("Bounce");
            bounceMaterial.friction = 0f;
            bounceMaterial.bounciness = 0f;
        }

        if (rollMaterial == null)
        {
            rollMaterial = new PhysicsMaterial2D("Roll");
            rollMaterial.friction = 0.4f;
            rollMaterial.bounciness = 0f;
        }

        SetRollingMode();

        if (healthModule != null)
        {
            healthModule.Initialize(5);

            healthModule.onHealthChanged += HandleHealthChange;
            healthModule.onDeath += HandleDeath;
        }
    }

    void HandleDeath()
    {
        healthModule.onDeath -= HandleDeath;
        healthModule.onHealthChanged -= HandleHealthChange;
        Destroy(gameObject);
    }

    void HandleHealthChange(float current, float max)
    {
        int index = Mathf.Clamp((int)current, 0, acornStates.Count - 1);
        spriteRenderer.sprite = acornStates[index];
    }

    void FixedUpdate()
    {
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, col.radius + 0.1f, groundLayer);
        bool wasGrounded = isGrounded;
        isGrounded = groundHit.collider != null;

        // Check for walls using raycasting
        CheckForWalls();

        if (isBouncing)
        {
            bounceTimer -= Time.fixedDeltaTime;

            if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0)
            {
                Launch();
            }

            float currentXVel = movingRight ? rollingSpeed : -rollingSpeed;
            rb.linearVelocity = new Vector2(currentXVel, rb.linearVelocity.y);

            RotateBouncing();

            if (bounceTimer <= 0 && isGrounded)
            {
                SetRollingMode();
            }
        }
        else
        {
            float targetXVel = movingRight ? rollingSpeed : -rollingSpeed;
            rb.linearVelocity = new Vector2(targetXVel, rb.linearVelocity.y);

            RotateRolling();
        }

        if (bounce)
        {
            StartBouncing(5f);
            bounce = false;
        }
    }

    void CheckForWalls()
    {
        if (!canFlip) return;

        Vector2 direction = movingRight ? Vector2.right : Vector2.left;
        Vector2 origin = (Vector2)transform.position;
        float radius = col.radius;

        // Cast multiple rays vertically to detect walls at different heights
        for (int i = 0; i < horizontalRays; i++)
        {
            float verticalOffset = (i - (horizontalRays - 1) * 0.5f) * raySpacing;
            Vector2 rayOrigin = origin + new Vector2(0, verticalOffset);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, direction, radius + wallCheckDistance, wallLayer);

            // Debug visualization
            Debug.DrawRay(rayOrigin, direction * (radius + wallCheckDistance), hit.collider != null ? Color.red : Color.green);

            if (hit.collider != null)
            {
                // Only flip if the wall is primarily horizontal (normal x is significant)
                if (Mathf.Abs(hit.normal.x) > Mathf.Abs(hit.normal.y))
                {
                    FlipDirection();
                    break; // Only flip once per frame
                }
            }
        }
    }

    void RotateRolling()
    {
        float direction = movingRight ? -1f : 1f;
        float rotationAmount = direction * rollingRotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, 0, rotationAmount);
    }

    void RotateBouncing()
    {
        float direction = movingRight ? -1f : 1f;
        float rotationAmount = direction * bouncingRotationSpeed * Time.fixedDeltaTime;
        transform.Rotate(0, 0, rotationAmount);
    }

    void Launch()
    {
        float xVel = movingRight ? rollingSpeed : -rollingSpeed;
        float yVel = Mathf.Sqrt(2 * bounceHeight * Mathf.Abs(Physics2D.gravity.y * rb.gravityScale));
        rb.linearVelocity = new Vector2(xVel, yVel);
    }

    void FlipDirection()
    {
        movingRight = !movingRight;
        canFlip = false;
        Invoke(nameof(ResetFlip), 0.2f);

        float newXVel = movingRight ? rollingSpeed : -rollingSpeed;
        float yVel = isBouncing ? Mathf.Max(rb.linearVelocity.y, 2f) : rb.linearVelocity.y;
        rb.linearVelocity = new Vector2(newXVel, yVel);
    }

    void ResetFlip()
    {
        canFlip = true;
    }

    void SetRollingMode()
    {
        isBouncing = false;

        if (col != null)
        {
            col.sharedMaterial = rollMaterial;
        }

        rb.linearVelocity = new Vector2(movingRight ? rollingSpeed : -rollingSpeed, rb.linearVelocity.y);
    }

    void SetBouncingMode()
    {
        isBouncing = true;
        bounceTimer = bounceDuration;

        if (col != null)
        {
            col.sharedMaterial = bounceMaterial;
        }

        if (isGrounded)
        {
            Launch();
        }
        else
        {
            rb.linearVelocity = new Vector2(movingRight ? rollingSpeed : -rollingSpeed, rb.linearVelocity.y);
        }
    }

    public void StartBouncing(float duration = -1f)
    {
        if (duration > 0)
        {
            bounceDuration = duration;
        }

        SetBouncingMode();
    }

    public void StartRolling()
    {
        SetRollingMode();
    }

    public bool IsBouncing()
    {
        return isBouncing;
    }
}