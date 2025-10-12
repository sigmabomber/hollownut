using UnityEngine;
using System.Collections;

public class Mantis : MonoBehaviour
{
    private enum MantisState
    {
        Idle,
        Roaming,
        Chasing,
        Blocking,
        Attacking,
        Cooldown
    }

    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int IsAttackingHash = Animator.StringToHash("StartAttack");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int SlashTriggerHash = Animator.StringToHash("Slash");
    private static readonly int CounterAttackHash = Animator.StringToHash("CounterAttack");
    private static readonly int JumpbackHash = Animator.StringToHash("Jumpback");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [Header("Combat Settings")]
    public float blockDuration = 2f;
    public float timeBetweenSlashes = 0.2f;
    public float chaseSpeed = 3f;
    public float postAttackCooldown = 1f;
    public float blockRange = 2f;
    public float attackRange = 1.5f;
    public float decisionRange = 2f;

    [Header("Detection Settings")]
    public float detectionRange = 6f;
    public LayerMask playerLayermask;

    [Header("Attack Settings")]
    public float slashDamage = 30f;
    public float counterAttackDamage = 50f;

    [Header("Jumpback Settings")]
    public float jumpbackDistance = 1.5f;
    public float jumpbackDuration = 0.3f;

    [Header("Roaming Settings")]
    public float roamSpeed = 1.5f;
    public float roamWaitTime = 2f;
    public float roamDistance = 3f;

    [Header("Attack Detection")]
    [SerializeField] private float attackWidth = 1.2f;
    [SerializeField] private float attackHeight = 0.8f;
    [SerializeField] private int numberOfRays = 5;

    [Header("VFX")]
    [SerializeField] private GameObject attackHitPlayerVFX;
    [SerializeField] private LayerMask obstacleLayers;

    private MantisState currentState = MantisState.Idle;
    private bool isBlocking = false;
    private float blockTimer = 0f;
    private float cooldownTimer = 0f;
    private Transform player;
    private bool playerAttackedDuringBlock = false;
    private int slashesRemaining = 0;
    private bool isJumpingBack = false;

    private Vector2 roamTarget;
    private float roamTimer = 0f;
    private bool isWaitingToRoam = false;

    private EnemyModule enemyModule;
    private HealthModule healthModule;
    private Animator animator;
    private Animator slashAnimator;
    public GameObject slashObj;
    private StickAttack cachedPlayerAttack;
    private SpriteRenderer spriteRenderer;

    private bool isDead = false;
    private Coroutine currentAttackCoroutine;

    private bool hasHitThisAttack = false;

    void Start()
    {
        enemyModule = GetComponent<EnemyModule>();
        healthModule = GetComponent<HealthModule>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (slashObj != null)
        {
            slashAnimator = slashObj.GetComponent<Animator>();
            slashObj.SetActive(false);
        }

        PickNewRoamTarget();

        if (enemyModule != null)
        {
            enemyModule.Initialize(
                cooldown: enemyModule.attackCooldown,
                detectionRange: detectionRange,
                attackRange: attackRange,
                layerMask: playerLayermask);

            enemyModule.OnTargetDetected += OnPlayerDetected;
        }

        if (healthModule != null)
        {
            healthModule.Initialize(60);
            healthModule.onHealthChanged += OnDamageTaken;
            healthModule.onDeath += HandleDeath;
            healthModule.onInvincDamage += OnInvincDamage;
        }
    }

    private void OnInvincDamage()
    {
        if (currentState == MantisState.Blocking && !playerAttackedDuringBlock)
        {
            playerAttackedDuringBlock = true;
            ExecuteCounterAttack();
        }
    }

    void HandleDeath()
    {
        if (isDead) return;
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        isDead = true;

        animator.SetBool(DeathHash, true);
        spriteRenderer.color = Color.white;

        isJumpingBack = false;
        isBlocking = false;
        isWaitingToRoam = false;

        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }

        if (slashObj != null)
        {
            slashObj.SetActive(false);
        }

        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    void Update()
    {
        if (isDead) return;

        if (enemyModule?.target == null)
        {
            if (currentState != MantisState.Roaming && currentState != MantisState.Idle)
            {
                TransitionToState(MantisState.Roaming);
            }
            else if (currentState == MantisState.Idle)
            {
                TransitionToState(MantisState.Roaming);
            }
        }
        else
        {
            CachePlayerReferences();
        }

        switch (currentState)
        {
            case MantisState.Idle:
                UpdateIdleState();
                break;
            case MantisState.Roaming:
                UpdateRoamingState();
                break;
            case MantisState.Chasing:
                UpdateChasingState();
                break;
            case MantisState.Blocking:
                UpdateBlockingState();
                break;
            case MantisState.Attacking:
                UpdateAttackingState();
                break;
            case MantisState.Cooldown:
                UpdateCooldownState();
                break;
        }

        UpdateAnimator();
    }

