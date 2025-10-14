using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering.LookDev;

public class SquirrelBoss : MonoBehaviour
{
    [Header("Boss Settings")]
    public int phase = 1;
    public float phase2HealthThreshold = 0.5f;

    [Header("Movement")]
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    public float airControlMultiplier = 0.7f;

    [Header("Attack Settings")]
    public GameObject acornProjectile;
    public GameObject rollingAcornPrefab;
    public Vector3 throwOffset = new Vector3(1f, 0.5f, 0f);
    public float throwForce = 15f;
    public int biteDamage = 2;
    public float attackCooldown = 1.5f;
    public float telegraphTime = 0.5f;

    [Header("Special Phase 2")]
    public GameObject miniSquirrelPrefab;
    public int numberOfMiniSquirrels = 3;
    public float healAmount = 0.3f;

    [Header("Visual Feedback")]
    public ParticleSystem hitParticles;
    public ParticleSystem phaseTransitionParticles;
    public Color phase1Color = Color.white;
    public Color phase2Color = new Color(1f, 0.3f, 0.3f);
    public float flashDuration = 0.1f;

    [Header("Audio")]
    public AudioClip roarSound;
    public AudioClip biteSound;
    public AudioClip impactSound;
    public AudioClip phaseTransitionSound;

    // References
    private EnemyModule enemyModule;
    private HealthModule healthModule;
    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    // State machine
    private enum BossState { Idle, Telegraphing, Attacking, Recovering, Special, PhaseTransition, Stunned, Dead }
    private BossState currentState = BossState.Idle;
    private BossState previousState;

    // Attack tracking
    private List<AttackData> phase1Attacks;
    private List<AttackData> phase2Attacks;
    private AttackData lastUsedAttack;
    private AttackData currentAttackData;
    private float stateTimer;
    private bool specialAbilityUsed = false;
    private List<GameObject> miniSquirrels = new List<GameObject>();
    private int consecutiveSameAttacks = 0;

    // Combat dynamics
    private bool isInvulnerable = false;
    private float recoveryTime = 0.3f;
    private Vector2 knockbackVelocity;
    private bool isGrounded;
    private float groundCheckRadius = 0.2f;
    private LayerMask groundLayer;

    // Visual effects
    private Color originalColor;
    private bool isFlashing = false;
    private MaterialPropertyBlock propBlock;

    // Rolling acorn reference
    private RollingBouncingBall currentRollingAcorn;

    // Attack data structure
    private class AttackData
    {
        public System.Action attackAction;
        public string name;
        public float weight;
        public float minRange;
        public float maxRange;

        public AttackData(System.Action action, string n, float w = 1f, float minR = 0f, float maxR = 999f)
        {
            attackAction = action;
            name = n;
            weight = w;
            minRange = minR;
            maxRange = maxR;
        }
    }

    void Start()
    {
        // Get components
        enemyModule = GetComponent<EnemyModule>();
        healthModule = GetComponent<HealthModule>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        groundLayer = LayerMask.GetMask("Ground");

        originalColor = spriteRenderer.color;
        propBlock = new MaterialPropertyBlock();

        // Initialize modules
        healthModule.Initialize(150f);
        enemyModule.Initialize(
            cooldown: attackCooldown,
            detectionRange: 20f,
            attackRange: 15f,
            layerMask: LayerMask.GetMask("Player")
        );

        // Set up events
        healthModule.onHealthChanged += OnHealthChanged;
        healthModule.onDeath += Die;
        enemyModule.OnTargetDetected += OnTargetDetected;

        // Setup attack patterns
        InitializeAttackPatterns();

        // Start in idle
        TransitionToState(BossState.Idle);
    }

