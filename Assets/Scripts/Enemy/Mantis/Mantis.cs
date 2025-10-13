using UnityEngine;
using System.Collections;

public class Mantis : MonoBehaviour
{
    private enum MantisState
    {
        Idle,
        Roaming,
        Chasing,
        Attacking,
        Retreating,
        Parrying,
        DefensiveStance
    }

    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
    private static readonly int IsAttackingHash = Animator.StringToHash("StartAttack");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int SlashTriggerHash = Animator.StringToHash("Slash");
    private static readonly int CounterAttackHash = Animator.StringToHash("CounterAttack");
    private static readonly int JumpbackHash = Animator.StringToHash("Jumpback");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [Header("Combat Settings")]
    public float chaseSpeed = 4.5f;
    public float attackRange = 1.8f;
    public float optimalDistance = 3f;
    public float timeBetweenAttacks = 0.8f;

    [Header("Jump Back Settings")]
    [SerializeField] private float jumpBackForce = 8f;

    [Header("Defensive Mode Settings")]
    [SerializeField] private float defensiveModeThreshold = 0.4f;
    [SerializeField] private float defensiveJumpBackForce = 10f;
    [SerializeField] private float defensiveParryChance = 0.7f;
    [SerializeField] private float defensiveStanceDuration = 2f;
    [SerializeField] private float defensiveCooldown = 1.5f;
    [SerializeField] private int defensiveMaxSlashCount = 2;

    [Header("Detection Settings")]
    public float detectionRange = 8f;
    public LayerMask playerLayermask;

    [Header("Attack Settings")]
    public float slashDamage = 30f;
    public float dashAttackSpeed = 8f;
    public float dashAttackDuration = 0.3f;
    public int minSlashCount = 1;
    public int maxSlashCount = 3;
    public float timeBetweenSlashes = 0.15f;

    [Header("Parry Settings")]
    public float parryDuration = 0.35f;
    public float parryChance = 0.4f;
    public float parryCounterDamage = 50f;
    public float parryDetectionRange = 2.5f;

    [Header("Roaming Settings")]
    public float roamSpeed = 2f;
    public float roamWaitTime = 1.5f;
    public float roamDistance = 4f;

    [Header("Attack Detection")]
    [SerializeField] private float attackWidth = 1.2f;
    [SerializeField] private float attackHeight = 0.8f;
    [SerializeField] private int numberOfRays = 5;

    [Header("VFX")]
    [SerializeField] private GameObject attackHitPlayerVFX;
    [SerializeField] private LayerMask obstacleLayers;

    private MantisState currentState = MantisState.Idle;
    private Transform player;
    private float stateTimer = 0f;
    private float attackCooldownTimer = 0f;
    private bool playerAttackedDuringParry = false;
    private int currentSlashCount = 0;
    private int targetSlashCount = 0;

    private Vector2 roamTarget;
    private float roamTimer = 0f;
    private bool isWaitingToRoam = false;

    private EnemyModule enemyModule;
    private HealthModule healthModule;
    private Animator animator;
    private Animator slashAnimator;
    public GameObject slashObj;
    private SpriteRenderer spriteRenderer;
    private StickAttack cachedPlayerAttack;
    private Rigidbody2D rb;

    private bool isDead = false;
    private bool isDefensiveMode = false;
    private Coroutine currentActionCoroutine;
    private Coroutine jumpBackCoroutine;
    private Coroutine flashCoroutine;
    private bool hasHitThisAttack = false;
    private bool isDashing = false;
    private float lastPlayerAttackTime = -999f;

    public bool IsDefensiveMode => isDefensiveMode;

