using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SquirrelBoss : MonoBehaviour
{
    [Header("Boss Settings")]
    public int phase = 1;
    public float phase2HealthThreshold = 0.5f;

    [Header("Movement")]
    public float runSpeed = 8f;
    public float jumpForce = 12f;
    public float groundCheckRadius = 0.3f;

    [Header("Attack Settings")]
    public int biteDamage = 2;
    public float attackCooldown = 1.5f;
    public float telegraphTime = 0.5f;
    public float biteRange = 2.5f;

    [Header("Throw Attack - Phase 1")]
    public GameObject acornProjectile;
    public int throwCount = 3;
    public float throwDelay = 0.3f;
    public float throwForce = 15f;
    public float throwArcHeight = 2f;

    [Header("Acorn Rain - Phase 2")]
    public int rainWaves = 3;
    public int acornsPerWave = 5;
    public float rainWaveDelay = 0.6f;
    public float acornFallSpeed = 12f;
    public float rainRadius = 4f;

    [Header("Rolling Acorn Attack")]
    public GameObject rollingAcornPrefab;
    [Range(1, 5)]
    public int numberOfAcorns = 1;
    public float acornSpawnDelay = 0.3f;
    public float acornHealth = 10f;
    public float acornBounceForce = 5f;
    public float delayBeforeJump = 0.8f;
    public float jumpMultiplier = 2f;
    public float dropSpeed = 30f;

    [Header("Special Escape - Phase 2")]
    public GameObject miniSquirrelPrefab;
    public int numberOfMiniSquirrels = 3;
    public float healAmount = 0.3f;
    public float escapeDuration = 15f;

    [Header("Acorn Limits")]
    public int maxAcornsInScene = 15;

    [Header("Visual & Audio")]
    public Color phase1Color = Color.white;
    public Color phase2Color = new Color(1f, 0.3f, 0.3f);
    public AudioClip roarSound;
    public AudioClip biteSound;
    public AudioClip throwSound;
    public AudioClip impactSound;
    public AudioClip phaseTransitionSound;
    public ParticleSystem phaseTransitionParticles;
    public ParticleSystem landParticles;

    // References
    private HealthModule healthModule;
    private Transform player;
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D bossCollider;

    // State
    private enum BossState { Idle, Attacking, Special, Dead, PhaseTransition }
    private BossState currentState = BossState.Idle;
    private float attackTimer;
    private bool specialAbilityUsed = false;
    private List<GameObject> miniSquirrels = new List<GameObject>();
    private List<RollingBouncingBall> activeRollingAcorns = new List<RollingBouncingBall>();

    // Acorn tracking
    private List<GameObject> activeAcorns = new List<GameObject>();

    // Ground check
    private bool isGrounded;
    private LayerMask groundLayer;
    private LayerMask playerLayer;

    // Animation hashes
    private readonly int animThrow = Animator.StringToHash("Throw");
    private readonly int animCharge = Animator.StringToHash("Charge");
    private readonly int animAirBite = Animator.StringToHash("AirBite");
    private readonly int animGroundBite = Animator.StringToHash("GroundBite");
    private readonly int animAcornRain = Animator.StringToHash("AcornRain");
    private readonly int animEscape = Animator.StringToHash("Escape");
    private readonly int animReturn = Animator.StringToHash("Return");
    private readonly int animPhaseTransition = Animator.StringToHash("PhaseTransition");
    private readonly int animDeath = Animator.StringToHash("Death");

    void Start()
    {
        healthModule = GetComponent<HealthModule>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        bossCollider = GetComponent<Collider2D>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        groundLayer = LayerMask.GetMask("Ground");
        playerLayer = LayerMask.GetMask("Player");

        healthModule.Initialize(150f);
        healthModule.onHealthChanged += OnHealthChanged;
        healthModule.onDeath += Die;

        attackTimer = attackCooldown * 0.5f; // First attack happens sooner
    }

    void Update()
    {
        if (currentState == BossState.Dead || player == null) return;

        isGrounded = CheckGrounded();
        UpdateFacing();

        if (currentState == BossState.Idle)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0)
            {
                ChooseAttack();
            }
        }
    }

    void FixedUpdate()
    {
        // Apply air resistance when not grounded and not attacking
        if (!isGrounded && currentState != BossState.Attacking && currentState != BossState.Special)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.95f, rb.linearVelocity.y);
        }
    }

    void UpdateFacing()
    {
        if (currentState != BossState.Attacking && currentState != BossState.Special && player != null)
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            if (Mathf.Abs(dir) > 0.1f)
            {
                transform.localScale = new Vector3(dir, 1, 1);
            }
        }
    }

    bool CheckGrounded()
    {
        Vector2 checkPos = transform.position + Vector3.down * 0.5f;
        bool grounded = Physics2D.OverlapCircle(checkPos, groundCheckRadius, groundLayer);

        // Visual feedback when landing
        if (grounded && !isGrounded && landParticles != null)
        {
            landParticles.Play();
        }

        return grounded;
    }

    void ChooseAttack()
    {
        if (phase == 1)
        {
            // Phase 1: More ranged attacks
            int attack = Random.Range(0, 4);
            switch (attack)
            {
                case 0: StartCoroutine(ThrowAcorns()); break;
                case 1: StartCoroutine(RunCharge()); break;
                case 2: StartCoroutine(AirBite()); break;
                case 3: StartCoroutine(GroundBite()); break;
            }
        }
        else
        {
            // Phase 2: More aggressive and varied attacks
            List<int> attacks = new List<int> { 0, 1, 2, 3 };

            if (!specialAbilityUsed && healthModule.currentHealth < healthModule.GetMaxHealth() * 0.7f)
            {
                attacks.Add(4); // Special escape
            }

            int attack = attacks[Random.Range(0, attacks.Count)];
            switch (attack)
            {
                case 0: StartCoroutine(RunCharge()); break;
                case 1: StartCoroutine(AirBite()); break;
                case 2: StartCoroutine(GroundBite()); break;
                case 3: StartCoroutine(AcornRain()); break;
                case 4: StartCoroutine(SpecialEscape()); break;
            }
        }
    }

    // === ACORN MANAGEMENT ===

    bool CanSpawnAcorn()
    {
        return activeAcorns.Count < maxAcornsInScene;
    }

    void RegisterAcorn(GameObject acorn)
    {
        if (!activeAcorns.Contains(acorn))
        {
            activeAcorns.Add(acorn);

            HealthModule acornHealth = acorn.GetComponent<HealthModule>();
            if (acornHealth != null)
            {
                acornHealth.onDeath += () => UnregisterAcorn(acorn);
            }
            else
            {
                StartCoroutine(AcornCleanupRoutine(acorn));
            }
        }
    }

    void UnregisterAcorn(GameObject acorn)
    {
        if (activeAcorns.Contains(acorn))
        {
            activeAcorns.Remove(acorn);
        }
    }

    IEnumerator AcornCleanupRoutine(GameObject acorn)
    {
        yield return new WaitForSeconds(10f);
        UnregisterAcorn(acorn);
        if (acorn != null)
            Destroy(acorn);
    }

    void CleanupOldestAcorns(int count)
    {
        for (int i = 0; i < count && activeAcorns.Count > 0; i++)
        {
            GameObject oldestAcorn = activeAcorns[0];
            if (oldestAcorn != null)
            {
                Destroy(oldestAcorn);
            }
            activeAcorns.RemoveAt(0);
        }
    }

    // === ATTACK COROUTINES ===

    IEnumerator ThrowAcorns()
    {
        currentState = BossState.Attacking;
        anim.SetTrigger(animThrow);

        yield return new WaitForSeconds(telegraphTime);

        int acornsSpawned = 0;
        for (int i = 0; i < throwCount; i++)
        {
            if (!CanSpawnAcorn())
                continue;

            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 spawnPos = transform.position + Vector3.up * 1f + (Vector3)dirToPlayer * 0.5f;

            GameObject acorn = Instantiate(acornProjectile, spawnPos, Quaternion.identity);
            RegisterAcorn(acorn);

            Rigidbody2D acornRb = acorn.GetComponent<Rigidbody2D>();
            if (acornRb != null)
            {
                // Add arc to the throw
                Vector2 throwDir = CalculateArcThrowDirection(transform.position, player.position, throwArcHeight);
                acornRb.AddForce(throwDir * throwForce, ForceMode2D.Impulse);
            }

            acornsSpawned++;
            PlaySound(throwSound, 0.6f);
            yield return new WaitForSeconds(throwDelay);
        }

        yield return new WaitForSeconds(0.3f);
        ResetToIdle();
    }

    Vector2 CalculateArcThrowDirection(Vector2 from, Vector2 to, float arcHeight)
    {
        Vector2 direction = to - from;
        float distance = direction.magnitude;
        direction.Normalize();

        // Add upward component for arc
        float arcFactor = Mathf.Clamp01(distance / 8f);
        return new Vector2(direction.x, direction.y + arcHeight * arcFactor).normalized;
    }

    IEnumerator RunCharge()
    {
        currentState = BossState.Attacking;
        anim.SetTrigger(animCharge);

        yield return new WaitForSeconds(telegraphTime);

        Vector2 targetPos = player.position;
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        transform.localScale = new Vector3(Mathf.Sign(direction.x), 1, 1);

        float chargeTime = 0.8f;
        float elapsed = 0f;

        while (elapsed < chargeTime)
        {
            rb.linearVelocity = direction * runSpeed * 1.5f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(0.5f);
        ResetToIdle();
    }

    IEnumerator AirBite()
    {
        currentState = BossState.Attacking;
        anim.SetTrigger(animAirBite);

        yield return new WaitForSeconds(telegraphTime);

        // Jump toward player with prediction
        Vector2 predictedPlayerPos = (Vector2)player.position + (Vector2)player.GetComponent<Rigidbody2D>().linearVelocity * 0.3f;
        Vector2 jumpDir = ((predictedPlayerPos - (Vector2)transform.position) + Vector2.up * 2f).normalized;

        rb.AddForce(jumpDir * jumpForce * 1.2f, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.4f);

        PlaySound(biteSound, 0.7f);

        yield return new WaitUntil(() => isGrounded);
        yield return new WaitForSeconds(0.3f);
        ResetToIdle();
    }

    IEnumerator GroundBite()
    {
        currentState = BossState.Attacking;
        anim.SetTrigger(animGroundBite);

        yield return new WaitForSeconds(telegraphTime);

        Vector2 hopDir = new Vector2(
            Mathf.Sign(player.position.x - transform.position.x),
            0.5f
        ).normalized;

        rb.AddForce(hopDir * jumpForce * 0.7f, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.4f);

        PlaySound(biteSound, 0.7f);
        yield return new WaitForSeconds(0.5f);
        ResetToIdle();
    }

    IEnumerator AcornRain()
    {
        currentState = BossState.Attacking;
        anim.SetTrigger(animAcornRain);

        // Jump back for safety
        Vector2 retreatDir = new Vector2(
            -Mathf.Sign(player.position.x - transform.position.x),
            1f
        ).normalized;
        rb.AddForce(retreatDir * jumpForce * 0.8f, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.6f);

        for (int wave = 0; wave < rainWaves; wave++)
        {
            Vector2 targetArea = player.position + (Vector3)player.GetComponent<Rigidbody2D>().linearVelocity * 0.4f;
            int acornsThisWave = 0;

            for (int i = 0; i < acornsPerWave; i++)
            {
                if (!CanSpawnAcorn())
                {
                    CleanupOldestAcorns(1);
                }

                float angle = Random.Range(0f, 360f);
                float radius = Random.Range(0f, rainRadius);
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                ) * radius;

                Vector2 spawnPos = targetArea + offset + Vector2.up * 8f;
                GameObject acorn = Instantiate(acornProjectile, spawnPos, Quaternion.identity);
                RegisterAcorn(acorn);

                Rigidbody2D acornRb = acorn.GetComponent<Rigidbody2D>();
                if (acornRb)
                {
                    acornRb.linearVelocity = Vector2.down * acornFallSpeed;
                    acornRb.gravityScale = 0.3f;
                }

                acornsThisWave++;
            }

            PlaySound(throwSound, 0.5f);
            yield return new WaitForSeconds(rainWaveDelay);
        }

        yield return new WaitForSeconds(0.5f);
        ResetToIdle();
    }

    IEnumerator SpecialEscape()
    {
        specialAbilityUsed = true;
        currentState = BossState.Special;

        anim.SetTrigger(animEscape);
        PlaySound(roarSound, 0.8f);

        // Spawn rolling acorns
        StartCoroutine(SpawnRollingAcorns());

        // Fade out with particles
        if (phaseTransitionParticles != null)
            phaseTransitionParticles.Play();

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
        bossCollider.enabled = false;

        // Spawn mini squirrels in a circle
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

        // Wait for mini squirrels to die or timeout
        float escapeTimer = escapeDuration;
        while (miniSquirrels.Count > 0 && escapeTimer > 0)
        {
            escapeTimer -= Time.deltaTime;
            yield return null;
        }

        // Cleanup remaining mini squirrels
        foreach (GameObject mini in miniSquirrels.ToArray())
        {
            if (mini != null)
                Destroy(mini);
        }
        miniSquirrels.Clear();

        // Fade back in
        spriteRenderer.enabled = true;
        bossCollider.enabled = true;
        elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(new Color(1, 1, 1, 0), phase2Color, elapsed / fadeTime);
            yield return null;
        }

        anim.SetTrigger(animReturn);
        yield return new WaitForSeconds(0.8f);
        ResetToIdle();
    }

    IEnumerator SpawnRollingAcorns()
    {
        int availableSlots = maxAcornsInScene - activeAcorns.Count;
        int acornsToSpawn = Mathf.Min(numberOfAcorns, availableSlots);

        if (acornsToSpawn <= 0)
        {
            CleanupOldestAcorns(numberOfAcorns);
            acornsToSpawn = Mathf.Min(numberOfAcorns, maxAcornsInScene);
        }

        for (int acornIndex = 0; acornIndex < acornsToSpawn; acornIndex++)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1f;

            if (acornsToSpawn > 1)
            {
                float spreadX = (acornIndex - (acornsToSpawn - 1) / 2f) * 1.5f;
                spawnPos += Vector3.right * spreadX;
            }

            GameObject acornObj = Instantiate(rollingAcornPrefab, spawnPos, Quaternion.identity);
            RegisterAcorn(acornObj);

            RollingBouncingBall rollingAcorn = acornObj.GetComponent<RollingBouncingBall>();
            if (rollingAcorn != null)
            {
                activeRollingAcorns.Add(rollingAcorn);
                rollingAcorn.StartRolling();

                HealthModule acornHealthModule = acornObj.GetComponent<HealthModule>();
                if (acornHealthModule != null)
                {
                    acornHealthModule.Initialize(acornHealth);
                    RollingBouncingBall capturedAcorn = rollingAcorn;
                    acornHealthModule.onDeath += () => RemoveAcornFromList(capturedAcorn);
                }
            }

            if (acornIndex < acornsToSpawn - 1)
            {
                yield return new WaitForSeconds(acornSpawnDelay);
            }
        }

        yield return new WaitForSeconds(delayBeforeJump);

        // Jump and slam
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(Vector2.up * jumpForce * jumpMultiplier, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.5f);

        rb.linearVelocity = new Vector2(0, -dropSpeed);
        yield return new WaitUntil(() => isGrounded);

        // Impact effect
        PlaySound(impactSound, 0.8f);
        if (landParticles != null)
            landParticles.Play();

        // Make all acorns bounce
        foreach (RollingBouncingBall acorn in activeRollingAcorns.ToArray())
        {
            if (acorn != null)
            {
                acorn.StartBouncing(acornBounceForce);
            }
        }
    }

    void RemoveAcornFromList(RollingBouncingBall acorn)
    {
        if (activeRollingAcorns.Contains(acorn))
        {
            activeRollingAcorns.Remove(acorn);
        }
        UnregisterAcorn(acorn.gameObject);
    }

    void OnMiniSquirrelDeath(GameObject miniSquirrel)
    {
        miniSquirrels.Remove(miniSquirrel);
    }

    void ResetToIdle()
    {
        currentState = BossState.Idle;
        attackTimer = attackCooldown;
    }

    // === ANIMATION EVENTS ===

    public void DealBiteDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, biteRange, playerLayer);

        foreach (Collider2D hit in hits)
        {
            HealthModule playerHealth = hit.GetComponent<HealthModule>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(biteDamage);

                // Knockback
                Rigidbody2D playerRb = hit.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    playerRb.AddForce(knockbackDir * 5f, ForceMode2D.Impulse);
                }
            }
        }
    }

    // === EVENTS ===

    void OnHealthChanged(float currentHealth, float maxHealth)
    {
        if (phase == 1 && currentHealth <= maxHealth * phase2HealthThreshold)
        {
            phase = 2;
            StartCoroutine(PhaseTransition());
        }
    }

    IEnumerator PhaseTransition()
    {
        currentState = BossState.PhaseTransition;
        rb.linearVelocity = Vector2.zero;

        anim.SetTrigger(animPhaseTransition);
        PlaySound(phaseTransitionSound, 1f);

        if (phaseTransitionParticles != null)
            phaseTransitionParticles.Play();

        // Color and size shift
        float transitionTime = 2f;
        float elapsed = 0f;
        Vector3 originalScale = transform.localScale;

        while (elapsed < transitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionTime;

            spriteRenderer.color = Color.Lerp(phase1Color, phase2Color, t);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.2f, t);

            yield return null;
        }

        // Shockwave effect
        PlaySound(roarSound, 1.2f);
        yield return new WaitForSeconds(1f);

        currentState = BossState.Idle;
        attackTimer = attackCooldown * 0.5f;
    }

    void Die()
    {
        currentState = BossState.Dead;
        StopAllCoroutines();

        anim.SetTrigger(animDeath);
        PlaySound(roarSound, 1f);

        enabled = false;
        rb.linearVelocity = Vector2.zero;
        bossCollider.enabled = false;

        // Cleanup all spawned objects
        foreach (GameObject mini in miniSquirrels.ToArray())
        {
            if (mini != null) Destroy(mini);
        }
        miniSquirrels.Clear();

        foreach (RollingBouncingBall acorn in activeRollingAcorns.ToArray())
        {
            if (acorn != null) Destroy(acorn.gameObject);
        }
        activeRollingAcorns.Clear();

        foreach (GameObject acorn in activeAcorns.ToArray())
        {
            if (acorn != null) Destroy(acorn);
        }
        activeAcorns.Clear();
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
        if (healthModule != null)
        {
            healthModule.onHealthChanged -= OnHealthChanged;
            healthModule.onDeath -= Die;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, biteRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.5f, groundCheckRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rainRadius);
    }
}