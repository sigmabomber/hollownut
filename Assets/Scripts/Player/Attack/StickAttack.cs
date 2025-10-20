using NUnit.Framework;
using System.Collections;
using UnityEngine;

using System.Collections.Generic;

public class StickAttack : MonoBehaviour
{
    private KeyCode attackKey;
    private KeyCode leftKey;
    private KeyCode rightKey;
    private KeyCode upKey;
    private KeyCode downKey;

    private PlayerMovement plrMovement;
    private Rigidbody2D rb;
    private Animator playerAnimator;
    public Animator slashAnimator;
    public GameObject slashObj;

    private static readonly int StartAttackHash = Animator.StringToHash("StartAttack");
    private static readonly int AttackDirectionHash = Animator.StringToHash("SlashDirection");
    private static readonly int JumpingHash = Animator.StringToHash("Jumping");

    private bool jumping = false;
    private bool canAttack = true;
    private bool attacking = false;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDuration = 0.2f;

    [SerializeField] private GameObject attackHitEnemyVFX;
    [SerializeField] private GameObject attackHitNonEnemyVFX;

    [SerializeField] private float attackForce = 50f;
    [SerializeField] private int attackDamage = 10;

    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackWidth = 1.2f;
    [SerializeField] private float attackHeight = 0.8f;
    [SerializeField] private int numberOfRays = 5;

    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private LayerMask obstacleLayers;

    private Direction direction;
    private enum Direction { Left, Right, Up, Down }

    private bool hasHitThisAttack = false;

    private bool hasStickUnlocked = false;


    public List<string> sfx = new();

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<Animator>();
        plrMovement = GetComponent<PlayerMovement>();

