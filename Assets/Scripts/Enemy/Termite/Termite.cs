using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using System.Runtime.CompilerServices;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyModule))]
public class Termite : MonoBehaviour
{
    private EnemyModule enemyModule;
    private HealthModule healthModule;
    private Rigidbody2D rb;
    private Transform target;
    private bool isSticking = false;
    private bool isWaiting = false;
    private bool canStick = false;
    private bool isInAttackSequence = false;
    private bool isAvoidingPlayer = false;
    private bool isHypingUp = false;
    private bool isDead = false;

    private Vector3 lastContactPoint;
    private Transform lastContactTarget;
    private Vector3 localStickOffset;

    public Sprite detectionSprite;
    private BoxCollider2D boxCollider;
    private CircleCollider2D circleCollider;
    private Collider2D enemyCollider;

    public float jumpForce = 10f;
    public float stickDuration = 0.5f;
    public float jumpWindup = 0.2f;
    public float missWaitTime = 0.5f;
    public float jumpArcHeight = 2f;
    public float weightToAdd = 2f;

    public int maxHypeJumps = 3;
    public float hypeJumpForce = 3f;
    public float hypeJumpArcHeight = 1f;
    public float hypeJumpInterval = 0.3f;
    public float hypeWindup = 0.1f;

    public float avoidanceTime = 1f;
    public float avoidanceJumpForce = 6f;

    public float idleJumpForce = 5f;
    public float idleJumpIntervalMin = 0.5f;
    public float idleJumpIntervalMax = 1.5f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundLayerMask = 1;
    public LayerMask playerLayerMask;

    public float maxStickDistance = 1.5f;
    public float stickRaycastDistance = 1f;

    public float attackCooldown = 2f;
    private float lastAttackTime = 0f;

    private float currentSpeed = 0f;
    private Vector3 lastPosition;
    private float speedSmoothing = 0.1f;

    private bool isJumping = false;
    private float lastJumpTime = 0f;
    private float jumpCooldown = 0.1f;
    private float groundCheckCooldown = 0.2f;
    private float lastGroundedTime = 0f;

    private float playerDetectionRadius = 0.8f;

    private float lastDirection = 1f;
    private Vector2 lastMovementDirection = Vector2.right;
    private float directionChangeThreshold = 0.1f;

    private Coroutine idleRoutine;
    private Coroutine stickDetectionRoutine;
    private Coroutine attackSequenceRoutine;
    private Coroutine hypeJumpsRoutine;
    private Coroutine finalAttackRoutine;
    private Coroutine stickToPlayerRoutine;
    private Coroutine avoidanceBehaviorRoutine;
    private Coroutine flashColorRoutine;
    private Coroutine deathSequenceRoutine;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");
    private static readonly int StickHash = Animator.StringToHash("Stick");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private bool CanAttack => Time.time - lastAttackTime >= attackCooldown;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyCollider = GetComponent<Collider2D>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        lastPosition = transform.position;

        playerLayerMask = LayerMask.GetMask("Player");

        enemyModule = GetComponent<EnemyModule>();
        enemyModule.Initialize(
            cooldown: 1.5f,
            detectionRange: 5f,
            attackRange: 4f,
            layerMask: LayerMask.GetMask("Player"),
            detectionCol: new Color(0f, 1f, 0f, 0.2f),
            attackCol: new Color(1f, 0f, 0f, 0.3f),
            sprite: detectionSprite
        );

        enemyModule.OnTargetDetected += targetObj =>
        {
            target = targetObj.transform;
        };

        enemyModule.OnStartAttack += StartAttackingEnemy;

        healthModule = GetComponent<HealthModule>();
        healthModule.Initialize(1f);
        healthModule.onHealthChanged += HandleHealthChange;
        healthModule.onDeath += HandleDeath;

