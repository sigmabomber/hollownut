using System.Collections;
using UnityEngine;

public class StickAttack : MonoBehaviour
{
    private KeyCode attackKey = Constants.PlayerData.PlayerControls.attack;
    private KeyCode leftKey = Constants.PlayerData.PlayerControls.left;
    private KeyCode rightKey = Constants.PlayerData.PlayerControls.right;
    private KeyCode upKey = Constants.PlayerData.PlayerControls.up;
    private KeyCode downKey = Constants.PlayerData.PlayerControls.down;

    private PlayerMovement plrMovement;
    private Rigidbody2D rb;
    private Animator playerAnimator;
    public Animator slashAnimator;
    public GameObject slashObj;
    // Animation hashes
    private static readonly int StartAttackHash = Animator.StringToHash("StartAttack");
    private static readonly int AttackDirectionHash = Animator.StringToHash("SlashDirection");
    private static readonly int SlashComboHash = Animator.StringToHash("SlashCombo");


    // Attack parameters
    private bool canAttack = true;
    private bool attacking = false;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDuration = 0.2f;

    [SerializeField] private GameObject attackHitEnemyVFX;
    [SerializeField] private GameObject attackHitNonEnemyVFX;

    // Attack force and damage
    [SerializeField] private float attackForce = 5f;
    [SerializeField] private int attackDamage = 1;

    // Hitbox GameObjects
    [SerializeField] private GameObject sideHitbox;
    [SerializeField] private GameObject upHitbox;
    [SerializeField] private GameObject downHitbox;

    // Layer masks
    [SerializeField] private LayerMask enemyLayers;

    private Direction direction;
    private enum Direction { Left, Right, Up, Down }

    // Track only one hit per attack with priority
    private Collider2D primaryHitThisAttack;
    private bool hasHitThisAttack = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        plrMovement = GetComponent<PlayerMovement>();