        StartCoroutine(GetKeybinds());

    }


    private IEnumerator GetKeybinds()
    {
        yield return new WaitForSeconds(1f);

        GameManager.Instance.CurrentSettings.SettingsUpdated += UpdateKeybinds;
        if (GameManager.Instance?.CurrentSettings != null)
        {
            Dictionary<string, KeyCode> keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();

            attackKey = keybinds["attack"];
            leftKey = keybinds["left"];
            rightKey = keybinds["right"];
            upKey = keybinds["up"];
            downKey = keybinds["down"];

        }
        else
        {
           
            attackKey = KeyCode.X;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
        }
    }
    private void UpdateKeybinds()
    {
        if (GameManager.Instance?.CurrentSettings != null)
        {
            Dictionary<string, KeyCode> keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();

            attackKey = keybinds["attack"];
            leftKey = keybinds["left"];
            rightKey = keybinds["right"];
            upKey = keybinds["up"];
            downKey = keybinds["down"];

        }
        else
        {
         
            attackKey = KeyCode.X;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
        }
    }

    private void Update()
    {
        if(!canAttack)
        hasStickUnlocked = GameManager.Instance.CurrentPlayer.StickUnlocked;

        
        if (canAttack)
        {
            GetDirection();
            GetInput();
            slashObj.SetActive(attacking);
        }
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
        if (Input.GetKeyDown(attackKey) && !plrMovement.IsDashing()  && !plrMovement.IsQuickDropping() && canAttack)
        {
            PlaySFX();
            StartAttack();
        }
    }

    void StartAttack()
    {
        attacking = true;
        canAttack = false;
        hasHitThisAttack = false;
        jumping = plrMovement.IsJumping();
        DetectHits();
        StartCoroutine(AttackDuration());
        StartCoroutine(AttackCooldown());
    }

    void DetectHits()
    {
        Vector2 attackOrigin = GetAttackOrigin();
        Vector2 attackSize = GetAttackSize();
        Vector2 attackDirection = GetAttackDirectionVector();
        RaycastHit2D[] boxHits = Physics2D.BoxCastAll(attackOrigin, attackSize, 0f, attackDirection, 0.1f, enemyLayers | obstacleLayers);
        RaycastHit2D[] rayHits = PerformMultiRaycast(attackOrigin, attackSize, attackDirection);
        ProcessHits(boxHits);
        ProcessHits(rayHits);
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
            RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, attackRange, enemyLayers | obstacleLayers);
            allHits.AddRange(hits);
            Debug.DrawRay(rayOrigin, direction * attackRange, Color.red, 1f);
        }

        return allHits.ToArray();
    }

    void ProcessHits(RaycastHit2D[] hits)
    {
        if (hasHitThisAttack) return;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hasHitThisAttack)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                int ignoreLayer = LayerMask.NameToLayer("Ignore");
                if (hit.collider.gameObject.layer == playerLayer) continue;
                if (hit.collider.gameObject.layer == ignoreLayer) continue;

                bool isEnemy = ((1 << hit.collider.gameObject.layer) & enemyLayers) != 0;
                ProcessHit(hit.collider, hit.point, isEnemy);
                if (hasHitThisAttack) break;
            }
        }
    }

    void ProcessHit(Collider2D other, Vector2 hitPoint, bool isEnemy)
    {
        HealthModule enemyHealth = null;
        if (isEnemy)
        {
            enemyHealth = other.GetComponent<HealthModule>();
        }
        if (enemyHealth.invincible) return;
        if (isEnemy && (enemyHealth == null))
            return;

        hasHitThisAttack = true;
        PlayHitVFX(hitPoint, isEnemy);

        if (isEnemy && enemyHealth != null)
        {
            enemyHealth.TakeDamage(attackDamage);
            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                ApplyKnockback(enemyRb);
            }
        }
        else
        {
            print("Hit: " + other.name);
        }
    }

    Vector2 GetAttackOrigin()
    {
        Vector2 baseOrigin = (Vector2)transform.position;

        switch (direction)
        {
            case Direction.Left:
                return baseOrigin + Vector2.left * (attackRange * 0.5f);
            case Direction.Right:
                return baseOrigin + Vector2.right * (attackRange * 0.5f);
            case Direction.Up:
                return baseOrigin + Vector2.up * (attackRange * 0.5f);
            case Direction.Down:
                return baseOrigin + Vector2.down * (attackRange * 0.5f);
            default:
                return baseOrigin + Vector2.right * (attackRange * 0.5f);
        }
    }

    Vector2 GetAttackSize()
    {
        switch (direction)
        {
            case Direction.Left:
            case Direction.Right:
                return new Vector2(attackRange, attackHeight);
            case Direction.Up:
            case Direction.Down:
                return new Vector2(attackWidth, attackRange);
            default:
                return new Vector2(attackRange, attackHeight);
        }
    }

    Vector2 GetAttackDirectionVector()
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

    IEnumerator AttackDuration()
    {
        yield return new WaitForSeconds(attackDuration);
        attacking = false;
    }

    IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    void UpdateAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool(StartAttackHash, attacking);
            playerAnimator.SetInteger(AttackDirectionHash, (int)direction);
        }

        if (slashAnimator != null && slashObj.activeSelf == true)
        {
            slashAnimator.SetBool(StartAttackHash, attacking);
            slashAnimator.SetInteger(AttackDirectionHash, (int)direction);
            slashAnimator.SetBool(JumpingHash, jumping);
        }
    }


    void PlaySFX()
    {
       string clipName = sfx[Random.Range(0, sfx.Count)];


        SoundManager.Instance.PlaySFX(clipName);
    }
    private void OnDrawGizmosSelected()
    {
        if (attacking)
        {
            Gizmos.color = Color.red;
            Vector2 origin = GetAttackOrigin();
            Vector2 size = GetAttackSize();
            Gizmos.DrawWireCube(origin, size);
        }
    }

    public bool IsAttacking() => attacking;
    public bool CanAttack() => canAttack;
    public string GetCurrentDirectionAsString() => direction.ToString();
}
