using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    public StickAttack cachedPlayerAttack;
    private Rigidbody2D rb;

    private bool isDead = false;
    private bool isDefensiveMode = false;
    private Coroutine currentActionCoroutine;
    private Coroutine jumpBackCoroutine;
    private Coroutine flashCoroutine;
    private bool hasHitThisAttack = false;
    private bool isDashing = false;

    private bool isInitialized = false;
    private List<Coroutine> activeCoroutines = new List<Coroutine>();
    private float lastStateChangeTime = 0f;
    private const float MIN_STATE_CHANGE_TIME = 0.1f;
    private bool hasDetectedPlayer = false;
    private bool isAttackInProgress = false;
    private bool shouldMoveDuringAttack = false;

    public GameObject cachedPlayerObject = null;
    private Vector2 lastKnownPlayerPosition;
    private float lastPlayerSeenTime = -999f;
    private const float PLAYER_SEARCH_DURATION = 10f; 

    public bool IsDefensiveMode => isDefensiveMode;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

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
        else
        {
            Debug.LogWarning("SlashObj not assigned in Mantis script");
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
        else
        {
            Debug.LogError("EnemyModule not found on Mantis");
        }

        if (healthModule != null)
        {
            healthModule.Initialize(80);
            healthModule.onHealthChanged += OnDamageTaken;
            healthModule.onDeath += HandleDeath;
            healthModule.onInvincDamage += OnParryTrigger;
        }
        else
        {
            Debug.LogError("HealthModule not found on Mantis");
        }

        isInitialized = true;
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

        if (animator != null)
        {
            animator.SetBool(IsBlockingHash, false);
            animator.SetBool(IsAttackingHash, false);
            animator.SetFloat(MoveSpeedHash, 0f);
            animator.SetBool(DeathHash, true);
        }

        spriteRenderer.color = Color.white;
        CleanupAllCoroutines();

        if (slashObj != null)
        {
            slashObj.SetActive(false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        yield return new WaitForSeconds(2f);

        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void CleanupAllCoroutines()
    {
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

        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        activeCoroutines.Clear();

        isAttackInProgress = false;
        shouldMoveDuringAttack = false;
    }

    void Update()
    {
        if (isDead || !isInitialized) return;

        UpdateDefensiveMode();

        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (stateTimer > 0)
            stateTimer -= Time.deltaTime;

        UpdatePlayerReference();

        if (player != null && ShouldUpdateFacingDirection())
        {
            UpdateFacingDirection();
        }

        switch (currentState)
        {
            case MantisState.Idle:
                HandleIdleState();
                break;
            case MantisState.Roaming:
                HandleRoamingState();
                break;
            case MantisState.Chasing:
                HandleChasingState();
                break;
            case MantisState.Attacking:
                HandleAttackingState();
                break;
            case MantisState.Retreating:
                break;
            case MantisState.Parrying:
                HandleParryingState();
                break;
            case MantisState.DefensiveStance:
                HandleDefensiveStanceState();
                break;
        }

        UpdateAnimator();
    }

    private void UpdatePlayerReference()
    {
        if (cachedPlayerObject != null)
        {
            if (cachedPlayerObject.activeInHierarchy && cachedPlayerObject.transform != null)
            {
                player = cachedPlayerObject.transform;
                cachedPlayerAttack = player?.GetComponent<StickAttack>();

                if (player != null && IsPlayerInDetectionRange())
                {
                    lastKnownPlayerPosition = player.position;
                    lastPlayerSeenTime = Time.time;
                }
                return;
            }
            else
            {
                cachedPlayerObject = null;
                player = null;
                cachedPlayerAttack = null;
            }
        }
        if (enemyModule?.target != null)
        {
            OnPlayerDetected(enemyModule.target);
        }
    }

  

    private void HandleIdleState()
    {
        if (hasDetectedPlayer)
        {
            TransitionToState(MantisState.Chasing);
        }
        else
        {
            TransitionToState(MantisState.Roaming);
        }
    }

    private void HandleRoamingState()
    {
        if (hasDetectedPlayer)
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
        direction.y = 0;
        float distanceToTarget = Vector2.Distance(transform.position, roamTarget);

        if (distanceToTarget > 0.5f)
        {
            if (rb != null)
            {
                rb.linearVelocity = direction * roamSpeed;
            }
            else
            {
                transform.position += (Vector3)direction * roamSpeed * Time.deltaTime;
            }

            if (Mathf.Abs(direction.x) > 0.1f)
            {
                transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);
            }
        }
        else
        {
            isWaitingToRoam = true;
            roamTimer = roamWaitTime;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleChasingState()
    {
        if (cachedPlayerObject == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        player = cachedPlayerObject.transform; 
        UpdateFacingDirection();

        float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);

        MoveTowardsPlayer(chaseSpeed);

        if (attackCooldownTimer <= 0 && !isAttackInProgress)
        {
            if (distanceToPlayer <= attackRange)
            {
                if (isDefensiveMode)
                    StartDefensiveAttack();
                else
                    StartSlashCombo();
            }
            else if (distanceToPlayer <= optimalDistance)
            {
                StartDashAttack();
            }
        }
    }

    private bool IsPlayerInDetectionRange()
    {
        return cachedPlayerObject != null;
    }

   

    private void HandleAttackingState()
    {
        if (player != null && ShouldUpdateFacingDirection())
        {
            UpdateFacingDirection();
        }

        if (shouldMoveDuringAttack && player != null)
        {
            Vector2 direction = new Vector2(Mathf.Sign(player.position.x - transform.position.x), 0);
            if (rb != null)
            {
                rb.linearVelocity = direction * chaseSpeed * 0.3f;
            }
        }
        else
        {
            if (rb != null && !isDashing)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void HandleParryingState()
    {
        if (stateTimer <= 0)
        {
            if (healthModule != null)
            {
                healthModule.invincible = false;
            }

            if (!playerAttackedDuringParry)
            {
                TransitionToState(MantisState.Chasing);
                attackCooldownTimer = 0.3f;
            }
        }
    }

    private void HandleDefensiveStanceState()
    {
        if (player == null)
        {
            TransitionToState(MantisState.Chasing);
            return;
        }

        if (stateTimer <= 0)
        {
            TransitionToState(MantisState.Chasing);
        }
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
            transform.localScale = new Vector3(Mathf.Sign(directionToPlayer.x), 1, 1);
        }
    }

    private void StartDefensiveAttack()
    {
        if (isAttackInProgress || attackCooldownTimer > 0) return;

        TransitionToState(MantisState.Attacking);
        targetSlashCount = Random.Range(1, defensiveMaxSlashCount + 1);
        currentSlashCount = 0;
        isAttackInProgress = true;
        shouldMoveDuringAttack = false;
        attackCooldownTimer = timeBetweenAttacks * 1.5f;
        currentActionCoroutine = StartCoroutine(ExecuteDefensiveSlashCombo());
        TrackCoroutine(currentActionCoroutine);
    }

    private IEnumerator ExecuteDefensiveSlashCombo()
    {
        SetFacingDirectionToPlayer();

        for (int i = 0; i < targetSlashCount; i++)
        {
            if (currentState != MantisState.Attacking || isDead || !IsPlayerInAttackRange())
            {
                SafeSlashObjectDeactivation();
                isAttackInProgress = false;
                shouldMoveDuringAttack = false;
                yield break;
            }

            if (slashObj != null)
            {
                slashObj.SetActive(true);
                if (slashAnimator != null)
                {
                    slashAnimator.SetBool(IsAttackingHash, true);
                }
            }
            SoundManager.Instance.PlaySFX("mantis_swing");
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
                SafeSlashObjectDeactivation();
                yield return new WaitForSeconds(timeBetweenSlashes);

                if (!IsPlayerInAttackRange())
                {
                    SafeSlashObjectDeactivation();
                    isAttackInProgress = false;
                    shouldMoveDuringAttack = false;
                    yield break;
                }
            }

            currentSlashCount++;
        }

        SafeSlashObjectDeactivation();
        isAttackInProgress = false;
        shouldMoveDuringAttack = false;
        yield return new WaitForSeconds(0.2f);
        StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
    }

    private bool IsPlayerInAttackRange()
    {
        if (player == null) return false;
        float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
        return distanceToPlayer <= attackRange * 2f;
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
        TrackCoroutine(jumpBackCoroutine);
    }

    private IEnumerator ExecuteJumpBack(float force)
    {
        if (animator != null)
        {
            animator.SetTrigger(JumpbackHash);
        }

        Vector2 jumpDirection = Vector2.zero;
        if (player != null)
        {
            jumpDirection = new Vector2(-Mathf.Sign(player.position.x - transform.position.x), 0.5f).normalized;
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

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (player != null)
        {
            UpdateFacingDirection();
        }

        if (isDefensiveMode && !isDead)
        {
            StartDefensiveStance();
        }
        else if (!isDead)
        {
            TransitionToState(MantisState.Chasing);
        }

        jumpBackCoroutine = null;
    }

    private void TransitionToState(MantisState newState)
    {
        if (currentState == newState) return;

        if (Time.time - lastStateChangeTime < MIN_STATE_CHANGE_TIME)
        {
            return;
        }

        lastStateChangeTime = Time.time;

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
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        if (currentState == MantisState.Parrying && healthModule != null)
        {
            healthModule.invincible = false;
        }

        SafeSlashObjectDeactivation();

        playerAttackedDuringParry = false;
        isDashing = false;
        isAttackInProgress = false;
        shouldMoveDuringAttack = false;

        if (newState != MantisState.Retreating && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        currentState = newState;
        stateTimer = 0f;
    }

    private void MoveTowardsPlayer(float speed)
    {
        if (player == null || rb == null) return;

        Vector2 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        rb.linearVelocity = direction * speed;
    }


    private void OnPlayerDetected(GameObject detectedPlayer)
    {
        hasDetectedPlayer = true;
        cachedPlayerObject = detectedPlayer;
        player = detectedPlayer.transform;
        cachedPlayerAttack = player?.GetComponent<StickAttack>();
        lastKnownPlayerPosition = player.position;
        lastPlayerSeenTime = Time.time;

        TransitionToState(MantisState.Chasing);
    }

   

    private void ExecuteParryCounter()
    {
        if (isAttackInProgress || attackCooldownTimer > 0) return;

        TransitionToState(MantisState.Attacking);
        isAttackInProgress = true;
        shouldMoveDuringAttack = false;
        attackCooldownTimer = timeBetweenAttacks * 0.8f;
        currentActionCoroutine = StartCoroutine(PerformParryCounter());
        TrackCoroutine(currentActionCoroutine);
    }

    private IEnumerator PerformParryCounter()
    {
        SetFacingDirectionToPlayer();

        if (!IsPlayerInAttackRange())
        {
            TransitionToState(MantisState.Chasing);
            isAttackInProgress = false;
            shouldMoveDuringAttack = false;
            yield break;
        }

        if (slashObj != null)
        {
            slashObj.SetActive(true);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, true);
            }
        }
        SoundManager.Instance.PlaySFX("mantis_swing");

        if (animator != null)
        {
            animator.SetTrigger(CounterAttackHash);
        }

        yield return new WaitForSeconds(0.08f);

        hasHitThisAttack = false;
        DetectHits(parryCounterDamage);

        yield return new WaitForSeconds(0.25f);

        SafeSlashObjectDeactivation();
        isAttackInProgress = false;
        shouldMoveDuringAttack = false;
        TransitionToState(MantisState.Chasing);
    }

    private void StartSlashCombo()
    {
        if (isAttackInProgress || attackCooldownTimer > 0) return;

        TransitionToState(MantisState.Attacking);
        targetSlashCount = Random.Range(minSlashCount, maxSlashCount + 1);
        currentSlashCount = 0;
        isAttackInProgress = true;
        shouldMoveDuringAttack = false;
        attackCooldownTimer = timeBetweenAttacks;
        currentActionCoroutine = StartCoroutine(ExecuteSlashCombo());
        TrackCoroutine(currentActionCoroutine);
    }

    private IEnumerator ExecuteSlashCombo()
    {
        SetFacingDirectionToPlayer();

        for (int i = 0; i < targetSlashCount; i++)
        {
            if (currentState != MantisState.Attacking || isDead || !IsPlayerInAttackRange())
            {
                SafeSlashObjectDeactivation();
                isAttackInProgress = false;
                shouldMoveDuringAttack = false;
                yield break;
            }

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
            SoundManager.Instance.PlaySFX("mantis_swing");
            hasHitThisAttack = false;
            DetectHits(slashDamage);

            yield return new WaitForSeconds(0.2f);

            if (i < targetSlashCount - 1)
            {
                SafeSlashObjectDeactivation();
                yield return new WaitForSeconds(timeBetweenSlashes);

                if (!IsPlayerInAttackRange())
                {
                    SafeSlashObjectDeactivation();
                    isAttackInProgress = false;
                    shouldMoveDuringAttack = false;
                    yield break;
                }
            }

            currentSlashCount++;
        }

        SafeSlashObjectDeactivation();
        isAttackInProgress = false;
        shouldMoveDuringAttack = false;
        yield return new WaitForSeconds(0.2f);
        StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
    }

    private void StartDashAttack()
    {
        if (isAttackInProgress || attackCooldownTimer > 0) return;

        TransitionToState(MantisState.Attacking);
        isAttackInProgress = true;
        shouldMoveDuringAttack = true;
        attackCooldownTimer = timeBetweenAttacks * 1.2f;
        currentActionCoroutine = StartCoroutine(ExecuteDashAttack());
        TrackCoroutine(currentActionCoroutine);
    }

    private IEnumerator ExecuteDashAttack()
    {
        if (player == null)
        {
            isAttackInProgress = false;
            shouldMoveDuringAttack = false;
            yield break;
        }

        SetFacingDirectionToPlayer();

        isDashing = true;
        Vector2 dashDirection = new Vector2(Mathf.Sign(player.position.x - transform.position.x), 0);

        if (slashObj != null)
        {
            slashObj.SetActive(true);
            if (slashAnimator != null)
            {
                slashAnimator.SetBool(IsAttackingHash, true);
            }
        }

        SoundManager.Instance.PlaySFX("mantis_swing");

        if (animator != null)
        {
            animator.SetTrigger(SlashTriggerHash);
        }

        float dashTimer = 0f;
        hasHitThisAttack = false;

        while (dashTimer < dashAttackDuration)
        {
            if (player == null || isDead) break;

            Vector2 currentDirection = new Vector2(Mathf.Sign(player.position.x - transform.position.x), 0);

            if (rb != null)
            {
                rb.linearVelocity = currentDirection * dashAttackSpeed;
            }
            else
            {
                transform.position += (Vector3)currentDirection * dashAttackSpeed * Time.deltaTime;
            }

            if (!hasHitThisAttack)
            {
                DetectHits(slashDamage * 1.2f);
            }

            dashTimer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        isAttackInProgress = false;
        shouldMoveDuringAttack = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        SafeSlashObjectDeactivation();

        if (player != null && !isDead)
        {
            float distanceToPlayer = Mathf.Abs(player.position.x - transform.position.x);
            if (distanceToPlayer <= attackRange)
            {
                StartSlashCombo();
            }
            else
            {
                StartJumpBack(isDefensiveMode ? defensiveJumpBackForce : jumpBackForce);
            }
        }
        else if (!isDead)
        {
            TransitionToState(MantisState.Chasing);
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
        if (hasHitThisAttack) return;

        Vector2 origin = GetAttackOrigin();
        Vector2 size = GetAttackSize();
        Vector2 direction = GetAttackDirectionVector();

        RaycastHit2D[] hits = PerformMultiRaycast(origin, size, direction);
        ProcessHits(hits, damage);
    }

    RaycastHit2D[] PerformMultiRaycast(Vector2 origin, Vector2 size, Vector2 direction)
    {
        List<RaycastHit2D> allHits = new List<RaycastHit2D>();

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
        if (!isPlayer) return;

        HealthModule playerHealth = other.GetComponent<HealthModule>();
        if (playerHealth == null) return;

        hasHitThisAttack = true;

        PlayHitVFX(hitPoint, isPlayer);
        playerHealth.TakeDamage(damage);
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
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), 0).normalized;
        roamTarget = (Vector2)transform.position + randomDirection * Random.Range(roamDistance * 0.5f, roamDistance);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(IsBlockingHash, currentState == MantisState.Parrying || currentState == MantisState.DefensiveStance);

        if (currentState != MantisState.Attacking)
        {
            animator.SetBool(IsAttackingHash, false);
        }

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
        if (current <= 0 || isDead) return;


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



    private void TrackCoroutine(Coroutine coroutine)
    {
        if (coroutine != null && !activeCoroutines.Contains(coroutine))
        {
            activeCoroutines.Add(coroutine);
        }
    }

    private void SafeSlashObjectDeactivation()
    {
        if (slashObj != null)
        {
            slashObj.SetActive(false);
            if (slashAnimator != null && slashAnimator.gameObject.activeSelf == true)
            {
                slashAnimator.SetBool(IsAttackingHash, false);
            }
        }
    }

    private void OnDisable()
    {
        CleanupAllCoroutines();
    }

    private void OnDestroy()
    {
        CleanupAllCoroutines();

        if (enemyModule != null)
        {
            enemyModule.OnTargetDetected -= OnPlayerDetected;
        }

        if (healthModule != null)
        {
            healthModule.onHealthChanged -= OnDamageTaken;
            healthModule.onInvincDamage -= OnParryTrigger;
            healthModule.onDeath -= HandleDeath;
        }
    }
}