        // Deactivate all hitboxes at start
        DeactivateAllHitboxes();
    }


    private void Update()
    {
        GetDirection();
        GetInput();

        slashObj.SetActive(attacking);
    }

    private void FixedUpdate()
    {
        UpdateAnimation();
    }

    void GetDirection()
    {
        if (!attacking)
        {
            if (Input.GetKey(leftKey)) direction = Direction.Left;
            else if (Input.GetKey(rightKey)) direction = Direction.Right;
            else if (Input.GetKey(upKey)) direction = Direction.Up;
            else if (Input.GetKey(downKey)) direction = Direction.Down;
        }
    }

    void GetInput()
    {
        if (Input.GetKeyDown(attackKey) && !plrMovement.IsDashing() && !plrMovement.IsWallSliding() && !plrMovement.IsQuickDropping() && canAttack)
        {
            StartAttack();
        }
    }


    void StartAttack()
    {
        attacking = true;
        canAttack = false;
        hasHitThisAttack = false;
        primaryHitThisAttack = null;

        // Activate the appropriate hitbox based on direction
        ActivateHitboxBasedOnDirection();

        StartCoroutine(AttackDuration());
        StartCoroutine(AttackCooldown());
    }

    void ActivateHitboxBasedOnDirection()
    {
        // Deactivate all hitboxes first
        DeactivateAllHitboxes();

        // Activate only the relevant hitbox based on direction
        switch (direction)
        {
            case Direction.Left:
            case Direction.Right:
                // Directions 0 (Left) and 1 (Right) use side hitbox
                if (sideHitbox != null) sideHitbox.SetActive(true);
                break;
            case Direction.Up:
                // Direction 2 (Up) uses up hitbox
                if (upHitbox != null) upHitbox.SetActive(true);
                break;
            case Direction.Down:
                // Direction 3 (Down) uses down hitbox
                if (downHitbox != null) downHitbox.SetActive(true);
                break;
        }
    }

    void DeactivateAllHitboxes()
    {
        if (sideHitbox != null) sideHitbox.SetActive(false);
        if (upHitbox != null) upHitbox.SetActive(false);
        if (downHitbox != null) downHitbox.SetActive(false);
    }

    IEnumerator AttackDuration()
    {
        yield return new WaitForSeconds(attackDuration);
        attacking = false;

        DeactivateAllHitboxes();
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    public void OnHitboxTriggerEnter(Collider2D other)
    {
        // Skip if we've already processed a hit this attack
        if (hasHitThisAttack) return;

        // Skip player layer
        int playerLayer = LayerMask.NameToLayer("Player");
        int ignoreLayer = LayerMask.NameToLayer("Ignore");
        if (other.gameObject.layer == playerLayer) return;
        if (other.gameObject.layer == ignoreLayer) return;

        bool isEnemy = ((1 << other.gameObject.layer) & enemyLayers) != 0;

        // Priority system: If we haven't hit anything yet, OR if this is an enemy and our current hit isn't
        if (!hasHitThisAttack || (isEnemy && primaryHitThisAttack != null && !IsEnemy(primaryHitThisAttack)))
        {
            ProcessHit(other, isEnemy);
        }
    }

    void ProcessHit(Collider2D other, bool isEnemy)
    {
        HealthModule enemyHealth = null;
        if (isEnemy)
        {
            enemyHealth = other.GetComponent<HealthModule>();
        }

        // Only check CanBeHit if this is an enemy with a HealthModule
        if (isEnemy && (enemyHealth == null || !enemyHealth.CanBeHit()))
            return;

        hasHitThisAttack = true;
        primaryHitThisAttack = other;

        // Get the contact point between hitbox and enemy for accurate VFX placement
        Vector2 contactPoint = GetContactPoint(other);

        PlayHitVFX(contactPoint, isEnemy);

        // Only process damage and knockback for enemies
        if (isEnemy && enemyHealth != null)
        {
            enemyHealth.TakeDamage(attackDamage);

            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                ApplyKnockback(enemyRb);
            }
        }
    }

    bool IsEnemy(Collider2D collider)
    {
        return ((1 << collider.gameObject.layer) & enemyLayers) != 0;
    }

    Vector2 GetContactPoint(Collider2D other)
    {
        // Get the active hitbox
        GameObject activeHitbox = GetActiveHitbox();
        if (activeHitbox == null) return other.bounds.center;

        // Get the hitbox collider
        Collider2D hitboxCollider = activeHitbox.GetComponent<Collider2D>();
        if (hitboxCollider == null) return other.bounds.center;

        // Find the closest point on the enemy to the hitbox
        Vector2 closestPointOnEnemy = other.ClosestPoint(hitboxCollider.bounds.center);

        // Find the closest point on the hitbox to that enemy point
        Vector2 closestPointOnHitbox = hitboxCollider.ClosestPoint(closestPointOnEnemy);

        // Return the midpoint between them (the contact area)
        return (closestPointOnEnemy + closestPointOnHitbox) * 0.5f;
    }

    GameObject GetActiveHitbox()
    {
        if (sideHitbox != null && sideHitbox.activeInHierarchy) return sideHitbox;
        if (upHitbox != null && upHitbox.activeInHierarchy) return upHitbox;
        if (downHitbox != null && downHitbox.activeInHierarchy) return downHitbox;
        return null;
    }

    void PlayHitVFX(Vector2 position, bool isEnemy)
    {
        GameObject vfxPrefab = isEnemy ? attackHitEnemyVFX : attackHitNonEnemyVFX;

        if (vfxPrefab != null)
        {
            Quaternion rotation = GetVFXRotation();
            Instantiate(vfxPrefab, position, rotation);
        }
    }

    Quaternion GetVFXRotation()
    {
        float randomAngle = Random.Range(-15f, 15f);

        switch (direction)
        {
            case Direction.Left: return Quaternion.Euler(0, 0, 180f + randomAngle);
            case Direction.Right: return Quaternion.Euler(0, 0, 0f + randomAngle);
            case Direction.Up: return Quaternion.Euler(0, 0, 90f + randomAngle);
            case Direction.Down: return Quaternion.Euler(0, 0, 270f + randomAngle);
            default: return Quaternion.Euler(0, 0, randomAngle);
        }
    }

    void ApplyKnockback(Rigidbody2D enemyRb)
    {
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = GetKnockbackDirection();
            EffectsModule.Instance.KnockBack(new KnockbackData(knockbackDirection, attackForce, enemyRb));

        }
    }

    Vector2 GetKnockbackDirection()
    {
        switch (direction)
        {
            case Direction.Left: return Vector2.left;
            case Direction.Right: return Vector2.right;
            case Direction.Up: return Vector2.up;
            case Direction.Down: return Vector2.down;
            default: return Vector2.right;
        }
    }

    void UpdateAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(StartAttackHash, attacking);
            playerAnimator.SetInteger(AttackDirectionHash, (int)direction);
        }

        if (slashAnimator != null && slashObj.activeSelf == true )
        {
            slashAnimator.SetBool(StartAttackHash, attacking);
            slashAnimator.SetInteger(AttackDirectionHash, (int)direction);
        }


    }

    public bool IsAttacking() => attacking;
    public bool CanAttack() => canAttack;
    public string GetCurrentDirectionAsString() => direction.ToString();
}