    void Start()
    {
        enemyModule = GetComponent<EnemyModule>();
        healthModule = GetComponent<HealthModule>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (slashObj != null)
        {
            slashAnimator = slashObj.GetComponent<Animator>();
            slashObj.SetActive(false);
        }

        PickNewRoamTarget();

        if (enemyModule != null)
        {
            enemyModule.Initialize(
                cooldown: 0.5f,
                detectionRange: detectionRange,
                attackRange: attackRange,
                layerMask: playerLayermask);

            enemyModule.OnTargetDetected += OnPlayerDetected;
        }

        if (healthModule != null)
        {
            healthModule.Initialize(80);
            healthModule.onHealthChanged += OnDamageTaken;
            healthModule.onDeath += HandleDeath;
            healthModule.onInvincDamage += OnParryTrigger;
        }
    }

    private void UpdateDefensiveMode()
    {
        if (healthModule == null) return;

        float healthPercent = healthModule.GetCurrentHealth() / healthModule.GetMaxHealth();
        bool shouldBeDefensive = healthPercent <= defensiveModeThreshold;

        if (shouldBeDefensive && !isDefensiveMode)
        {
            isDefensiveMode = true;
            EnterDefensiveMode();
        }
        else if (!shouldBeDefensive && isDefensiveMode)
        {
            isDefensiveMode = false;
        }
    }

    private void EnterDefensiveMode()
    {
        if (currentState == MantisState.Chasing || currentState == MantisState.Retreating)
        {
            StartDefensiveStance();
        }
    }

    private void StartDefensiveStance()
    {
        TransitionToState(MantisState.DefensiveStance);
        stateTimer = defensiveStanceDuration;
    }

    private void OnParryTrigger()
    {
        if (currentState == MantisState.Parrying && !playerAttackedDuringParry)
        {
            playerAttackedDuringParry = true;
            ExecuteParryCounter();
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

        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }

        if (jumpBackCoroutine != null)
        {
            StopCoroutine(jumpBackCoroutine);
            jumpBackCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
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

        UpdateDefensiveMode();

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (stateTimer > 0)
            stateTimer -= Time.deltaTime;

        if (enemyModule?.target == null)
        {
            if (currentState != MantisState.Roaming && currentState != MantisState.Idle)
            {
                TransitionToState(MantisState.Roaming);
            }
        }
        else if (player == null)
        {
            player = enemyModule.target.transform;
            cachedPlayerAttack = player.GetComponent<StickAttack>();
        }

        if (player != null && ShouldUpdateFacingDirection())
        {
            UpdateFacingDirection();
        }

        if ((currentState == MantisState.Chasing || currentState == MantisState.DefensiveStance) &&
            player != null && attackCooldownTimer <= 0)
        {
            CheckForIncomingAttack();
        }

        switch (currentState)
        {
            case MantisState.Idle:
                TransitionToState(MantisState.Roaming);
                break;
            case MantisState.Roaming:
                UpdateRoaming();
                break;
            case MantisState.Chasing:
                UpdateChasing();
                break;
            case MantisState.Attacking:
                UpdateAttacking();
                break;
            case MantisState.Retreating:
                UpdateRetreating();
                break;
            case MantisState.Parrying:
                UpdateParrying();
                break;
            case MantisState.DefensiveStance:
                UpdateDefensiveStance();
                break;
        }

        UpdateAnimator();
    }

    private bool ShouldUpdateFacingDirection()
    {
        if (isDashing) return false;
        if (currentState == MantisState.Attacking) return false;
        if (currentState == MantisState.Parrying) return false;
        if (currentState == MantisState.Retreating) return false;
        return true;
    }

    private void UpdateFacingDirection()
    {
        if (player == null) return;

        Vector2 directionToPlayer = player.position - transform.position;

        if (Mathf.Abs(directionToPlayer.x) > 0.1f)
        {
            float newScaleX = Mathf.Sign(directionToPlayer.x);
            transform.localScale = new Vector3(newScaleX, 1, 1);
        }
    }

    private void UpdateDefensiveStance()
    {
        if (player == null)
        {
            TransitionToState(MantisState.Roaming);
            return;
        }

        if (stateTimer <= 0)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (isDefensiveMode)
            {
                if (distanceToPlayer <= attackRange && attackCooldownTimer <= 0)
                {
                    StartDefensiveAttack();
                }
                else
                {
                    TransitionToState(MantisState.Chasing);
                    attackCooldownTimer = defensiveCooldown;
                }
            }
            else
            {
                TransitionToState(MantisState.Chasing);
            }
        }
    }