    private void CachePlayerReferences()
    {
        if (player == null && enemyModule.target != null)
        {
            player = enemyModule.target.transform;
            cachedPlayerAttack = player.GetComponent<StickAttack>();
        }
    }

    private void TransitionToState(MantisState newState)
    {
        switch (currentState)
        {
            case MantisState.Blocking:
                isBlocking = false;
                healthModule.invincible = false;
                StopAllCoroutines();
                StartCoroutine(FlashColor(Color.white));
                break;
            case MantisState.Attacking:
                slashesRemaining = 0;
                if (currentAttackCoroutine != null)
                {
                    StopCoroutine(currentAttackCoroutine);
                    currentAttackCoroutine = null;
                }
                if (slashObj != null)
                {
                    slashObj.SetActive(false);
                }
                break;
        }

        currentState = newState;
    }

    private void UpdateIdleState()
    {
        TransitionToState(MantisState.Roaming);
    }

    private void UpdateRoamingState()
    {
        if (enemyModule?.target != null)
        {
            TransitionToState(MantisState.Chasing);
            return;
        }

        if (isWaitingToRoam)
        {
            roamTimer -= Time.deltaTime;
            if (roamTimer <= 0)
            {
                isWaitingToRoam = false;
                PickNewRoamTarget();
            }
            return;
        }

        Vector2 direction = (roamTarget - (Vector2)transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, roamTarget);

        if (distanceToTarget > 0.1f)
        {
            transform.position += (Vector3)direction * roamSpeed * Time.deltaTime;

            if (direction.x != 0)
            {
                transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
            }
        }
        else
        {
            isWaitingToRoam = true;
            roamTimer = roamWaitTime;
        }
    }

