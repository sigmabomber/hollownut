using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(EnemyModule))]
public class AntlionLarva : MonoBehaviour
{
    [Header("Attack Settings")]
    public float undergroundDuration = 1.5f;
    public float jumpForce = 5f;
    public float dashSpeed = 15f;
    public float dashDuration = 0.5f;
    public float behindOffset = 2f;
    public float burrowSpeed = 8f;
    public float burrowDepth = 3f;
    public float bounceForce = 8f;
    public float bounceDistance = 3f;
    public float emergeJumpForce = 7f;

    [Header("Burrowing Visual Settings")]
    public float landPauseDuration = 0.3f;
    public float burrowInDuration = 0.8f;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3f;
    public float patrolChangeTime = 3f;
    public float groundSearchRadius = 5f;

    [Header("Enemy Module Settings")]
    public float attackCooldown = 3f;
    public float detectionRadius = 8f;
    public float attackRadius = 4f;
    public LayerMask targetLayer = 1 << 6;

    [Header("Ground Check Settings")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.5f;
    public Transform groundCheckPoint;

    [Header("Visual Settings")]
    public float burrowVisibilityDuration = 0.5f;
    public float fadeSpeed = 3f;
    public Sprite detectionSprite;

    [Header("Collision Settings")]
    public LayerMask playerLayer;
    public float hitDetectionRadius = 1f;

    private EnemyModule enemyModule;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private bool isDashing = false;
    private bool isUnderground = false;
    private bool isAttacking = false;
    private bool hasHitPlayer = false;
    private bool isChasing = false;
    private Color originalColor;
    private Vector3 surfacePosition;
    private float originalGravityScale;

    private GameObject cachedTarget;
    private Vector2 currentWalkDirection = Vector2.right;
    private float lastPatrolChangeTime;

    public GameObject Target => cachedTarget;

    private void Start()
    {
        enemyModule = GetComponent<EnemyModule>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        originalGravityScale = rb.gravityScale;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (groundCheckPoint == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = Vector3.down * 0.5f;
            groundCheckPoint = check.transform;
        }

        InitializeEnemyModule();
        lastPatrolChangeTime = Time.time;
    }

    private void Update()
    {
        if (!isAttacking && !isDashing && IsGrounded())
        {
            HandleMovement();
        }
    }

    private void InitializeEnemyModule()
    {
        if (enemyModule != null)
        {
            enemyModule.Initialize(
                cooldown: attackCooldown,
                detectionRange: detectionRadius,
                attackRange: attackRadius,
                layerMask: targetLayer,
                detectionCol: new Color(1f, 0f, 0f, 0.3f),
                attackCol: new Color(1f, 0.5f, 0f, 0.3f),
                sprite: detectionSprite
            );

            enemyModule.OnStartAttack += HandleStartAttack;
            enemyModule.OnTargetDetected += HandleTargetDetected;
        }
        else
        {
            Debug.LogError("EnemyModule component not found!");
        }
    }

    private void HandleTargetDetected(GameObject target)
    {
        if (!isAttacking && !isChasing)
        {
            cachedTarget = target;
            isChasing = true;
        }
    }

    private void HandleStartAttack()
    {
        if (!isAttacking && IsGrounded() && enemyModule.target != null)
        {
            cachedTarget = enemyModule.target;
            isChasing = false;
            StartCoroutine(PerformAttack());
        }
    }

    private void HandleMovement()
    {
        if (isChasing && cachedTarget != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void ChasePlayer()
    {
        if (cachedTarget == null) return;

        Vector2 direction = (cachedTarget.transform.position - transform.position).normalized;
        Vector2 moveDirection = new Vector2(direction.x, 0f).normalized;

        if (IsGroundAhead(moveDirection))
            rb.linearVelocity = new Vector2(moveDirection.x * chaseSpeed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void Patrol()
    {
        if (Time.time - lastPatrolChangeTime > patrolChangeTime)
        {
            currentWalkDirection = currentWalkDirection == Vector2.right ? Vector2.left : Vector2.right;
            lastPatrolChangeTime = Time.time;
        }

        if (IsGroundAhead(currentWalkDirection))
            rb.linearVelocity = new Vector2(currentWalkDirection.x * walkSpeed, rb.linearVelocity.y);
        else
        {
            currentWalkDirection = -currentWalkDirection;
            lastPatrolChangeTime = Time.time;
            rb.linearVelocity = new Vector2(currentWalkDirection.x * walkSpeed, rb.linearVelocity.y);
        }
    }

    private bool IsGroundAhead(Vector2 direction)
    {
        if (groundCheckPoint == null) return false;

        Vector2 checkPosition = (Vector2)groundCheckPoint.position + direction * 0.5f;
        RaycastHit2D hit = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);

        return hit.collider != null;
    }

    private void OnDestroy()
    {
        if (enemyModule != null)
        {
            enemyModule.OnStartAttack -= HandleStartAttack;
            enemyModule.OnTargetDetected -= HandleTargetDetected;
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        hasHitPlayer = false;

        float undergroundStartTime = Time.time;
        yield return StartCoroutine(BurrowUnderground());

        float timeSpentBurrowing = Time.time - undergroundStartTime;
        float remainingUndergroundTime = Mathf.Max(0.1f, undergroundDuration - timeSpentBurrowing);

        yield return StartCoroutine(MoveUndergroundBehindPlayer(remainingUndergroundTime));
        yield return StartCoroutine(EmergeFromGround());
        yield return StartCoroutine(DashTowardTarget());
        yield return StartCoroutine(EnsureSafeGround());

        isAttacking = false;
        cachedTarget = null;
    }

    private IEnumerator BurrowUnderground()
    {
        surfacePosition = transform.position;
        float burrowStartTime = Time.time;

        if (enemyCollider != null)
            enemyCollider.enabled = false;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        Vector3 undergroundPos = transform.position - new Vector3(0, burrowDepth, 0);
        float elapsed = 0f;

        while (elapsed < burrowInDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / burrowInDuration);

            transform.position = Vector3.Lerp(transform.position, undergroundPos, progress);
            float scale = Mathf.Lerp(1f, 0.8f, progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = c;
            }

            yield return null;
        }

        transform.localScale = Vector3.one;
        isUnderground = true;
    }

    private IEnumerator MoveUndergroundBehindPlayer(float movementDuration)
    {
        float startTime = Time.time;

        Vector3 targetPos;
        if (cachedTarget != null)
        {
            Vector3 dirToPlayer = (cachedTarget.transform.position - transform.position).normalized;
            targetPos = cachedTarget.transform.position - dirToPlayer * behindOffset;
        }
        else
        {
            targetPos = transform.position + Vector3.right * 1f;
        }

        Vector3 startPos = transform.position;

        while (Time.time - startTime < movementDuration)
        {
            float t = (Time.time - startTime) / movementDuration;
            transform.position = Vector3.Lerp(startPos, targetPos - new Vector3(0, burrowDepth, 0), t);
            yield return null;
        }

        transform.position = new Vector3(targetPos.x, transform.position.y, transform.position.z);
        surfacePosition = new Vector3(targetPos.x, surfacePosition.y, targetPos.z);
    }

    private IEnumerator EmergeFromGround()
    {
        isUnderground = false;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        Vector3 undergroundPosition = transform.position;
        Vector3 targetSurfacePosition = new Vector3(transform.position.x, surfacePosition.y, transform.position.z);

        float emergeTime = 0.5f;
        float elapsed = 0f;

        while (elapsed < emergeTime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / emergeTime);

            transform.position = Vector3.Lerp(undergroundPosition, targetSurfacePosition, progress);
            float scale = Mathf.Lerp(0.8f, 1f, progress);
            transform.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        transform.position = targetSurfacePosition;
        transform.localScale = Vector3.one;

        rb.gravityScale = originalGravityScale;
        if (enemyCollider != null)
            enemyCollider.enabled = true;

        rb.linearVelocity = new Vector2(0, emergeJumpForce);
        yield return new WaitUntil(() => rb.linearVelocity.y <= 0);
    }

    private IEnumerator DashTowardTarget()
    {
        if (cachedTarget == null)
            yield break;

        isDashing = true;

        float currentGravity = rb.gravityScale;
        rb.gravityScale = 0;

        float dashEndTime = Time.time + dashDuration;

        Vector2 dashTargetPos = cachedTarget.transform.position;
        Vector2 dashDirection = (dashTargetPos - (Vector2)transform.position).normalized;

        while (Time.time < dashEndTime)
        {
            rb.linearVelocity = dashDirection * dashSpeed;

            if (CheckPlayerCollision())
            {
                rb.linearVelocity = Vector2.zero;
                yield return StartCoroutine(JumpBackFromPlayer());
                break;
            }

            yield return null;
        }

        if (!hasHitPlayer)
        {
            rb.gravityScale = currentGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
            isDashing = false;

            while (!IsGrounded())
            {
                yield return null;
            }
        }

        isDashing = false;
    }

    private IEnumerator EnsureSafeGround()
    {
        yield return new WaitForSeconds(0.2f);

        if (!IsGrounded())
        {
            yield return StartCoroutine(FindNearestGround());
        }
    }

    private IEnumerator FindNearestGround()
    {
        Collider2D[] groundColliders = Physics2D.OverlapCircleAll(transform.position, groundSearchRadius, groundLayer);

        if (groundColliders.Length > 0)
        {
            Collider2D closestGround = null;
            float closestDistance = float.MaxValue;

            foreach (Collider2D ground in groundColliders)
            {
                float distance = Vector2.Distance(transform.position, ground.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGround = ground;
                }
            }

            if (closestGround != null)
            {
                Vector2 groundPos = closestGround.transform.position;
                Bounds groundBounds = closestGround.bounds;
                Vector2 targetPosition = new Vector2(groundBounds.center.x, groundBounds.max.y);

                float moveTime = 0f;
                float maxMoveTime = 2f;
                Vector2 startPosition = transform.position;

                while (moveTime < maxMoveTime && Vector2.Distance(transform.position, targetPosition) > 0.1f)
                {
                    moveTime += Time.deltaTime;
                    transform.position = Vector2.Lerp(startPosition, targetPosition, moveTime / maxMoveTime);
                    yield return null;
                }
            }
        }
    }

    private IEnumerator JumpBackFromPlayer()
    {
        rb.gravityScale = originalGravityScale;

        Vector3 jumpBackDirection;
        if (cachedTarget != null)
        {
            jumpBackDirection = (transform.position - cachedTarget.transform.position).normalized;
        }
        else
        {
            jumpBackDirection = Vector3.left;
        }

        Vector2 jumpBackForce = new Vector2(jumpBackDirection.x * bounceForce, bounceForce * 0.8f);

        rb.linearVelocity = jumpBackForce;

        while (!IsGrounded())
        {
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
    }

    private bool CheckPlayerCollision()
    {
        if (cachedTarget == null || hasHitPlayer) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitDetectionRadius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == cachedTarget)
            {
                hasHitPlayer = true;
                return true;
            }
        }

        float distToPlayer = Vector2.Distance(transform.position, cachedTarget.transform.position);
        if (distToPlayer < hitDetectionRadius)
        {
            hasHitPlayer = true;
            return true;
        }

        return false;
    }

    private bool IsGrounded()
    {
        if (groundCheckPoint == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(groundCheckPoint.position, groundCheckPoint.position + Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(groundCheckPoint.position + Vector3.down * groundCheckDistance, 0.1f);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitDetectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, groundSearchRadius);
    }
}
