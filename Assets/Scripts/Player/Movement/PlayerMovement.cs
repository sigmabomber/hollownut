using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsWallSlidingHash = Animator.StringToHash("IsWallSliding");
    private static readonly int IsDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int IsQuickDroppingHash = Animator.StringToHash("IsQuickDropping");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsLookingUpHash = Animator.StringToHash("isLookingUp");
    private static readonly int IsFallingHash = Animator.StringToHash("IsFalling");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");

    [Header("Movement State")]
    private float horizontalInput;
    private bool facingRight = true;
    private bool isGrounded;
    private bool isFalling;
    private bool isJumping;
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

    [Header("Quick Drop State")]
    private bool quickDropPressed;
    private bool isQuickDropping;
    private float quickDropCooldownTimer;

    [Header("Movement Settings")]
    [SerializeField] private float maxWalkSpeed = 8.3f;
    [SerializeField] private float acceleration = 200f;
    [SerializeField] private float deceleration = 200f;
    [SerializeField] private float airAcceleration = 120f;
    [SerializeField] private float airDeceleration = 80f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallMultiplier = 2.5f;
    public float weight = 1f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private int maxAirJumps = 0;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 30f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 0.6f;
    [SerializeField] private float dashEndSpeedMultiplier = 0.5f;
    [SerializeField] private bool canDashInAir = true;
    [SerializeField] private bool resetDashOnGround = true;

    [Header("Wall Settings")]
    [SerializeField] private float wallJumpForce = 18f;
    [SerializeField] private Vector2 wallJumpAngle = new(1.2f, 1.8f);
    [SerializeField] private float wallJumpLockTime = 0.1f;

    [Header("Quick Drop Settings")]
    [SerializeField] private float quickDropSpeed = 30f;
    [SerializeField] private float quickDropDuration = 0.18f;
    [SerializeField] private float quickDropCooldown = 0.25f;
    [SerializeField] private bool cancelQuickDropOnJump = true;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private Vector2 groundCheckSize = new(0.8f, 0.1f);
    [SerializeField] private Vector2 groundCheckOffset = new(0, -0.5f);
    [SerializeField] private LayerMask groundLayer = ~0;

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private Vector2 wallCheckOffset = new(0.3f, 0f);
    [SerializeField] private float wallCheckHeight = 0.8f;

    [Header("Animation Settings")]
    [SerializeField] private float animationSmoothSpeed = 15f;

    private float wallJumpLockTimer;
    private float originalGravity = 3.5f;
    private bool isLookingUp = false;
    public float currentAnimSpeed = 0f;

    [Header("Keycodes")]
    private KeyCode leftKey = Constants.PlayerData.PlayerControls.left;
    private KeyCode rightKey = Constants.PlayerData.PlayerControls.right;
    private KeyCode jumpKey = Constants.PlayerData.PlayerControls.jump;
    private KeyCode dashKey = Constants.PlayerData.PlayerControls.dash;
    private KeyCode upKey = Constants.PlayerData.PlayerControls.up;
    private KeyCode downKey = Constants.PlayerData.PlayerControls.down;

    [Header("Input Settings")]
    [SerializeField] private float inputSmoothSpeed = 10f;



    public bool canMove = true;
    public bool canJump = true;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rb.gravityScale = gravityScale;
        originalGravity = rb.gravityScale;

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
        CheckFalling();
        CheckJumping();
        HandleJump();
        HandleQuickDrop();
        UpdateTimers();
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        if (isQuickDropping) { ApplyQuickDrop(); return; }

        HandleMovement();
        ApplyFallPhysics();
        HandleFacingDirection();
    }

    private void GetInput()
    {
        float targetInput = 0f;
        if (Input.GetKey(leftKey)) targetInput -= 1f;
        if (Input.GetKey(rightKey)) targetInput += 1f;
        if (canMove)
        horizontalInput = Mathf.MoveTowards(horizontalInput, targetInput, inputSmoothSpeed * Time.deltaTime);
        if (canJump)
        {
            jumpPressed = Input.GetKeyDown(jumpKey);
            jumpHeld = Input.GetKey(jumpKey);
        }
        if (jumpPressed) jumpBufferCounter = jumpBufferTime;

        if (Input.GetKeyDown(dashKey) && canDash && (isGrounded || canDashInAir)) StartCoroutine(Dash());

        if (Input.GetKeyDown(upKey) && isGrounded && Mathf.Abs(horizontalInput) < 0.1f) isLookingUp = true;
        if (Input.GetKeyUp(upKey) || Mathf.Abs(horizontalInput) > 0.1f) isLookingUp = false;

        animator.SetBool(IsLookingUpHash, isLookingUp);
    }

    private void CheckGrounded()
    {
        Vector2 castOrigin = (Vector2)transform.position + groundCheckOffset;
        bool wasGrounded = isGrounded;
        RaycastHit2D hit = Physics2D.BoxCast(castOrigin, groundCheckSize, 0f, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null && hit.collider.gameObject != gameObject;

        if (isGrounded && !wasGrounded) OnLanded();

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            airJumpsRemaining = maxAirJumps;
            if (resetDashOnGround) canDash = true;
        }
        else { coyoteTimeCounter -= Time.deltaTime; }
    }

    private void CheckWalls()
    {
        bool wasOnWall = isOnWall;
        isOnWall = false;
        isWallSliding = false;
        wallDirection = 0;

        if (!isGrounded && horizontalInput != 0)
        {
            Vector2 checkDirection = Vector2.right * Mathf.Sign(horizontalInput);
            Vector2 checkOrigin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * checkDirection.x, 0);

            RaycastHit2D hit1 = Physics2D.Raycast(checkOrigin, checkDirection, wallCheckDistance, groundLayer);
            RaycastHit2D hit2 = Physics2D.Raycast(checkOrigin + Vector2.up * wallCheckHeight * 0.5f, checkDirection, wallCheckDistance, groundLayer);
            RaycastHit2D hit3 = Physics2D.Raycast(checkOrigin + Vector2.up * wallCheckHeight, checkDirection, wallCheckDistance, groundLayer);

            isOnWall = hit1.collider != null || hit2.collider != null || hit3.collider != null;

            if (isOnWall)
            {
                wallDirection = (int)Mathf.Sign(horizontalInput);

                if (rb.linearVelocity.y < 0)
                {
                    isWallSliding = true;
                }
            }
        }
    }

    private void CheckFalling()
    {
        isFalling = !isGrounded && !isWallSliding && !isDashing && !isQuickDropping && rb.linearVelocity.y < 0.1f;
    }

    private void CheckJumping()
    {
        isJumping = !isGrounded && !isWallSliding && !isDashing && !isQuickDropping && rb.linearVelocity.y > 0.1f;
    }

    private void OnLanded() { if (isQuickDropping) StopQuickDrop(); }

    private void HandleMovement()
    {
        if (wallJumpLockTimer > 0) return;

        float currentMaxSpeed = maxWalkSpeed / weight;
        float targetSpeed = horizontalInput * currentMaxSpeed;
        float currentSpeed = rb.linearVelocity.x;
        float speedDiff = targetSpeed - currentSpeed;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? (isGrounded ? acceleration : airAcceleration) : (isGrounded ? deceleration : airDeceleration);

        float movement = speedDiff * accelRate * Time.fixedDeltaTime;
        float newVelocityX = Mathf.Clamp(currentSpeed + movement, -currentMaxSpeed, currentMaxSpeed);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
    }

    private void ApplyFallPhysics()
    {
        if (rb.linearVelocity.y < 0) rb.linearVelocity += ((fallMultiplier - 1) * Physics2D.gravity.y * Time.fixedDeltaTime * Vector2.up) * weight;
    }

    private void HandleJump()
    {
        if (!jumpHeld && rb.linearVelocity.y > 0) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        if (jumpBufferCounter > 0 && !isQuickDropping)
        {
            float effectiveJumpForce = jumpForce / Mathf.Sqrt(weight);
            if (coyoteTimeCounter > 0) { PerformJump(Vector2.up * effectiveJumpForce); jumpBufferCounter = 0; coyoteTimeCounter = 0; }
            else if (isWallSliding) { PerformWallJump(); jumpBufferCounter = 0; }
            else if (airJumpsRemaining > 0) { PerformJump(Vector2.up * effectiveJumpForce); airJumpsRemaining--; jumpBufferCounter = 0; }
        }
    }

    private void HandleQuickDrop() { if (quickDropPressed && !isGrounded && !isQuickDropping && quickDropCooldownTimer <= 0) StartCoroutine(QuickDrop()); }

    private IEnumerator QuickDrop()
    {
        isQuickDropping = true;
        quickDropCooldownTimer = quickDropCooldown;
        rb.gravityScale = 0;

        float timer = 0f;
        while (timer < quickDropDuration && !isGrounded)
        {
            if (cancelQuickDropOnJump && jumpPressed) break;
            timer += Time.deltaTime;
            yield return null;
        }
        StopQuickDrop();
    }

    private void ApplyQuickDrop() { rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, -quickDropSpeed); }

    private void StopQuickDrop() { isQuickDropping = false; rb.gravityScale = gravityScale; if (spriteRenderer != null) spriteRenderer.color = Color.white; }

    private void PerformJump(Vector2 force) { rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); rb.AddForce(force, ForceMode2D.Impulse); }

    private void PerformWallJump()
    {
        isWallSliding = false;
        wallJumpLockTimer = wallJumpLockTime;
        Vector2 jumpDir = new Vector2(-wallDirection * wallJumpAngle.x, wallJumpAngle.y).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(jumpDir * wallJumpForce, ForceMode2D.Impulse);

        bool shouldFaceRight = wallDirection < 0;
        if (facingRight != shouldFaceRight) Flip();
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        rb.gravityScale = 0;

        Vector2 dashDir = new(horizontalInput, 0);
        if (dashDir == Vector2.zero) dashDir = facingRight ? Vector2.right : Vector2.left;
        rb.linearVelocity = dashDir.normalized * dashSpeed;

        yield return new WaitForSeconds(dashTime / weight);
        rb.linearVelocity *= dashEndSpeedMultiplier;
        rb.gravityScale = originalGravity;
        isDashing = false;
        dashCooldownTimer = dashCooldown;
    }

    private void HandleFacingDirection()
    {
        if (isDashing || isQuickDropping || wallJumpLockTimer > 0) return;
        if (horizontalInput > 0.1f && !facingRight) Flip();
        else if (horizontalInput < -0.1f && facingRight) Flip();
    }

    private void Flip() { facingRight = !facingRight; transform.localScale = new Vector3(facingRight ? 1 : -1, 1, 1); }

    private void UpdateTimers()
    {
        coyoteTimeCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
        wallJumpLockTimer -= Time.deltaTime;

        if (dashCooldownTimer > 0) { dashCooldownTimer -= Time.deltaTime; if (dashCooldownTimer <= 0) canDash = true; }
        if (quickDropCooldownTimer > 0) quickDropCooldownTimer -= Time.deltaTime;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float actualSpeed = Mathf.Abs(rb.linearVelocity.x);
        float maxSpeed = maxWalkSpeed / weight;
        float normalizedSpeed = Mathf.Clamp01(actualSpeed / maxSpeed);
        float inputInfluence = Mathf.Abs(horizontalInput);

        float targetSpeed = Mathf.Lerp(normalizedSpeed, inputInfluence, 0.3f);

        float smoothFactor = animationSmoothSpeed * Time.deltaTime;

        if (targetSpeed < currentAnimSpeed)
        {
            smoothFactor *= 2f;
        }

        currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, targetSpeed, smoothFactor);

        if (currentAnimSpeed < 0.01f)
        {
            currentAnimSpeed = 0f;
        }

        animator.SetFloat(SpeedHash, currentAnimSpeed);
        animator.SetBool(IsGroundedHash, isGrounded);
        animator.SetBool(IsWallSlidingHash, isWallSliding);
        animator.SetBool(IsDashingHash, isDashing);
        animator.SetBool(IsQuickDroppingHash, isQuickDropping);
        animator.SetBool(IsFallingHash, isFalling);
        animator.SetBool(IsJumpingHash, isJumping);
        animator.SetFloat(VerticalVelocityHash, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector2 groundCastOrigin = (Vector2)transform.position + groundCheckOffset;
        Gizmos.DrawWireCube(groundCastOrigin + Vector2.down * groundCheckDistance * 0.5f, new Vector3(groundCheckSize.x, groundCheckSize.y + groundCheckDistance, 0));

        if (Application.isPlaying)
        {
            Gizmos.color = isOnWall ? Color.yellow : Color.blue;
            Vector2 wallCheckDir = Vector2.right * wallDirection;
            Vector2 wallCheckOrigin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * wallCheckDir.x, 0);

            Gizmos.DrawLine(wallCheckOrigin, wallCheckOrigin + wallCheckDir * wallCheckDistance);
            Gizmos.DrawLine(wallCheckOrigin + Vector2.up * wallCheckHeight * 0.5f, wallCheckOrigin + Vector2.up * wallCheckHeight * 0.5f + wallCheckDir * wallCheckDistance);
            Gizmos.DrawLine(wallCheckOrigin + Vector2.up * wallCheckHeight, wallCheckOrigin + Vector2.up * wallCheckHeight + wallCheckDir * wallCheckDistance);
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool IsFalling() => isFalling;
    public bool IsJumping() => isJumping;
    public bool IsDashing() => isDashing;
    public bool IsWallSliding() => isWallSliding;
    public bool IsQuickDropping() => isQuickDropping;
    public Vector2 GetVelocity() => rb.linearVelocity;
    public bool CanDash() => canDash;
}