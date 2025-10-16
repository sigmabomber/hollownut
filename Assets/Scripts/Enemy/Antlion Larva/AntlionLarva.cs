using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

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
    public float emergeJumpForce = 7f;

    [Header("Burrowing Visual Settings")]
    public float landPauseDuration = 0.3f;
    public float burrowInDuration = 0.8f;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3f;
    public float patrolChangeTime = 3f;
    public float maxGroundSearchDistance = 10f;

    [Header("Enemy Module Settings")]
    public float attackCooldown = 3f;
    public float detectionRadius = 8f;
    public float attackRadius = 4f;
    public LayerMask targetLayer = 1 << 6;
    public LayerMask ignoreLayer;

    [Header("Attack Detection Settings")]
    public float dashAttackWidth = 1f;
    public float dashAttackHeight = 0.5f;
    public int dashDetectionRays = 3;
    public float dashDetectionDistance = 1.5f;

    [Header("Ground Check Settings")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.5f;
    public Transform groundCheckPoint;

    [Header("Visual Settings")]
    public Sprite detectionSprite;

    [Header("Collision Settings")]
    public LayerMask playerLayer;

    [Header("Knockback Settings")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.3f;

    [Header("Bounce Back Settings")]
    public float bounceBackForceX = 5f;
    public float bounceBackForceY = 3f;
    public float bounceRecoveryTime = 0.5f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D enemyCollider;
    private EnemyModule enemyModule;
    private HealthModule healthModule;
    private Animator animator;

    private bool isDashing = false;
    private bool isAttacking = false;
    private bool isStunned = false;
    private bool isSearchingForGround = false;
    private bool isUnderground = false;
    private bool isJumpingBack = false;
    private bool peakOut = false;
    private bool jumpOut = false;
    private bool isDead = false;
    private Coroutine currentDashCoroutine;
    private Coroutine currentJumpBackCoroutine;
    private Coroutine currentAttackCoroutine;
    private Coroutine currentSearchCoroutine;
    private GameObject cachedTarget;
    private Vector2 currentWalkDirection = Vector2.right;
    private float lastPatrolChangeTime;
    private float lastAttackTime;
    private Color originalColor;
    private Vector3 surfacePosition;
    private float originalGravityScale;
    private int lastFacingDir = 1;

    [Header("Health")]
    public float maxHealth = 100f;
    public GameObject Target => cachedTarget;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int BurrowHash = Animator.StringToHash("StartingBurrow");
    private static readonly int JumpOutHash = Animator.StringToHash("Jumpout");
    private static readonly int PeakOutHash = Animator.StringToHash("PeakOut");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [Header("Damage Settings")]
    public float damagePerHit = 10f;
    public float damageCooldown = 0.5f;

    private bool canDamage = true;
    private float lastDamageTime;
    private bool hasHitThisDash = false;
    private bool isCleaningUp = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();
        enemyModule = GetComponent<EnemyModule>();
        healthModule = GetComponent<HealthModule>();
        animator = GetComponent<Animator>();
        originalGravityScale = rb.gravityScale;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        if (groundCheckPoint == null)
        {
            GameObject check = new GameObject("GroundCheck");
            check.transform.SetParent(transform);
            check.transform.localPosition = Vector3.down * 0.5f;
            groundCheckPoint = check.transform;
        }

        InitializeModules();
        lastPatrolChangeTime = Time.time;
    }

    private void Update()
    {
        if (isDead || isCleaningUp) return;
        if (isJumpingBack) return;
        if (isAttacking || isDashing || isStunned || isSearchingForGround || isUnderground) return;

        if (!IsOnValidGround())
        {
            SafeStartCoroutine(SearchForGroundRoutine(), ref currentSearchCoroutine);
            return;
        }

        HandleMovement();

        float xVel = rb.linearVelocity.x;

        if (Mathf.Abs(xVel) > 0.05f)
            lastFacingDir = (xVel > 0) ? -1 : 1;

        transform.localScale = new Vector3(lastFacingDir, 1, 1);

        if (!isUnderground && spriteRenderer != null && spriteRenderer.color.a < 0.9f)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || isCleaningUp) return;
        UpdateAnimation();

        if (isDashing && !hasHitThisDash)
            DetectDashHits();
    }

    private void InitializeModules()
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

        if (healthModule != null)
        {
            healthModule.Initialize(maxHealth);
            healthModule.onHealthChanged += HandleHealthChanged;
            healthModule.onDeath += HandleDeath;
        }
    }

    public void KnockBack(KnockbackData data)
    {
        if (isStunned || isDead || isCleaningUp) return;
        if (isUnderground) ForceEmergenceFromKnockback();

        rb.AddForce(data.Direction * data.Force, ForceMode2D.Impulse);
        StartCoroutine(ApplyKnockbackStun());
    }

    void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        StopAllCoroutines();
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isCleaningUp = true;
        rb.linearVelocity = Vector2.zero;
        isAttacking = false;
        isDashing = false;
        isStunned = false;
        isSearchingForGround = false;
        isUnderground = false;
        isJumpingBack = false;

        if (isUnderground)
        {
            ForceEmergenceFromKnockback();
            yield return new WaitForSeconds(0.1f);
        }

        if (!IsOnValidGround() || Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            yield return new WaitUntil(() => Mathf.Abs(rb.linearVelocity.y) < 0.1f);
            yield return new WaitForSeconds(0.1f);
        }

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (enemyCollider != null) enemyCollider.enabled = false;
        if (enemyModule != null) enemyModule.enabled = false;

        if (animator != null)
        {
            animator.SetBool(DeathHash, true);
            animator.SetBool(BurrowHash, false);
            animator.SetBool(JumpOutHash, false);
            animator.SetBool(PeakOutHash, false);
            animator.SetFloat(SpeedHash, 0f);
        }

        yield return new WaitForSeconds(2f);
        isCleaningUp = false;
        Destroy(gameObject);
    }

    private void ForceEmergenceFromKnockback()
    {
        isUnderground = false;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }
        transform.localScale = Vector3.one;
        transform.position = new Vector3(transform.position.x, surfacePosition.y, transform.position.z);
        rb.gravityScale = originalGravityScale;
        if (enemyCollider != null) enemyCollider.enabled = true;
        if (healthModule != null) healthModule.invincible = false;
    }

    private IEnumerator ApplyKnockbackStun()
    {
        isStunned = true;
        ResetEnemyState();
        yield return new WaitForSeconds(knockbackDuration);
        if (!isDead && IsOnValidGround()) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        isStunned = false;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (isDead || isCleaningUp) return;
        if (isUnderground)
        {
            if (currentDashCoroutine != null)
            {
                StopCoroutine(currentDashCoroutine);
                currentDashCoroutine = null;
            }

            if (isDashing)
            {
                SafeStartCoroutine(JumpBackWithRecovery(), ref currentJumpBackCoroutine);
            }
            isDashing = false;
        }
    }

    private void ResetEnemyState()
    {
        if (isDead) return;

        isAttacking = false;
        isDashing = false;
        isSearchingForGround = false;
        isUnderground = false;
        isJumpingBack = false;
        hasHitThisDash = false;

        rb.gravityScale = originalGravityScale;
        if (enemyCollider != null) enemyCollider.enabled = true;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        transform.localScale = Vector3.one;
        if (healthModule != null) healthModule.invincible = false;

        if (currentJumpBackCoroutine != null)
        {
            StopCoroutine(currentJumpBackCoroutine);
            currentJumpBackCoroutine = null;
        }
    }

    private void HandleTargetDetected(GameObject target)
    {
        if (cachedTarget == null && !isDead)
            cachedTarget = target;
    }

    private void HandleStartAttack()
    {
        if (isDead || isCleaningUp || isAttacking || isStunned || isSearchingForGround || isUnderground || !IsOnValidGround())
            return;

        if (!IsTargetValid())
        {
            cachedTarget = null;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, cachedTarget.transform.position);
        if (distanceToPlayer <= attackRadius)
            SafeStartCoroutine(PerformAttack(), ref currentAttackCoroutine);
    }

    private void HandleMovement()
    {
        if (IsTargetValid() && !isStunned)
            ChasePlayer();
        else
            Patrol();
    }

    private void ChasePlayer()
    {
        if (!IsTargetValid()) return;

        Vector2 direction = (cachedTarget.transform.position - transform.position).normalized;
        Vector2 moveDirection = new Vector2(direction.x, 0f).normalized;
        rb.linearVelocity = new Vector2(moveDirection.x * chaseSpeed, rb.linearVelocity.y);
        transform.localScale = moveDirection.x > 0 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
    }

    private void Patrol()
    {
        if (Time.time - lastPatrolChangeTime > patrolChangeTime)
        {
            currentWalkDirection = currentWalkDirection == Vector2.right ? Vector2.left : Vector2.right;
            lastPatrolChangeTime = Time.time;
            transform.localScale = currentWalkDirection.x > 0 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
        }
        rb.linearVelocity = new Vector2(currentWalkDirection.x * walkSpeed, rb.linearVelocity.y);
    }

    private bool IsOnValidGround()
    {
        if (groundCheckPoint == null || isDead) return false;
        RaycastHit2D hit = Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private IEnumerator SearchForGroundRoutine()
    {
        if (isSearchingForGround || isDead) yield break;
        isSearchingForGround = true;
        rb.linearVelocity = Vector2.zero;

        float searchTime = 0f;
        float maxSearchTime = 3f;
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        randomDirection.y = 0;

        while (searchTime < maxSearchTime && !IsOnValidGround() && !isDead)
        {
            searchTime += Time.deltaTime;
            if (searchTime % 1f < Time.deltaTime)
            {
                randomDirection = Random.insideUnitCircle.normalized;
                randomDirection.y = 0;
            }
            rb.linearVelocity = new Vector2(randomDirection.x * walkSpeed, rb.linearVelocity.y);
            yield return null;
        }

        if (!isDead)
        {
            rb.linearVelocity = Vector2.zero;
            isSearchingForGround = false;
        }
        currentSearchCoroutine = null;
    }

    private void OnDestroy()
    {
        isCleaningUp = true;
        StopAllCoroutines();

        if (enemyModule != null)
        {
            enemyModule.OnStartAttack -= HandleStartAttack;
            enemyModule.OnTargetDetected -= HandleTargetDetected;
        }

        if (healthModule != null)
        {
            healthModule.onHealthChanged -= HandleHealthChanged;
            healthModule.onDeath -= HandleDeath;
        }
    }

    private void DetectDashHits()
    {
        if (!canDamage || !isDashing || hasHitThisDash || !IsTargetValid()) return;

        Vector2 dashDirection = GetDashDirection();
        Vector2 detectionOrigin = GetDashDetectionOrigin();

        RaycastHit2D hit = Physics2D.Raycast(detectionOrigin, dashDirection, dashDetectionDistance, playerLayer);

        if (hit.collider != null && !hasHitThisDash && hit.transform.gameObject.layer != LayerMask.NameToLayer("Ignore"))
        {
            HealthModule playerHealth = hit.collider.GetComponent<HealthModule>();
            if (playerHealth != null && hit.collider.gameObject == cachedTarget)
            {
                ProcessDashHit(playerHealth, hit.point);
            }
        }
    }

    private void ProcessDashHit(HealthModule playerHealth, Vector2 hitPoint)
    {
        playerHealth.TakeDamage(damagePerHit);
        hasHitThisDash = true;
        StartCoroutine(DamageCooldown());

        if (currentDashCoroutine != null)
        {
            StopCoroutine(currentDashCoroutine);
            currentDashCoroutine = null;
        }

        isDashing = false;
        SafeStartCoroutine(JumpBackWithRecovery(), ref currentJumpBackCoroutine);
    }

    private Vector2 GetDashDirection()
    {
        if (!IsTargetValid()) return Vector2.right;
        Vector2 direction = (cachedTarget.transform.position - transform.position).normalized;
        return new Vector2(direction.x, 0f).normalized;
    }

    private Vector2 GetDashDetectionOrigin()
    {
        Vector2 baseOrigin = (Vector2)transform.position;
        Vector2 dashDirection = GetDashDirection();
        return baseOrigin + dashDirection * (dashDetectionDistance * 0.3f);
    }

    private IEnumerator DamageCooldown()
    {
        canDamage = false;
        yield return new WaitForSeconds(damageCooldown);
        canDamage = true;
    }

    private IEnumerator JumpBackWithRecovery()
    {
        if (isDead || !IsTargetValid())
        {
            isJumpingBack = false;
            yield break;
        }

        isJumpingBack = true;
        rb.gravityScale = originalGravityScale;
        Vector2 bounceDirection = (transform.position - cachedTarget.transform.position).normalized;
        rb.linearVelocity = new Vector2(bounceDirection.x * bounceBackForceX, bounceBackForceY);

        float recoveryTimer = 0f;
        bool hasLanded = false;

        while (recoveryTimer < bounceRecoveryTime && !hasLanded && !isDead)
        {
            recoveryTimer += Time.deltaTime;
            if (IsOnValidGround() && Mathf.Abs(rb.linearVelocity.y) < 0.1f) hasLanded = true;
            yield return null;
        }

        if (!isDead)
        {
            if (!hasLanded) yield return new WaitUntil(() => IsOnValidGround() && Mathf.Abs(rb.linearVelocity.y) < 0.1f || isDead);
            if (!isDead)
            {
                yield return new WaitForSeconds(0.1f);
                rb.linearVelocity = Vector2.zero;
                isJumpingBack = false;
                isAttacking = false;
            }
        }

        currentJumpBackCoroutine = null;
    }

    private IEnumerator PerformAttack()
    {
        if (!IsTargetValid())
        {
            isAttacking = false;
            yield break;
        }

        isAttacking = true;
        lastAttackTime = Time.time;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);

        if (!IsTargetValid() || isStunned || isDead)
        {
            ResetEnemyState();
            yield break;
        }

        yield return StartCoroutine(BurrowUnderground());
        if (isStunned || isDead || !IsTargetValid())
        {
            ResetEnemyState();
            yield break;
        }

        yield return StartCoroutine(MoveUndergroundBehindPlayer(undergroundDuration));
        if (isStunned || isDead || !IsTargetValid())
        {
            ResetEnemyState();
            yield break;
        }

        yield return StartCoroutine(EmergeFromGround());
        if (isStunned || isDead || !IsTargetValid())
        {
            ResetEnemyState();
            yield break;
        }

        hasHitThisDash = false;
        SafeStartCoroutine(DashTowardTarget(), ref currentDashCoroutine);
        yield return currentDashCoroutine;
        currentDashCoroutine = null;

        if (!isDead && !hasHitThisDash) yield return new WaitForSeconds(0.5f);
        isAttacking = false;
        currentAttackCoroutine = null;
    }

    private IEnumerator BurrowUnderground()
    {
        if (isDead) yield break;

        isUnderground = true;
        surfacePosition = transform.position;
        if (enemyCollider != null) enemyCollider.enabled = false;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        Vector3 undergroundPos = transform.position - new Vector3(0, burrowDepth, 0);
        float elapsed = 0f;
        if (healthModule != null) healthModule.invincible = true;

        while (elapsed < burrowInDuration && !isStunned && !isDead)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / burrowInDuration;
            transform.position = Vector3.Lerp(transform.position, undergroundPos, progress);
            float scale = Mathf.Lerp(1f, 0.8f, progress);
            transform.localScale = new Vector3(scale * lastFacingDir, scale, 1f);
            yield return null;
        }

        if (!isDead && spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }

    private IEnumerator MoveUndergroundBehindPlayer(float movementDuration)
    {
        float startTime = Time.time;
        while (Time.time - startTime < movementDuration && IsTargetValid() && !isStunned && !isDead)
        {
            Vector3 dirToPlayer = (cachedTarget.transform.position - transform.position).normalized;
            Vector3 targetPos = cachedTarget.transform.position - dirToPlayer * behindOffset;
            targetPos.y = surfacePosition.y - burrowDepth;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * burrowSpeed);
            yield return null;
        }
    }

    private IEnumerator EmergeFromGround()
    {
        if (isDead) yield break;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        Vector3 undergroundPosition = transform.position;
        Vector3 peekPosition = new Vector3(transform.position.x, surfacePosition.y + 0.4f, transform.position.z);
        Vector3 targetSurfacePosition = new Vector3(transform.position.x, surfacePosition.y, transform.position.z);

        float emergeTime = 0.3f;
        float peekPause = 0.4f;
        float dipTime = 0.25f;
        float elapsed = 0f;

        Transform player = FindAnyObjectByType<PlayerMovement>()?.transform;

        peakOut = true;
        while (elapsed < emergeTime && !isStunned && !isDead)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / emergeTime;
            transform.position = Vector3.Lerp(undergroundPosition, peekPosition, progress);
            float scale = Mathf.Lerp(0.8f, 0.9f, progress);

            if (player != null) lastFacingDir = (player.position.x > transform.position.x) ? -1 : 1;

            transform.localScale = new Vector3(scale * lastFacingDir, scale, 1f);
            yield return null;
        }

        if (isDead) yield break;

        transform.position = peekPosition;
        yield return new WaitForSeconds(peekPause);
        peakOut = false;

        elapsed = 0f;
        Vector3 dipPosition = new Vector3(transform.position.x, surfacePosition.y - 0.8f, transform.position.z);
        while (elapsed < dipTime && !isStunned && !isDead)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dipTime;
            transform.position = Vector3.Lerp(peekPosition, dipPosition, progress);
            float scale = Mathf.Lerp(0.9f, 0.8f, progress);

            if (player != null) lastFacingDir = (player.position.x > transform.position.x) ? -1 : 1;

            transform.localScale = new Vector3(scale * lastFacingDir, scale, 1f);
            yield return null;
        }

        if (isDead) yield break;

        jumpOut = true;
        yield return new WaitForSeconds(1.03f);

        elapsed = 0f;
        while (elapsed < emergeTime && !isStunned && !isDead)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / emergeTime;
            transform.position = Vector3.Lerp(dipPosition, targetSurfacePosition, progress);
            float scale = Mathf.Lerp(0.8f, 1f, progress);

            if (player != null) lastFacingDir = (player.position.x > transform.position.x) ? -1 : 1;

            transform.localScale = new Vector3(scale * lastFacingDir, scale, 1f);
            yield return null;
        }

        if (!isDead)
        {
            transform.position = targetSurfacePosition;
            rb.gravityScale = originalGravityScale;
            if (enemyCollider != null) enemyCollider.enabled = true;
            if (healthModule != null) healthModule.invincible = false;
            isUnderground = false;
            rb.linearVelocity = new Vector2(0, emergeJumpForce);
            yield return new WaitForSeconds(0.5f);

            jumpOut = false;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null || isDead) return;
        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(BurrowHash, isUnderground);
        animator.SetBool(JumpOutHash, jumpOut);
        animator.SetBool(PeakOutHash, peakOut);
    }

    private IEnumerator DashTowardTarget()
    {
        if (!IsTargetValid() || isStunned || isDead)
        {
            isDashing = false;
            yield break;
        }

        isDashing = true;
        float currentGravity = rb.gravityScale;
        rb.gravityScale = 0;
        Vector2 dashDirection = (cachedTarget.transform.position - transform.position).normalized;
        float dashEndTime = Time.time + dashDuration;

        while (Time.time < dashEndTime && !isStunned && isDashing && !isDead && IsTargetValid())
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            yield return null;
        }

        if (!isDead)
        {
            rb.gravityScale = currentGravity;
            rb.linearVelocity = Vector2.zero;
            isDashing = false;
            hasHitThisDash = false;
        }
        currentDashCoroutine = null;
    }

    private void SafeStartCoroutine(IEnumerator coroutine, ref Coroutine storedCoroutine)
    {
        if (isDead || isCleaningUp) return;
        if (storedCoroutine != null)
            StopCoroutine(storedCoroutine);
        storedCoroutine = StartCoroutine(coroutine);
    }

    private bool IsTargetValid()
    {
        return cachedTarget != null && cachedTarget.activeInHierarchy;
    }

    private void OnDrawGizmosSelected()
    {
        if (isDashing && !isDead)
        {
            Gizmos.color = Color.magenta;
            Vector2 origin = GetDashDetectionOrigin();
            Vector2 direction = GetDashDirection();
            Gizmos.DrawRay(origin, direction * dashDetectionDistance);
        }
    }
}
