using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    [Header("Movement State")]
    private float horizontalInput;
    private bool facingRight = true;
    private bool isGrounded;
    private int airJumpsRemaining;

    [Header("Jump State")]
    private bool jumpPressed;
    private bool jumpHeld;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;

    [Header("Dash State")]
    private bool isDashing;
    private bool canDash = true;
    private float dashCooldownTimer;

    [Header("Wall State")]
    private bool isOnWall;
    private bool isWallSliding;
    private int wallDirection;
    private float wallStickCounter;

    [Header("Quick Drop State")]
    private bool quickDropPressed;
    private bool isQuickDropping;
    private float quickDropCooldownTimer;

    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 45f;
    [SerializeField] private float airAcceleration = 45f;
    [SerializeField] private float airDeceleration = 35f;
    [SerializeField] private float gravityScale = 3.5f;
    [SerializeField] private float fallMultiplier = 1.5f; 

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 16f;
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private int maxAirJumps = 1;
    [SerializeField] private float variableJumpHeight = 0.5f;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 28f;
    [SerializeField] private float dashTime = 0.12f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private float dashEndSpeedMultiplier = 0.6f;
    [SerializeField] private bool canDashInAir = true;
    [SerializeField] private bool resetDashOnGround = true;

    [Header("Wall Settings")]
    [SerializeField] private float wallSlideSpeed = 2.5f;
    [SerializeField] private float wallJumpForce = 18f;
    [SerializeField] private Vector2 wallJumpAngle = new Vector2(1.2f, 1.8f);
    [SerializeField] private float wallStickTime = 0.15f;
    [SerializeField] private float wallJumpLockTime = 0.1f; 

    [Header("Quick Drop Settings")]
    [SerializeField] private float quickDropSpeed = 30f;
    [SerializeField] private float quickDropDuration = 0.18f;
    [SerializeField] private float quickDropCooldown = 0.25f;
    [SerializeField] private bool cancelQuickDropOnJump = true;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);
    [SerializeField] private LayerMask groundLayer = ~0; 

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallCheckHeight = 1f;

    private float wallJumpLockTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        rb.gravityScale = gravityScale;

        groundLayer = LayerMask.GetMask("Ground");
        if (groundLayer == 0)
        {
            Debug.LogWarning("Ground layer not found! Using Default layer. Please create a 'Ground' layer.");
            groundLayer = LayerMask.GetMask("Default");
        }

    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        CheckWall();
        HandleJump();
        HandleQuickDrop();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        if (isQuickDropping)
        {
            ApplyQuickDrop();
            return;
        }

        if (isWallSliding)
        {
            HandleWallSlide();
        }
        else
        {
            HandleMovement();
        }

        ApplyBetterFallPhysics();
        HandleFacingDirection();
    }

    private void GetInput()
    {
        horizontalInput = 0;
        if (Input.GetKey(Constants.PlayerData.PlayerControls.left)) horizontalInput -= 1;
        if (Input.GetKey(Constants.PlayerData.PlayerControls.right)) horizontalInput += 1;

        jumpPressed = Input.GetKeyDown(Constants.PlayerData.PlayerControls.jump);
        jumpHeld = Input.GetKey(Constants.PlayerData.PlayerControls.jump);

        if (jumpPressed)
        {
            jumpBufferCounter = jumpBufferTime;
        }

        if (Input.GetKeyDown(Constants.PlayerData.PlayerControls.dash) && canDash && (isGrounded || canDashInAir))
        {
            StartCoroutine(Dash());
        }

        quickDropPressed = Input.GetKeyDown(Constants.PlayerData.PlayerControls.down);
    }

    private void CheckGrounded()
    {
        Vector2 castOrigin = (Vector2)transform.position + groundCheckOffset;
        bool wasGrounded = isGrounded;

        RaycastHit2D hit = Physics2D.BoxCast(castOrigin, groundCheckSize, 0f, Vector2.down, groundCheckDistance, groundLayer);

        isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;

        if (isGrounded && !wasGrounded)
        {
            OnLanded();
        }

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            airJumpsRemaining = maxAirJumps;

            if (resetDashOnGround)
                canDash = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void OnLanded()
    {
        if (isQuickDropping)
        {
            StopQuickDrop();
        }
    }

    private void CheckWall()
    {
        if (wallJumpLockTimer > 0) return;

        int direction = facingRight ? 1 : -1;
        Vector2 rayOrigin = transform.position;

        bool hitWall = Physics2D.Raycast(rayOrigin, Vector2.right * direction, wallCheckDistance, groundLayer);

        isOnWall = hitWall && !isGrounded && rb.linearVelocity.y <= 0;

        if (isOnWall && Mathf.Sign(horizontalInput) == direction)
        {
            isWallSliding = true;
            wallDirection = direction;
            wallStickCounter = wallStickTime;
        }
        else
        {
            wallStickCounter -= Time.deltaTime;
            if (wallStickCounter <= 0)
            {
                isWallSliding = false;
            }
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = horizontalInput * maxSpeed;
        float currentSpeed = rb.linearVelocity.x;
        float speedDiff = targetSpeed - currentSpeed;

        float accelRate;
        if (Mathf.Abs(targetSpeed) > 0.01f)
        {
            accelRate = isGrounded ? acceleration : airAcceleration;
        }
        else
        {
            accelRate = isGrounded ? deceleration : airDeceleration;
        }

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;

        rb.linearVelocity = new Vector2(Mathf.Clamp(currentSpeed + movement, -maxSpeed, maxSpeed), rb.linearVelocity.y);
    }

    private void ApplyBetterFallPhysics()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void HandleWallSlide()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
    }

    private void HandleJump()
    {
        if (!jumpHeld && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        if (jumpBufferCounter > 0 && !isQuickDropping)
        {
            if (coyoteTimeCounter > 0)
            {
                PerformJump(Vector2.up * jumpForce);
                jumpBufferCounter = 0;
                coyoteTimeCounter = 0;
            }
            else if (isWallSliding)
            {
                PerformWallJump();
                jumpBufferCounter = 0;
            }
            else if (airJumpsRemaining > 0)
            {
                PerformJump(Vector2.up * jumpForce);
                airJumpsRemaining--;
                jumpBufferCounter = 0;
            }
        }
    }

    private void HandleQuickDrop()
    {
        if (quickDropPressed && !isGrounded && !isQuickDropping && quickDropCooldownTimer <= 0)
        {
            StartCoroutine(QuickDrop());
        }
    }

    private IEnumerator QuickDrop()
    {
        isQuickDropping = true;
        quickDropCooldownTimer = quickDropCooldown;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(0.7f, 0.9f, 1f, 0.8f);

        float timer = 0f;
        while (timer < quickDropDuration && !isGrounded)
        {
            if (cancelQuickDropOnJump && jumpPressed)
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopQuickDrop();
    }

    private void ApplyQuickDrop()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, -quickDropSpeed);
    }

    private void StopQuickDrop()
    {
        isQuickDropping = false;
        rb.gravityScale = gravityScale;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }

    private void PerformJump(Vector2 force)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private void PerformWallJump()
    {
        isWallSliding = false;
        wallJumpLockTimer = wallJumpLockTime;

        Vector2 jumpDir = new Vector2(-wallDirection * wallJumpAngle.x, wallJumpAngle.y).normalized;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);

        bool shouldFaceRight = wallDirection < 0; 
        if (facingRight != shouldFaceRight)
        {
            Flip();
        }

    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        Vector2 dashDirection = new Vector2(horizontalInput, 0);

        if (dashDirection == Vector2.zero)
        {
            dashDirection = facingRight ? Vector2.right : Vector2.left;
        }

        dashDirection = dashDirection.normalized;
        rb.linearVelocity = dashDirection * dashSpeed;

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1, 1, 1, 0.6f);

        yield return new WaitForSeconds(dashTime);

        rb.linearVelocity *= dashEndSpeedMultiplier;
        rb.gravityScale = originalGravity;
        isDashing = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        dashCooldownTimer = dashCooldown;
    }

    private void HandleFacingDirection()
    {
        if (isDashing || isQuickDropping || wallJumpLockTimer > 0) return;

        if (horizontalInput > 0.1f && !facingRight)
        {
            Flip();
        }
        else if (horizontalInput < -0.1f && facingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1);
    }

    private void UpdateTimers()
    {
        coyoteTimeCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
        wallJumpLockTimer -= Time.deltaTime;

        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
                canDash = true;
        }

        if (quickDropCooldownTimer > 0)
            quickDropCooldownTimer -= Time.deltaTime;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsWallSliding", isWallSliding);
        animator.SetBool("IsDashing", isDashing);
        animator.SetBool("IsQuickDropping", isQuickDropping);
        animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 castOrigin = (Vector2)transform.position + groundCheckOffset;
        Vector2 castEnd = castOrigin + Vector2.down * groundCheckDistance;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(castOrigin, groundCheckSize);
        Gizmos.DrawWireCube(castEnd, groundCheckSize);
        Gizmos.DrawLine(castOrigin, castEnd);

        Gizmos.color = isOnWall ? Color.blue : Color.yellow;
        int dir = facingRight ? 1 : -1;
        Gizmos.DrawLine(transform.position, (Vector2)transform.position + Vector2.right * dir * wallCheckDistance);
    }
    public bool IsGrounded() => isGrounded;
    public bool IsDashing() => isDashing;
    public bool IsWallSliding() => isWallSliding;
    public bool IsQuickDropping() => isQuickDropping;
    public Vector2 GetVelocity() => rb.linearVelocity;
    public bool CanDash() => canDash;
}