using UnityEngine;
using System.Collections;

public class RootTrap : MonoBehaviour
{
    [Header("Root Trap Settings")]
    public float emergeTime = 0.8f;
    public float activeTime = 2f;
    public float retractTime = 0.5f;
    public int damage = 15;
    public float immobilizeDuration = 1.5f;

    [Header("Visual References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public GameObject telegraphIndicator;
    public ParticleSystem emergeParticles;
    public ParticleSystem retractParticles;

    [Header("Collision")]
    public Collider2D damageCollider;
    public Collider2D triggerCollider;

    private bool isActive = false;
    private bool hasTriggered = false;
    private Transform playerTarget;

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (damageCollider == null) damageCollider = GetComponent<Collider2D>();

        if (damageCollider != null) damageCollider.enabled = false;
        if (triggerCollider != null) triggerCollider.enabled = false;

        spriteRenderer.enabled = false;
        if (telegraphIndicator != null) telegraphIndicator.SetActive(false);
    }

    public void Activate()
    {
        if (!hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(RootTrapSequence());
        }
    }

    public void ActivateAtPosition(Vector2 position, Transform player = null)
    {
        transform.position = position;
        playerTarget = player;
        Activate();
    }

    IEnumerator RootTrapSequence()
    {
        yield return StartCoroutine(TelegraphPhase());
        yield return StartCoroutine(EmergePhase());
        yield return StartCoroutine(ActivePhase());
        yield return StartCoroutine(RetractPhase());
        Destroy(gameObject, 1f);
    }

    IEnumerator TelegraphPhase()
    {
        if (telegraphIndicator != null)
        {
            telegraphIndicator.SetActive(true);
            float telegraphDuration = emergeTime * 0.7f;
            float elapsed = 0f;
            Vector3 originalScale = telegraphIndicator.transform.localScale;

            while (elapsed < telegraphDuration)
            {
                float pulse = Mathf.PingPong(elapsed * 5f, 0.3f) + 0.7f;
                telegraphIndicator.transform.localScale = originalScale * pulse;
                elapsed += Time.deltaTime;
                yield return null;
            }

            telegraphIndicator.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(emergeTime * 0.7f);
        }
    }

    IEnumerator EmergePhase()
    {
        spriteRenderer.enabled = true;

        if (emergeParticles != null)
            emergeParticles.Play();

        if (animator != null)
        {
            animator.SetTrigger("Emerge");
        }
        else
        {
            Vector3 targetScale = transform.localScale;
            transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < emergeTime)
            {
                transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / emergeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = targetScale;
        }

        if (damageCollider != null) damageCollider.enabled = true;
        isActive = true;

        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator ActivePhase()
    {
        float elapsed = 0f;

        while (elapsed < activeTime)
        {
            if (playerTarget != null)
            {
                transform.position = Vector2.Lerp(transform.position, playerTarget.position, Time.deltaTime * 0.3f);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator RetractPhase()
    {
        isActive = false;

        if (damageCollider != null) damageCollider.enabled = false;

        if (retractParticles != null)
            retractParticles.Play();

        if (animator != null)
        {
            animator.SetTrigger("Retract");
            yield return new WaitForSeconds(retractTime);
        }
        else
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < retractTime)
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, elapsed / retractTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = Vector3.zero;
        }

        spriteRenderer.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            PlayerHit(other.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isActive && collision.gameObject.CompareTag("Player"))
        {
            PlayerHit(collision.gameObject);
        }
    }

    void PlayerHit(GameObject player)
    {
        HealthModule playerHealth = player.GetComponent<HealthModule>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage, transform.position);
        }

        PlayerMovement playerController = player.GetComponent<PlayerMovement>();
        if (playerController != null)
        {
            StartCoroutine(ImmobilizePlayer(playerController));
        }

        PlayHitEffect();
    }

    IEnumerator ImmobilizePlayer(PlayerMovement playerController)
    {
        if (playerController != null)
        {
            bool couldMove = playerController.canMove;
            bool couldJump = playerController.canJump;

            playerController.canMove = false;
            playerController.canJump = false;

            SpriteRenderer playerRenderer = playerController.GetComponent<SpriteRenderer>();
            Color originalColor = playerRenderer.color;
            playerRenderer.color = new Color(0.5f, 0.3f, 0.1f, 1f);

            yield return new WaitForSeconds(immobilizeDuration);

            if (playerController != null)
            {
                playerController.canMove = couldMove;
                playerController.canJump = couldJump;
                playerRenderer.color = originalColor;
            }
        }
    }

    void PlayHitEffect()
    {
        CameraController cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.ShakeCamera(0.2f, 0.1f);
        }
    }

    [Header("Debug")]
    public bool showDebugGizmos = true;

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (isActive)
        {
            Gizmos.color = Color.red;
            if (damageCollider != null)
            {
                if (damageCollider is CircleCollider2D circleCollider)
                {
                    Gizmos.DrawWireSphere(transform.position, circleCollider.radius);
                }
                else if (damageCollider is BoxCollider2D boxCollider)
                {
                    Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
                }
            }
        }
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    public void SetImmobilizeDuration(float duration)
    {
        immobilizeDuration = duration;
    }

    public void ForceRetract()
    {
        if (isActive && hasTriggered)
        {
            StopAllCoroutines();
            StartCoroutine(RetractPhase());
        }
    }
}