    private void UpdateChasingState()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= decisionRange)
        {
            MakeCombatDecision();
            return;
        }

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)direction * chaseSpeed * Time.deltaTime;

        if (direction.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
        }
    }

    private void MakeCombatDecision()
    {
        if (Random.Range(0, 2) == 0)
        {
            StartBlocking();
        }
        else
        {
            StartAttackSequence();
        }
    }

    private void UpdateBlockingState()
    {
        blockTimer -= Time.deltaTime;

        if (blockTimer <= 0)
        {
            CheckDistanceAfterBlock();
        }
    }

    private void UpdateAttackingState()
    {
        if (slashesRemaining <= 0 && currentAttackCoroutine == null)
        {
            TransitionToState(MantisState.Cooldown);
            cooldownTimer = postAttackCooldown;
        }
    }

    private void UpdateCooldownState()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0)
        {
            CheckDistanceAfterAttack();
        }
    }

    private void CheckDistanceAfterBlock()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= decisionRange)
        {
            StartAttackSequence();
        }
        else
        {
            TransitionToState(MantisState.Chasing);
        }
    }

    private void CheckDistanceAfterAttack()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= decisionRange)
        {
            MakeCombatDecision();
        }
        else
        {
            TransitionToState(MantisState.Chasing);
        }
    }

    private void OnPlayerDetected(GameObject detectedPlayer)
    {
        if (currentState == MantisState.Idle || currentState == MantisState.Roaming)
        {
            TransitionToState(MantisState.Chasing);
            player = detectedPlayer.transform;
            cachedPlayerAttack = player.GetComponent<StickAttack>();
        }
    }

    private void StartAttackSequence()
    {
        if (currentState == MantisState.Attacking || currentState == MantisState.Blocking)
            return;

        TransitionToState(MantisState.Attacking);
        slashesRemaining = 2;
        currentAttackCoroutine = StartCoroutine(ExecuteDoubleSlash());
    }

    private IEnumerator ExecuteDoubleSlash()
    {
        for (int i = 0; i < 2; i++)
        {
            if (currentState != MantisState.Attacking) yield break;

            if (slashObj != null)
            {
                slashObj.SetActive(true);

                if (slashAnimator != null)
                {
                    slashAnimator.SetBool(IsAttackingHash, true);
                }
            }

            if (animator != null)
            {
                animator.SetTrigger(SlashTriggerHash);
            }

            yield return new WaitForSeconds(0.1f);

            hasHitThisAttack = false;
            DetectHits(slashDamage);

            yield return new WaitForSeconds(0.3f);

            if (i < 1 && slashObj != null)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
                slashObj.SetActive(false);
            }

            if (i < 1)
            {
                yield return new WaitForSeconds(timeBetweenSlashes - 0.3f);
            }

            slashesRemaining--;
        }

        if (slashObj != null)
        {
            slashObj.SetActive(false);
        }

        currentAttackCoroutine = null;
    }

    private void StartBlocking()
    {
        TransitionToState(MantisState.Blocking);
        isBlocking = true;
        blockTimer = blockDuration;
        healthModule.invincible = true;
        playerAttackedDuringBlock = false;
    }

    private void ExecuteCounterAttack()
    {
        if (currentState != MantisState.Blocking) return;

        TransitionToState(MantisState.Attacking);
        slashesRemaining = 1;
        currentAttackCoroutine = StartCoroutine(ExecuteCounterSlash());
    }

    private IEnumerator ExecuteCounterSlash()
    {
        if (currentState != MantisState.Attacking) yield break;

        yield return new WaitForSeconds(0.3f);
        if (slashObj != null)
        {
            slashObj.SetActive(true);

            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, true);
            }
        }

        if (animator != null)
        {
            animator.SetTrigger(CounterAttackHash);
        }

        yield return new WaitForSeconds(0.15f);

        hasHitThisAttack = false;
        DetectHits(counterAttackDamage);

        yield return new WaitForSeconds(0.4f);

        if (slashObj != null)
        {
            slashObj.SetActive(false);
            slashAnimator.SetBool(IsAttackingHash, false);
        }

        slashesRemaining = 0;
        currentAttackCoroutine = null;
    }

    void DetectHits(float damage)
    {
        Vector2 origin = GetAttackOrigin();
        Vector2 size = GetAttackSize();
        Vector2 direction = GetAttackDirectionVector();

        RaycastHit2D[] hits = PerformMultiRaycast(origin, size, direction);
        ProcessHits(hits, damage);
    }

    RaycastHit2D[] PerformMultiRaycast(Vector2 origin, Vector2 size, Vector2 direction)
    {
        System.Collections.Generic.List<RaycastHit2D> allHits = new System.Collections.Generic.List<RaycastHit2D>();

        float spacing;
        Vector2 perpendicular;

        if (direction == Vector2.left || direction == Vector2.right)
        {
            spacing = size.y / (numberOfRays - 1);
            perpendicular = Vector2.up;
        }
        else
        {
            spacing = size.x / (numberOfRays - 1);
            perpendicular = Vector2.right;
        }

        for (int i = 0; i < numberOfRays; i++)
        {
            Vector2 rayOrigin = origin + perpendicular * (i * spacing - size.y / 2);
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, 1.5f, playerLayermask | obstacleLayers);
            allHits.AddRange(hits);

            Debug.DrawRay(rayOrigin, direction * attackRange, Color.red, 1f);
        }

        return allHits.ToArray();
    }

    void ProcessHits(RaycastHit2D[] hits, float damage)
    {
        if (hasHitThisAttack) return;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hasHitThisAttack)
            {
                int enemyLayer = LayerMask.NameToLayer("Enemy");
                int ignoreLayer = LayerMask.NameToLayer("Ignore");
                if (hit.collider.gameObject.layer == enemyLayer) continue;
                if (hit.collider.gameObject.layer == ignoreLayer) continue;

                bool isPlayer = ((1 << hit.collider.gameObject.layer) & playerLayermask) != 0;
                ProcessHit(hit.collider, hit.point, isPlayer, damage);

                if (hasHitThisAttack) break;
            }
        }
    }

    Vector2 GetAttackOrigin()
    {
        Vector2 baseOrigin = (Vector2)transform.position;
        Vector2 direction = GetAttackDirectionVector();
        return baseOrigin + direction * 0.1f;

    }

    Vector2 GetAttackSize()
    {
        Vector2 direction = GetAttackDirectionVector();

        if (direction == Vector2.left || direction == Vector2.right)
        {
            return new Vector2(0.1f, attackHeight);
        }
        else
        {
            return new Vector2(attackWidth, 0.1f);
        }
    }

    Vector2 GetAttackDirectionVector()
    {
        if (player == null)
        {
            return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        }

        Vector2 toPlayer = (player.position - transform.position).normalized;

        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
        {
            return toPlayer.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return toPlayer.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    void ProcessHit(Collider2D other, Vector2 hitPoint, bool isPlayer, float damage)
    {
        HealthModule playerHealth = null;
        if (isPlayer)
        {
            playerHealth = other.GetComponent<HealthModule>();
        }

        if (isPlayer && (playerHealth == null))
            return;

        hasHitThisAttack = true;

        PlayHitVFX(hitPoint, isPlayer);

        if (isPlayer && playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
    }

    void PlayHitVFX(Vector2 position, bool isPlayer)
    {
        if (attackHitPlayerVFX != null && isPlayer)
        {
            Quaternion rotation = GetVFXRotation();
            Instantiate(attackHitPlayerVFX, position, rotation);
        }
    }

    Quaternion GetVFXRotation()
    {
        float randomAngle = Random.Range(-15f, 15f);
        Vector2 direction = GetAttackDirectionVector();

        if (direction == Vector2.left) return Quaternion.Euler(0, 0, 180f + randomAngle);
        if (direction == Vector2.right) return Quaternion.Euler(0, 0, 0f + randomAngle);
        if (direction == Vector2.up) return Quaternion.Euler(0, 0, 90f + randomAngle);
        if (direction == Vector2.down) return Quaternion.Euler(0, 0, 270f + randomAngle);

        return Quaternion.Euler(0, 0, randomAngle);
    }

    private void PickNewRoamTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        roamTarget = (Vector2)transform.position + randomDirection * Random.Range(roamDistance * 0.5f, roamDistance);
    }

    private IEnumerator JumpBack()
    {
        isJumpingBack = true;

        if (animator != null)
        {
            animator.SetBool(JumpbackHash, true);
        }

        Vector2 jumpDirection = Vector2.zero;
        if (player != null)
        {
            float horizontalDir = transform.position.x - player.position.x;
            jumpDirection = new Vector2(Mathf.Sign(horizontalDir), 0).normalized;

            if (Mathf.Abs(horizontalDir) < 0.01f)
            {
                jumpDirection = new Vector2(transform.localScale.x > 0 ? -1 : 1, 0);
            }
        }
        else
        {
            jumpDirection = new Vector2(transform.localScale.x > 0 ? -1 : 1, 0);
        }

        Vector2 startPos = transform.position;
        Vector2 endPos = new Vector2(startPos.x + jumpDirection.x * jumpbackDistance, startPos.y);
        float elapsed = 0f;

        while (elapsed < jumpbackDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpbackDuration;

            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 0.5f;

            float currentX = Mathf.Lerp(startPos.x, endPos.x, easeT);
            transform.position = new Vector3(currentX, startPos.y + arcHeight, transform.position.z);

            yield return null;
        }

        transform.position = new Vector3(endPos.x, startPos.y, transform.position.z);

        animator.SetBool(JumpbackHash, false);
        isJumpingBack = false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(IsBlockingHash, isBlocking);
        animator.SetBool(IsAttackingHash, currentState == MantisState.Attacking);

        float moveSpeed = 0f;
        if (currentState == MantisState.Chasing)
            moveSpeed = chaseSpeed;
        else if (currentState == MantisState.Roaming && !isWaitingToRoam)
            moveSpeed = roamSpeed;

        animator.SetFloat(MoveSpeedHash, moveSpeed);
    }

    public void OnDamageTaken(float current, float max)
    {
        StartCoroutine(FlashColor(Color.red));
        if (!isJumpingBack && current > 0)
        {
            StartCoroutine(JumpBack());
        }
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

    public void OnSlashDamageFrame()
    {
        if (currentState == MantisState.Attacking)
        {
            hasHitThisAttack = false;
            DetectHits(slashDamage);
        }
    }

    public void OnCounterDamageFrame()
    {
        if (currentState == MantisState.Attacking)
        {
            hasHitThisAttack = false;
            DetectHits(counterAttackDamage);
        }
    }

    public void ShowSlash()
    {
        if (slashObj != null)
        {
            slashObj.SetActive(true);
        }
    }

    public void HideSlash()
    {
        if (slashObj != null && slashesRemaining <= 0)
        {
            slashObj.SetActive(false);
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        if (enemyModule != null)
        {
            enemyModule.OnTargetDetected -= OnPlayerDetected;
        }

        if (healthModule != null)
        {
            healthModule.onHealthChanged -= OnDamageTaken;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, decisionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}