        idleRoutine = StartCoroutine(IdleRoutine());
        stickDetectionRoutine = StartCoroutine(StickDetectionRoutine());
    }

    void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();

        if (idleRoutine != null) StopCoroutine(idleRoutine);
        if (stickDetectionRoutine != null) StopCoroutine(stickDetectionRoutine);
        if (attackSequenceRoutine != null) StopCoroutine(attackSequenceRoutine);
        if (hypeJumpsRoutine != null) StopCoroutine(hypeJumpsRoutine);
        if (finalAttackRoutine != null) StopCoroutine(finalAttackRoutine);
        if (stickToPlayerRoutine != null) StopCoroutine(stickToPlayerRoutine);
        if (avoidanceBehaviorRoutine != null) StopCoroutine(avoidanceBehaviorRoutine);
        if (flashColorRoutine != null) StopCoroutine(flashColorRoutine);
        if (deathSequenceRoutine != null) StopCoroutine(deathSequenceRoutine);

        deathSequenceRoutine = StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        animator.SetBool(DeathHash, true);

        spriteRenderer.color = Color.white;
        isSticking = false;
        isWaiting = false;
        canStick = false;
        isInAttackSequence = false;
        isAvoidingPlayer = false;
        isHypingUp = false;
        isJumping = false;

        if (lastContactTarget != null)
        {
            transform.SetParent(null);

            Rigidbody2D playerRb = lastContactTarget.GetComponent<Rigidbody2D>();
            if (playerRb != null && EffectsModule.Instance != null)
            {
                EffectsModule.Instance.UndoSlow(new SlowedDownData(weightToAdd, playerRb));
            }
        }

        yield return new WaitForSeconds(2f);

        Destroy(gameObject);
    }

    void HandleHealthChange(float current, float max)
    {
        if (flashColorRoutine != null)
            StopCoroutine(flashColorRoutine);
        flashColorRoutine = StartCoroutine(FlashColor(Color.red));
    }

    IEnumerator FlashColor(Color color)
    {
        if (spriteRenderer != null && spriteRenderer.color.a > 0.5f)
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }

    private void Update()
    {
        if (isDead) return;

        UpdateSpeedTracking();
        UpdateJumpTracking();
        UpdateFlipping();
        UpdateAnimation();
        CheckAttackRange();
    }

    private void CheckAttackRange()
    {
        if (CanStartAttack() && target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget <= enemyModule.attackRadius)
            {
                attackSequenceRoutine = StartCoroutine(AttackSequence());
            }
        }
    }

    private bool CanStartAttack()
    {
        return !isDead && target != null &&
               !isSticking &&
               !isWaiting &&
               !isInAttackSequence &&
               !isAvoidingPlayer &&
               !isHypingUp &&
               CanAttack;
    }

    private void UpdateSpeedTracking()
    {
        Vector3 positionDelta = transform.position - lastPosition;
        float instantaneousSpeed = positionDelta.magnitude / Time.deltaTime;
        currentSpeed = Mathf.Lerp(currentSpeed, instantaneousSpeed, speedSmoothing);
        lastPosition = transform.position;
    }

    private void UpdateJumpTracking()
    {
        bool wasGrounded = !isJumping;
        bool isGrounded = CheckGrounded();

        if (isJumping && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            isJumping = false;
            lastGroundedTime = Time.time;
        }

        if (!isGrounded && rb.linearVelocity.y > 0.1f && Time.time - lastGroundedTime > groundCheckCooldown)
        {
            isJumping = true;
        }

        if (!isGrounded && rb.linearVelocity.y > 2f)
        {
            isJumping = true;
        }
    }

    private void UpdateFlipping()
    {
        if (isSticking) return;

        Vector2 currentDirection = GetCurrentMovementDirection();

        if (currentDirection.magnitude > directionChangeThreshold)
        {
            lastMovementDirection = currentDirection;

            if (Mathf.Abs(currentDirection.x) > 0.1f)
            {
                lastDirection = Mathf.Sign(currentDirection.x);
            }

            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * -lastDirection;
            transform.localScale = scale;
        }
    }

    private Vector2 GetCurrentMovementDirection()
    {
        if (rb.linearVelocity.magnitude > 0.5f)
        {
            return rb.linearVelocity.normalized;
        }

        Vector3 positionDelta = transform.position - lastPosition;
        if (positionDelta.magnitude > 0.01f)
        {
            return positionDelta.normalized;
        }

        if (target != null)
        {
            Vector2 toTarget = (target.position - transform.position).normalized;
            return toTarget;
        }

        return lastMovementDirection;
    }

    private bool CheckGrounded()
    {
        Vector2[] raycastOrigins = new Vector2[]
        {
            boxCollider.bounds.center - new Vector3(boxCollider.bounds.extents.x * 0.8f, boxCollider.bounds.extents.y, 0),
            boxCollider.bounds.center - new Vector3(0, boxCollider.bounds.extents.y, 0),
            boxCollider.bounds.center - new Vector3(-boxCollider.bounds.extents.x * 0.8f, boxCollider.bounds.extents.y, 0)
        };

        foreach (Vector2 origin in raycastOrigins)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                origin,
                Vector2.down,
                groundCheckDistance,
                groundLayerMask
            );

            Debug.DrawRay(origin, Vector2.down * groundCheckDistance, hit.collider != null ? Color.green : Color.red);

            if (hit.collider != null)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetBool(JumpingHash, isJumping);
        animator.SetFloat(SpeedHash, currentSpeed);
        animator.SetBool(StickHash, isSticking);
    }

    private void StartAttackingEnemy()
    {
        if (!CanStartAttack()) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= enemyModule.attackRadius)
        {
            attackSequenceRoutine = StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
    {
        if (isDead) yield break;

        isInAttackSequence = true;
        lastAttackTime = Time.time;

        int numHypeJumps = Random.Range(0, maxHypeJumps + 1);

        if (numHypeJumps > 0)
        {
            hypeJumpsRoutine = StartCoroutine(PerformHypeJumps(numHypeJumps));
            yield return hypeJumpsRoutine;
        }

        if (target != null && !isDead)
        {
            finalAttackRoutine = StartCoroutine(FinalAttackRoutine());
            yield return finalAttackRoutine;
        }

        isInAttackSequence = false;
    }

    private IEnumerator PerformHypeJumps(int numJumps)
    {
        if (isDead) yield break;

        isHypingUp = true;

        for (int i = 0; i < numJumps; i++)
        {
            if (target == null || isDead) break;

            yield return new WaitForSeconds(hypeWindup);

            if (!isDead)
            {
                Vector2 force = Vector2.up * hypeJumpForce;
                Jump(force);
            }

            if (i < numJumps - 1 && !isDead)
            {
                yield return new WaitForSeconds(hypeJumpInterval);
            }
        }

        if (!isDead)
        {
            yield return new WaitForSeconds(0.2f);
            isHypingUp = false;
        }
    }

    private IEnumerator FinalAttackRoutine()
    {
        if (isDead) yield break;

        isWaiting = true;
        yield return new WaitForSeconds(jumpWindup);

        if (target == null || isDead)
        {
            isWaiting = false;
            yield break;
        }

        FaceTarget();

        Vector2 dir = (target.position - transform.position).normalized;
        dir.y = Mathf.Clamp(dir.y, 0.2f, 0.8f);
        dir.Normalize();

        Vector2 force = dir * jumpForce;
        Jump(force);

        canStick = true;
        yield return new WaitForSeconds(0.8f);
        canStick = false;

        yield return new WaitForSeconds(missWaitTime);

        isWaiting = false;
    }

    private void FaceTarget()
    {
        if (target != null)
        {
            float directionToTarget = Mathf.Sign(target.position.x - transform.position.x);
            if (Mathf.Abs(directionToTarget) > 0.1f)
            {
                lastDirection = directionToTarget;
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * lastDirection;
                transform.localScale = scale;
            }
        }
    }

    private IEnumerator StickDetectionRoutine()
    {
        while (!isDead)
        {
            if (canStick && !isSticking && target != null)
            {
                CheckForPlayerStick();
            }
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void CheckForPlayerStick()
    {
        if (target == null || isDead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);
        if (distanceToPlayer > maxStickDistance)
        {
            return;
        }

        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(0.7f, 0.7f),
            new Vector2(-0.7f, 0.7f),
            new Vector2(0.7f, -0.7f),
            new Vector2(-0.7f, -0.7f)
        };

        foreach (Vector2 direction in directions)
        {
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                stickRaycastDistance,
                playerLayerMask
            );

            Debug.DrawRay(transform.position, direction * stickRaycastDistance,
                         hit.collider != null ? Color.yellow : Color.blue);

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                float hitDistance = Vector3.Distance(transform.position, hit.point);
                if (hitDistance <= maxStickDistance)
                {
                    stickToPlayerRoutine = StartCoroutine(StickToPlayerAtPoint(hit.collider.transform, hit.point));
                    return;
                }
            }
        }

        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            playerDetectionRadius,
            playerLayerMask
        );

        if (playerCollider != null && playerCollider.CompareTag("Player"))
        {
            Vector3 closestPoint = playerCollider.ClosestPoint(transform.position);
            float closestDistance = Vector3.Distance(transform.position, closestPoint);

            if (closestDistance <= maxStickDistance)
            {
                stickToPlayerRoutine = StartCoroutine(StickToPlayerAtPoint(playerCollider.transform, closestPoint));
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canStick || isSticking || isDead) return;

        if (collision.collider.CompareTag("Player"))
        {
            ContactPoint2D contact = collision.GetContact(0);
            float contactDistance = Vector3.Distance(transform.position, contact.point);

            if (contactDistance <= maxStickDistance)
            {
                stickToPlayerRoutine = StartCoroutine(StickToPlayerAtPoint(collision.transform, contact.point));
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canStick || isSticking || isDead) return;

        if (other.CompareTag("Player"))
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            float closestDistance = Vector3.Distance(transform.position, closestPoint);

            if (closestDistance <= maxStickDistance)
            {
                stickToPlayerRoutine = StartCoroutine(StickToPlayerAtPoint(other.transform, closestPoint));
            }
        }
    }

    private Vector2 GetAvoidanceDirection()
    {
        if (target == null)
        {
            return GetSafeDirection(new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 0.8f)).normalized);
        }

        Vector2 awayFromPlayer = (transform.position - target.position).normalized;
        awayFromPlayer += new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.1f, 0.5f));
        awayFromPlayer.Normalize();

        return GetSafeDirection(awayFromPlayer);
    }

    private Vector2 GetSafeDirection(Vector2 desiredDirection)
    {
        Vector2 checkPosition = (Vector2)transform.position + desiredDirection * 2f;

        RaycastHit2D groundCheck = Physics2D.Raycast(
            transform.position,
            desiredDirection,
            2f,
            groundLayerMask
        );

        RaycastHit2D groundBelow = Physics2D.Raycast(
            checkPosition + Vector2.up * 0.5f,
            Vector2.down,
            groundCheckDistance,
            groundLayerMask
        );

        if (groundCheck.collider == null || groundBelow.collider == null)
        {
            Vector2[] alternativeDirections = {
                new Vector2(-desiredDirection.x, desiredDirection.y),
                new Vector2(desiredDirection.x * 0.5f, desiredDirection.y),
                new Vector2(0f, 0.5f),
                new Vector2(Random.Range(-0.3f, 0.3f), 0.7f)
            };

            foreach (Vector2 altDir in alternativeDirections)
            {
                Vector2 altCheckPos = (Vector2)transform.position + altDir.normalized * 2f;
                RaycastHit2D altGroundCheck = Physics2D.Raycast(
                    transform.position,
                    altDir.normalized,
                    2f,
                    groundLayerMask
                );
                RaycastHit2D altGroundBelow = Physics2D.Raycast(
                    altCheckPos + Vector2.up * 0.5f,
                    Vector2.down,
                    groundCheckDistance,
                    groundLayerMask
                );

                if (altGroundCheck.collider != null && altGroundBelow.collider != null)
                {
                    return altDir.normalized;
                }
            }

            return Vector2.up;
        }

        return desiredDirection;
    }

    private IEnumerator AvoidanceBehavior()
    {
        if (isDead) yield break;

        isAvoidingPlayer = true;
        int numAvoidanceJumps = Random.Range(1, 3);

        for (int i = 0; i < numAvoidanceJumps; i++)
        {
            if (target == null || isDead) break;

            yield return new WaitForSeconds(0.3f);

            if (!isDead)
            {
                Vector2 avoidanceDir = GetAvoidanceDirection();
                Vector2 force = avoidanceDir * avoidanceJumpForce + Vector2.up * (jumpArcHeight * 0.7f);
                Jump(force);
            }

            yield return new WaitForSeconds(0.4f);
        }

        if (!isDead)
        {
            yield return new WaitForSeconds(avoidanceTime);
            isAvoidingPlayer = false;
        }
    }

    private void Jump(Vector2 force)
    {
        if (isDead) return;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);

        if (Mathf.Abs(force.x) > 0.1f)
        {
            lastDirection = Mathf.Sign(force.x);
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * lastDirection;
            transform.localScale = scale;
        }

        isJumping = true;
        lastJumpTime = Time.time;
    }

    private IEnumerator StickToPlayerAtPoint(Transform player, Vector3 contactPoint)
    {
        if (!canStick || isSticking || isDead) yield break;

        lastContactTarget = player;
        lastContactPoint = contactPoint;
        localStickOffset = lastContactTarget.InverseTransformPoint(lastContactPoint);

        isSticking = true;
        canStick = false;
        isJumping = false;
        healthModule.invincible = true;

        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            if (EffectsModule.Instance != null)
            {
                EffectsModule.Instance.SlowedDown(new SlowedDownData(weightToAdd, playerRb));
            }
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (boxCollider != null)
            boxCollider.isTrigger = true;

        transform.SetParent(lastContactTarget);

        float stickTimer = 0f;
        while (stickTimer < stickDuration && lastContactTarget != null && !isDead)
        {
            Vector3 worldStickPosition = lastContactTarget.TransformPoint(localStickOffset);
            transform.position = new Vector3(worldStickPosition.x, worldStickPosition.y, -5);
            stickTimer += Time.deltaTime;
            yield return null;
        }

        if (lastContactTarget != null)
            transform.SetParent(null);

        rb.bodyType = RigidbodyType2D.Dynamic;

        if (boxCollider != null)
            boxCollider.isTrigger = false;

        if (playerRb != null && EffectsModule.Instance != null && !isDead)
        {
            EffectsModule.Instance.UndoSlow(new SlowedDownData(weightToAdd, playerRb));
        }

        if (!isDead)
        {
            Vector2 jumpOffDir = GetAvoidanceDirection();
            rb.AddForce(jumpOffDir * (jumpForce * 0.6f), ForceMode2D.Impulse);

            isSticking = false;
            localStickOffset = Vector3.zero;

            avoidanceBehaviorRoutine = StartCoroutine(AvoidanceBehavior());

            isWaiting = true;
            yield return new WaitForSeconds(0.3f);
            isWaiting = false;
        }
    }

    private IEnumerator IdleRoutine()
    {
        while (!isDead)
        {
            if (!isWaiting && !isSticking && target == null && !isAvoidingPlayer && !isHypingUp)
            {
                Vector2 desiredDir = new Vector2(Random.Range(-1f, 1f), 0f).normalized;
                Vector2 safeDir = GetSafeDirection(desiredDir);

                Jump(safeDir * idleJumpForce + Vector2.up * (jumpArcHeight / 2f));
            }

            yield return new WaitForSeconds(Random.Range(idleJumpIntervalMin, idleJumpIntervalMax));
        }
    }

    public float CurrentSpeed => currentSpeed;
    public float GetCurrentSpeed() => currentSpeed;
    public void ResetSpeedTracking()
    {
        currentSpeed = 0f;
        lastPosition = transform.position;
    }

    public bool IsJumping => isJumping;
    public bool GetIsJumping() => isJumping;
    public void ForceJumpState(bool jumping) => isJumping = jumping;

    public float LastDirection => lastDirection;
    public Vector2 LastMovementDirection => lastMovementDirection;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxStickDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);
    }
}