    private void StartDefensiveAttack()
    {
        TransitionToState(MantisState.Attacking);
        targetSlashCount = Random.Range(1, defensiveMaxSlashCount + 1);
        currentSlashCount = 0;
        currentActionCoroutine = StartCoroutine(ExecuteDefensiveSlashCombo());
    }

    private IEnumerator ExecuteDefensiveSlashCombo()
    {
        SetFacingDirectionToPlayer();

        for (int i = 0; i < targetSlashCount; i++)
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

            yield return new WaitForSeconds(0.2f);

            if (i < targetSlashCount - 1)
            {
                if (slashObj != null)
                {
                    slashObj.SetActive(false);
                    if (slashAnimator != null)
                    {
                        slashAnimator.SetBool(IsAttackingHash, false);
                    }
                }
                yield return new WaitForSeconds(timeBetweenSlashes);
            }

            currentSlashCount++;
        }

        if (slashObj != null)
        {
            slashObj.SetActive(false);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
            }
        }

        attackCooldownTimer = timeBetweenAttacks * 1.2f;
        StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
    }

    private void CheckForIncomingAttack()
    {
        if (cachedPlayerAttack == null || player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= parryDetectionRange)
        {
            if (cachedPlayerAttack.IsAttacking() && Time.time - lastPlayerAttackTime > 0.5f)
            {
                lastPlayerAttackTime = Time.time;

                float currentParryChance = isDefensiveMode ? defensiveParryChance : parryChance;

                if (Random.value < currentParryChance)
                {
                    StartParry();
                }
                else if (isDefensiveMode)
                {
                    QuickJumpBack();
                }
            }
        }
    }

    private void QuickJumpBack()
    {
        if (currentState == MantisState.DefensiveStance || currentState == MantisState.Chasing)
        {
            StartJumpBack(defensiveJumpBackForce);
        }
    }

    private void StartJumpBack(float force)
    {
        if (jumpBackCoroutine != null)
        {
            StopCoroutine(jumpBackCoroutine);
            jumpBackCoroutine = null;
        }

        TransitionToState(MantisState.Retreating);
        jumpBackCoroutine = StartCoroutine(ExecuteJumpBack(force));
    }

    private IEnumerator ExecuteJumpBack(float force)
    {
        animator.SetTrigger(JumpbackHash);

        Vector2 jumpDirection = Vector2.zero;
        if (player != null)
        {
            jumpDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
            jumpDirection = new Vector2(Mathf.Sign(jumpDirection.x), 0.5f).normalized;
        }
        else
        {
            jumpDirection = new Vector2(-Mathf.Sign(transform.localScale.x), 0.5f).normalized;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(jumpDirection * force, ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.5f);

        if (isDefensiveMode)
        {
            StartDefensiveStance();
        }
        else
        {
            TransitionToState(MantisState.Chasing);
        }

        jumpBackCoroutine = null;
    }

    private void TransitionToState(MantisState newState)
    {
        if (currentActionCoroutine != null)
        {
            StopCoroutine(currentActionCoroutine);
            currentActionCoroutine = null;
        }

        if (jumpBackCoroutine != null && newState != MantisState.Retreating)
        {
            StopCoroutine(jumpBackCoroutine);
            jumpBackCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
            spriteRenderer.color = Color.white;
        }

        if (currentState == MantisState.Parrying)
        {
            healthModule.invincible = false;
        }

        if (slashObj != null && currentState == MantisState.Attacking)
        {
            slashObj.SetActive(false);
        }

        playerAttackedDuringParry = false;
        isDashing = false;

        currentState = newState;
        stateTimer = 0f;
    }

    private void UpdateRoaming()
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
            if (Mathf.Abs(direction.x) > 0.1f)
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

    private void UpdateChasing()
    {
        if (player == null)
        {
            TransitionToState(MantisState.Roaming);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (isDefensiveMode)
        {
            UpdateDefensiveChasing(distanceToPlayer);
        }
        else
        {
            UpdateAggressiveChasing(distanceToPlayer);
        }
    }

    private void UpdateDefensiveChasing(float distanceToPlayer)
    {
        if (attackCooldownTimer <= 0)
        {
            if (distanceToPlayer <= attackRange)
            {
                StartDefensiveAttack();
            }
            else if (distanceToPlayer <= optimalDistance * 0.7f)
            {
                StartDashAttack();
            }
            else
            {
                if (distanceToPlayer > optimalDistance * 1.2f)
                {
                    MoveTowardsPlayer(chaseSpeed * 0.8f);
                }
            }
        }
    }

    private void UpdateAggressiveChasing(float distanceToPlayer)
    {
        if (attackCooldownTimer <= 0)
        {
            if (distanceToPlayer <= attackRange)
            {
                StartSlashCombo();
            }
            else if (distanceToPlayer <= optimalDistance)
            {
                StartDashAttack();
            }
            else
            {
                MoveTowardsPlayer(chaseSpeed);
            }
        }
    }

    private void UpdateAttacking()
    {
    }

    private void UpdateRetreating()
    {
    }

    private void UpdateParrying()
    {
        if (stateTimer <= 0)
        {
            healthModule.invincible = false;

            if (!playerAttackedDuringParry)
            {
                if (isDefensiveMode)
                {
                    StartDefensiveStance();
                }
                else
                {
                    TransitionToState(MantisState.Chasing);
                }
                attackCooldownTimer = 0.3f;
            }
        }
    }

    private void MoveTowardsPlayer(float speed)
    {
        if (player == null) return;

        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)direction * speed * Time.deltaTime;

        if (Mathf.Abs(direction.x) > 0.1f)
        {
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
        }
    }

    private void OnPlayerDetected(GameObject detectedPlayer)
    {
        if (currentState == MantisState.Idle || currentState == MantisState.Roaming)
        {
            player = detectedPlayer.transform;
            cachedPlayerAttack = player.GetComponent<StickAttack>();

            if (isDefensiveMode)
            {
                StartDefensiveStance();
            }
            else
            {
                TransitionToState(MantisState.Chasing);
            }
        }
    }

    private void StartParry()
    {
        TransitionToState(MantisState.Parrying);
        stateTimer = parryDuration;
        healthModule.invincible = true;
        playerAttackedDuringParry = false;
        StartFlash(Color.cyan, 0.1f);
    }

    private void ExecuteParryCounter()
    {
        TransitionToState(MantisState.Attacking);
        currentActionCoroutine = StartCoroutine(PerformParryCounter());
    }

    private IEnumerator PerformParryCounter()
    {
        SetFacingDirectionToPlayer();

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

        yield return new WaitForSeconds(0.08f);

        hasHitThisAttack = false;
        DetectHits(parryCounterDamage);

        yield return new WaitForSeconds(0.25f);

        if (slashObj != null)
        {
            slashObj.SetActive(false);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
            }
        }

        attackCooldownTimer = 0.3f;

        if (isDefensiveMode)
        {
            StartDefensiveStance();
        }
        else
        {
            TransitionToState(MantisState.Chasing);
        }
    }

    private void StartSlashCombo()
    {
        TransitionToState(MantisState.Attacking);
        targetSlashCount = Random.Range(minSlashCount, maxSlashCount + 1);
        currentSlashCount = 0;
        currentActionCoroutine = StartCoroutine(ExecuteSlashCombo());
    }

    private IEnumerator ExecuteSlashCombo()
    {
        SetFacingDirectionToPlayer();

        for (int i = 0; i < targetSlashCount; i++)
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

            yield return new WaitForSeconds(0.2f);

            if (i < targetSlashCount - 1)
            {
                if (slashObj != null)
                {
                    slashObj.SetActive(false);
                    if (slashAnimator != null)
                    {
                        slashAnimator.SetBool(IsAttackingHash, false);
                    }
                }
                yield return new WaitForSeconds(timeBetweenSlashes);
            }

            currentSlashCount++;
        }

        if (slashObj != null)
        {
            slashObj.SetActive(false);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
            }
        }

        attackCooldownTimer = timeBetweenAttacks;
        StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
    }

    private void StartDashAttack()
    {
        TransitionToState(MantisState.Attacking);
        currentActionCoroutine = StartCoroutine(ExecuteDashAttack());
    }

    private IEnumerator ExecuteDashAttack()
    {
        if (player == null) yield break;

        SetFacingDirectionToPlayer();

        isDashing = true;
        Vector2 dashDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;

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

        float dashTimer = 0f;
        hasHitThisAttack = false;

        while (dashTimer < dashAttackDuration)
        {
            if (player == null) break;

            Vector2 currentDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)currentDirection * dashAttackSpeed * Time.deltaTime;

            if (!hasHitThisAttack)
            {
                DetectHits(slashDamage * 1.2f);
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;

        if (slashObj != null)
        {
            slashObj.SetActive(false);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
            }
        }

        attackCooldownTimer = timeBetweenAttacks;

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= attackRange)
            {
                StartSlashCombo();
            }
            else
            {
                StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
            }
        }
        else
        {
            TransitionToState(MantisState.Roaming);
        }
    }

    private void SetFacingDirectionToPlayer()
    {
        if (player == null) return;

        Vector2 directionToPlayer = player.position - transform.position;
        if (Mathf.Abs(directionToPlayer.x) > 0.1f)
        {
            transform.localScale = new Vector3(Mathf.Sign(directionToPlayer.x), 1, 1);
        }
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
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, attackRange, playerLayermask | obstacleLayers);
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
        return transform.localScale.x > 0 ? Vector2.right : Vector2.left;
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

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(IsBlockingHash, currentState == MantisState.Parrying || currentState == MantisState.DefensiveStance);
        animator.SetBool(IsAttackingHash, currentState == MantisState.Attacking);

        float moveSpeed = 0f;
        switch (currentState)
        {
            case MantisState.Chasing:
                moveSpeed = isDefensiveMode ? chaseSpeed * 0.8f : chaseSpeed;
                break;
            case MantisState.Roaming:
                if (!isWaitingToRoam)
                    moveSpeed = roamSpeed;
                break;
            case MantisState.Attacking:
                if (isDashing)
                    moveSpeed = dashAttackSpeed;
                break;
        }

        animator.SetFloat(MoveSpeedHash, moveSpeed);
    }

    public void OnDamageTaken(float current, float max)
    {
        if (current <= 0) return;

        StartFlash(Color.red, 0.1f);

        if (isDefensiveMode && current > 0)
        {
            QuickJumpBack();
            attackCooldownTimer = defensiveCooldown;
        }
        else if (currentState == MantisState.Chasing || currentState == MantisState.Retreating || currentState == MantisState.Attacking)
        {
            StartJumpBack(jumpBackForce);
            attackCooldownTimer = 0.4f;
        }
    }

    private void StartFlash(Color color, float duration)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashColor(color, duration));
    }

    private IEnumerator FlashColor(Color color, float duration)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(duration);
            spriteRenderer.color = Color.white;
        }
        flashCoroutine = null;
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
            healthModule.onInvincDamage -= OnParryTrigger;
        }
    }

   
}