    void InitializeAttackPatterns()
    {
        // Phase 1: Balanced attacks
        phase1Attacks = new List<AttackData>
        {
            new AttackData(AcornRollAttack, "Acorn Roll", 1.2f, 0f, 999f),
            new AttackData(ChargeAttack, "Charge", 1.5f, 5f, 999f),
            new AttackData(LeapBite, "Leap Bite", 1.3f, 3f, 10f),
            new AttackData(GroundBite, "Ground Bite", 1.0f, 0f, 5f)
        };

        // Phase 2: More aggressive, adds new attacks
        phase2Attacks = new List<AttackData>
        {
            new AttackData(AcornRollAttack, "Acorn Roll", 1.0f, 0f, 999f),
            new AttackData(FrenzyCharge, "Frenzy Charge", 1.8f, 5f, 999f),
            new AttackData(LeapBite, "Leap Bite", 1.5f, 3f, 10f),
            new AttackData(GroundBite, "Ground Bite", 1.2f, 0f, 5f),
            new AttackData(AcornRain, "Acorn Rain", 1.3f, 7f, 999f),
            new AttackData(SpecialEscape, "Special Escape", 0.5f, 0f, 999f)
        };
    }

    void Update()
    {
        if (currentState == BossState.Dead || healthModule.currentHealth <= 0) return;

        // Update grounded state
        isGrounded = CheckGrounded();

        // Update state machine
        UpdateState();

        // Update facing direction (except during specific attacks)
        if (currentState != BossState.Attacking && currentState != BossState.Recovering)
        {
            UpdateFacingDirection();
        }

        // Update timers
        stateTimer -= Time.deltaTime;

        // Apply knockback damping
        if (knockbackVelocity.magnitude > 0.1f)
        {
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.deltaTime * 5f);
        }
    }

    void FixedUpdate()
    {
        if (currentState == BossState.Dead) return;

        // Apply any knockback
        if (knockbackVelocity.magnitude > 0.1f)
        {
            rb.linearVelocity = new Vector2(knockbackVelocity.x, rb.linearVelocity.y);
        }
    }

    void UpdateState()
    {
        switch (currentState)
        {
            case BossState.Idle:
                HandleIdleState();
                break;
            case BossState.Telegraphing:
                HandleTelegraphingState();
                break;
            case BossState.Attacking:
                // Handled by coroutines
                break;
            case BossState.Recovering:
                HandleRecoveringState();
                break;
            case BossState.PhaseTransition:
                HandlePhaseTransitionState();
                break;
            case BossState.Stunned:
                HandleStunnedState();
                break;
        }
    }

    void HandleIdleState()
    {
        // Slow down movement
        rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0, Time.deltaTime * 3f), rb.linearVelocity.y);

        if (stateTimer <= 0 && player != null)
        {
            ChooseNextAttack();
        }
    }

    void HandleTelegraphingState()
    {
        // Face player during telegraph
        UpdateFacingDirection();

        if (stateTimer <= 0 && currentAttackData != null)
        {
            ExecuteAttack();
        }
    }

    void HandleRecoveringState()
    {
        // Slow movement during recovery
        rb.linearVelocity = new Vector2(
            Mathf.Lerp(rb.linearVelocity.x, 0, Time.deltaTime * 4f),
            rb.linearVelocity.y
        );

        if (stateTimer <= 0)
        {
            TransitionToState(BossState.Idle);
        }
    }

    void HandlePhaseTransitionState()
    {
        rb.linearVelocity = Vector2.zero;

        if (stateTimer <= 0)
        {
            TransitionToState(BossState.Idle);
        }
    }

    void HandleStunnedState()
    {
        if (stateTimer <= 0)
        {
            TransitionToState(BossState.Idle);
        }
    }

    void TransitionToState(BossState newState)
    {
        previousState = currentState;
        currentState = newState;

        // Set default timers based on state
        switch (newState)
        {
            case BossState.Idle:
                stateTimer = Random.Range(0.3f, 0.8f);
                if (anim != null) anim.SetTrigger("Idle");
                break;
            case BossState.Recovering:
                stateTimer = recoveryTime;
                if (anim != null) anim.SetTrigger("Recover");
                break;
        }
    }

    void ChooseNextAttack()
    {
        if (player == null) return;

        List<AttackData> availableAttacks = phase == 1 ? phase1Attacks : phase2Attacks;
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Filter attacks by range and availability
        List<AttackData> validAttacks = new List<AttackData>();

        foreach (AttackData attack in availableAttacks)
        {
            // Skip special escape if already used
            if (attack.name == "Special Escape" && specialAbilityUsed) continue;

            // Check range
            if (distanceToPlayer >= attack.minRange && distanceToPlayer <= attack.maxRange)
            {
                // Reduce weight if same as last attack
                float weight = attack.weight;
                if (lastUsedAttack != null && attack.name == lastUsedAttack.name)
                {
                    weight *= 0.3f; // Reduce chance of repeating
                }

                validAttacks.Add(attack);
            }
        }

        if (validAttacks.Count == 0)
        {
            validAttacks = availableAttacks; // Fallback
        }

        // Weighted random selection
        float totalWeight = 0f;
        foreach (AttackData a in validAttacks) totalWeight += a.weight;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (AttackData attack in validAttacks)
        {
            cumulative += attack.weight;
            if (randomValue <= cumulative)
            {
                currentAttackData = attack;
                lastUsedAttack = attack;
                break;
            }
        }

        TelegraphAttack();
    }

    void TelegraphAttack()
    {
        TransitionToState(BossState.Telegraphing);
        stateTimer = telegraphTime;

        // Play appropriate telegraph animation
        if (currentAttackData.name == "Acorn Roll")
        {
            anim.SetTrigger("TelegraphRoll");
        }
        else if (currentAttackData.name == "Charge" || currentAttackData.name == "Frenzy Charge")
        {
            anim.SetTrigger("TelegraphCharge");
            PlaySound(roarSound, 0.6f);
        }
        else if (currentAttackData.name == "Leap Bite")
        {
            anim.SetTrigger("TelegraphJump");
        }
        else if (currentAttackData.name == "Acorn Rain")
        {
            anim.SetTrigger("TelegraphThrow");
        }
        else if (currentAttackData.name == "Ground Bite")
        {
            anim.SetTrigger("TelegraphBite");
        }

        // Visual feedback - quick flash
        StartCoroutine(FlashSprite(Color.yellow, telegraphTime * 0.5f));
    }

    void ExecuteAttack()
    {
        TransitionToState(BossState.Attacking);
        currentAttackData.attackAction.Invoke();
    }

    void UpdateFacingDirection()
    {
        if (player != null)
        {
            float direction = Mathf.Sign(player.position.x - transform.position.x);
            if (Mathf.Abs(direction) > 0.1f)
            {
                transform.localScale = new Vector3(direction, 1, 1);
            }
        }
    }

    bool CheckGrounded()
    {
        return Physics2D.OverlapCircle(
            transform.position + Vector3.down * 0.5f,
            groundCheckRadius,
            groundLayer
        );
    }

    // === ATTACK IMPLEMENTATIONS ===

    void AcornRollAttack() => StartCoroutine(AcornRollAttackRoutine());

    IEnumerator AcornRollAttackRoutine()
    {
        anim.SetTrigger("StartRoll");

        // Create rolling acorn with impact
        if (rollingAcornPrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1f;
            GameObject acornObj = Instantiate(rollingAcornPrefab, spawnPos, Quaternion.identity);

            if (acornObj != null)
            {
                currentRollingAcorn = acornObj.GetComponent<RollingBouncingBall>();

                if (currentRollingAcorn != null)
                {
                    // Try to start rolling - if it fails, the acorn might need initialization
                    try
                    {
                        currentRollingAcorn.StartRolling();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to start rolling acorn: {e.Message}");
                        // Acorn still exists, just won't roll properly
                    }

                    // Give acorn more health
                    HealthModule acornHealth = acornObj.GetComponent<HealthModule>();
                    if (acornHealth != null) acornHealth.Initialize(10);
                }
                else
                {
                    Debug.LogWarning("RollingBouncingBall component not found on acorn prefab!");
                }
            }
        }
        else
        {
            Debug.LogWarning("Rolling acorn prefab not assigned!");
        }

        // Wait a bit after spawning acorn
        yield return new WaitForSeconds(0.8f);

        // Epic jump with charge-up
        anim.SetTrigger("JumpHigh");
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(0.3f);

        rb.AddForce(Vector2.up * jumpForce * 2f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.5f);

        // Meteor drop
        anim.SetTrigger("QuickDrop");
        rb.linearVelocity = new Vector2(0, -30f);

        yield return new WaitUntil(() => isGrounded);

        // Impact effects
        CreateLandingShockwave();
        CameraShake(0.5f, 0.3f);
        PlaySound(impactSound, 0.8f);

        yield return new WaitForSeconds(0.5f);

        TransitionToState(BossState.Recovering);
    }

    void ChargeAttack() => StartCoroutine(ChargeAttackRoutine(1, 1.3f));
    void FrenzyCharge() => StartCoroutine(ChargeAttackRoutine(3, 1.5f));

    IEnumerator ChargeAttackRoutine(int charges, float speedMultiplier)
    {
        anim.SetTrigger("Charge");

        for (int i = 0; i < charges; i++)
        {
            // Lock onto player position
            Vector2 targetPos = player.position;
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;

            // Face charge direction
            transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);

            float chargeDuration = 0.6f;
            float chargeTimer = 0f;

            // Charge with acceleration
            while (chargeTimer < chargeDuration)
            {
                float acceleration = Mathf.Lerp(0.3f, 1f, chargeTimer / chargeDuration);
                rb.linearVelocity = direction * runSpeed * speedMultiplier * acceleration;

                chargeTimer += Time.deltaTime;
                yield return null;
            }

            // Brief stop with dust effect
            rb.linearVelocity = Vector2.zero;
            CameraShake(0.2f, 0.1f);

            if (i < charges - 1)
            {
                yield return new WaitForSeconds(0.4f);
            }
        }

        yield return new WaitForSeconds(0.3f);
        TransitionToState(BossState.Recovering);
    }

    void LeapBite() => StartCoroutine(LeapBiteRoutine());

    IEnumerator LeapBiteRoutine()
    {
        anim.SetTrigger("JumpBite");

        // Jump up
        Vector2 jumpDir = Vector2.up + Vector2.right * Mathf.Sign(player.position.x - transform.position.x) * 0.3f;
        rb.AddForce(jumpDir.normalized * jumpForce * 1.2f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.3f);

        // Dive toward player
        Vector2 diveDirection = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = diveDirection * runSpeed * 1.8f;

        yield return new WaitForSeconds(0.4f);

        // Wait for landing
        yield return new WaitUntil(() => isGrounded);

        PlaySound(impactSound, 0.5f);
        CameraShake(0.3f, 0.15f);

        yield return new WaitForSeconds(0.3f);
        TransitionToState(BossState.Recovering);
    }

    void GroundBite() => StartCoroutine(GroundBiteRoutine());

    IEnumerator GroundBiteRoutine()
    {
        anim.SetTrigger("LowBite");

        Vector2 hopDirection = new Vector2(
            Mathf.Sign(player.position.x - transform.position.x),
            0.5f
        ).normalized;

        rb.AddForce(hopDirection * jumpForce * 0.7f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.5f);

        // Bite hitbox activated via animation event
        PlaySound(biteSound, 0.7f);

        yield return new WaitForSeconds(0.5f);
        TransitionToState(BossState.Recovering);
    }

    void AcornRain() => StartCoroutine(AcornRainRoutine());

    IEnumerator AcornRainRoutine()
    {
        anim.SetTrigger("AcornRain");

        // Jump back for safety
        Vector2 retreatDir = new Vector2(
            -Mathf.Sign(player.position.x - transform.position.x),
            1f
        ).normalized;

        rb.AddForce(retreatDir * jumpForce * 0.8f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.5f);

        // Rain acorns around player
        int waveCount = phase == 1 ? 2 : 3;
        int acornsPerWave = phase == 1 ? 4 : 5;

        for (int wave = 0; wave < waveCount; wave++)
        {
            Vector2 targetArea = player.position;

            for (int i = 0; i < acornsPerWave; i++)
            {
                float angle = (360f / acornsPerWave) * i + (wave * 30f);
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * 3f;

                Vector2 spawnPos = targetArea + offset + Vector2.up * 8f;
                GameObject acorn = Instantiate(acornProjectile, spawnPos, Quaternion.identity);

                Rigidbody2D acornRb = acorn.GetComponent<Rigidbody2D>();
                if (acornRb)
                {
                    acornRb.linearVelocity = Vector2.down * 12f;
                    acornRb.gravityScale = 0.5f;
                }
            }

            yield return new WaitForSeconds(0.6f);
        }

        yield return new WaitForSeconds(0.5f);
        TransitionToState(BossState.Recovering);
    }

    void SpecialEscape() => StartCoroutine(SpecialEscapeAbility());

    IEnumerator SpecialEscapeAbility()
    {
        specialAbilityUsed = true;
        TransitionToState(BossState.Special);

        anim.SetTrigger("Escape");
        PlaySound(phaseTransitionSound, 0.8f);

        // Become invulnerable
        isInvulnerable = true;

        // Fade out
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Color startColor = spriteRenderer.color;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(startColor, new Color(1, 1, 1, 0), elapsed / fadeTime);
            yield return null;
        }

        spriteRenderer.enabled = false;

        // Spawn mini squirrels
        for (int i = 0; i < numberOfMiniSquirrels; i++)
        {
            float angle = i * (360f / numberOfMiniSquirrels);
            Vector2 spawnPos = (Vector2)transform.position +
                new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * 3f;

            GameObject miniSquirrel = Instantiate(miniSquirrelPrefab, spawnPos, Quaternion.identity);
            miniSquirrels.Add(miniSquirrel);

            HealthModule miniHealth = miniSquirrel.GetComponent<HealthModule>();
            if (miniHealth != null)
            {
                GameObject capturedMini = miniSquirrel;
                miniHealth.onDeath += () => OnMiniSquirrelDeath(capturedMini);
            }
        }

        // Heal over time
        float healPerTick = healthModule.GetMaxHealth() * healAmount / 3f;
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(1f);
            healthModule.Heal(healPerTick);
        }

        // Wait for mini squirrels or timeout
        float escapeTimer = 10f;
        while (miniSquirrels.Count > 0 && escapeTimer > 0)
        {
            escapeTimer -= Time.deltaTime;
            yield return null;
        }

        // Fade back in
        spriteRenderer.enabled = true;
        elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(new Color(1, 1, 1, 0), phase == 1 ? phase1Color : phase2Color, elapsed / fadeTime);
            yield return null;
        }

        isInvulnerable = false;
        anim.SetTrigger("Return");

        yield return new WaitForSeconds(0.5f);
        TransitionToState(BossState.Idle);
    }

    void OnMiniSquirrelDeath(GameObject miniSquirrel)
    {
        if (miniSquirrels.Contains(miniSquirrel))
        {
            miniSquirrels.Remove(miniSquirrel);
        }
    }

    // === DAMAGE AND EFFECTS ===

    void CreateLandingShockwave()
    {
        float shockwaveRadius = phase == 1 ? 3f : 4f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                HealthModule playerHealth = hit.GetComponent<HealthModule>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(biteDamage);

                    // Knockback player
                    Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                    if (playerRb != null)
                    {
                        Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                        playerRb.AddForce(knockDir * 10f, ForceMode2D.Impulse);
                    }
                }
            }
        }
    }

    // Animation event - called during bite animations
    public void DealBiteDamage()
    {
        float biteRange = 2.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, biteRange);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                HealthModule playerHealth = hit.GetComponent<HealthModule>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(biteDamage);
                    PlaySound(biteSound, 0.7f);
                }
            }
        }
    }

    // === EVENT HANDLERS ===

    void OnHealthChanged(float currentHealth, float maxHealth)
    {
        // Flash on hit
        if (!isInvulnerable && currentHealth < healthModule.GetMaxHealth())
        {
            StartCoroutine(FlashSprite(Color.red, flashDuration));
        }

        // Phase transition
        if (phase == 1 && currentHealth <= maxHealth * phase2HealthThreshold)
        {
            phase = 2;
            StartPhaseTransition();
        }
    }

    void StartPhaseTransition()
    {
        StopAllCoroutines();
        StartCoroutine(PhaseTransitionRoutine());
    }

    IEnumerator PhaseTransitionRoutine()
    {
        TransitionToState(BossState.PhaseTransition);
        stateTimer = 3f;

        anim.SetTrigger("PhaseTransition");
        PlaySound(phaseTransitionSound, 1f);
        rb.linearVelocity = Vector2.zero;

        isInvulnerable = true;

        // Visual effects
        if (phaseTransitionParticles) phaseTransitionParticles.Play();

        // Dramatic color shift
        float transitionTime = 2f;
        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(phase1Color, phase2Color, elapsed / transitionTime);
            yield return null;
        }

        // Brief pause
        yield return new WaitForSeconds(1f);

        isInvulnerable = false;

        // Roar and return to battle
        anim.SetTrigger("Roar");
        PlaySound(roarSound, 1f);
        CameraShake(0.5f, 0.5f);

        yield return new WaitForSeconds(0.5f);

        TransitionToState(BossState.Idle);
    }

    void OnTargetDetected(GameObject target)
    {
        if (currentState == BossState.Idle)
        {
            anim.SetTrigger("Roar");
            PlaySound(roarSound, 0.8f);
        }
    }

    void Die()
    {
        TransitionToState(BossState.Dead);
        StopAllCoroutines();

        anim.SetTrigger("Death");
        PlaySound(impactSound, 1f);

        // Disable components
        enabled = false;
        if (enemyModule != null) enemyModule.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        // Clean up
        foreach (GameObject mini in miniSquirrels)
        {
            if (mini != null) Destroy(mini);
        }
        miniSquirrels.Clear();

        if (currentRollingAcorn != null)
        {
            Destroy(currentRollingAcorn.gameObject);
        }

        // Final effects
        CameraShake(1f, 0.5f);
    }

    // === UTILITY METHODS ===

    IEnumerator FlashSprite(Color flashColor, float duration)
    {
        if (isFlashing) yield break;

        isFlashing = true;
        Color original = spriteRenderer.color;

        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = original;

        isFlashing = false;
    }

    void CameraShake(float intensity, float duration)
    {
        // This would need a CameraShake script on your camera
        // Here's a simple implementation you can call
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
           /* CameraShake shaker = mainCam.GetComponent<CameraShake>();
            if (shaker != null)
            {
                shaker.Shake(intensity, duration);
            }*/
        }
    }

    void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (healthModule != null)
        {
            healthModule.onHealthChanged -= OnHealthChanged;
            healthModule.onDeath -= Die;
        }

        if (enemyModule != null)
        {
            enemyModule.OnTargetDetected -= OnTargetDetected;
        }
    }

    // === DEBUG VISUALIZATION ===

    void OnDrawGizmosSelected()
    {
        // Draw attack ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 2.5f); // Bite range

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, phase == 1 ? 3f : 4f); // Shockwave range

        // Draw ground check
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.5f, groundCheckRadius);
    }
}