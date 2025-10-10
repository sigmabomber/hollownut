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
    private int slashCombo = 1;
    // Animation hashes
    private static readonly int StartAttackHash = Animator.StringToHash("StartAttack");
    private static readonly int AttackDirectionHash = Animator.StringToHash("SlashDirection");
    private static readonly int SlashComboHash = Animator.StringToHash("SlashCombo");
  

    // Attack parameters
    private bool canAttack = true;
    private bool attacking = false;
    [SerializeField] private float attackCooldown = 0.4f;
    [SerializeField] private float attackDuration = 0.2f;

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

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        playerAnimator = GetComponent<Animator>();
        plrMovement = GetComponent<PlayerMovement>();



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

        playerAnimator.SetInteger(AttackDirectionHash, (int)direction);

        



        slashCombo = slashCombo == 1 ? 2 : 1;

        StartCoroutine(AttackDuration());
        StartCoroutine(AttackCooldown());
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

    

   

    

    public void OnHitboxTriggerEnter(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & enemyLayers) != 0)
        {
            HealthModule enemyHealth = other.GetComponent<HealthModule>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);

                Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    ApplyKnockback(enemyRb);
                }
            }
        }
    }

    void ApplyKnockback(Rigidbody2D enemyRb)
    {
        if (enemyRb != null)
        {
            Vector2 knockbackDirection = GetKnockbackDirection();
            enemyRb.AddForce(knockbackDirection * attackForce, ForceMode2D.Impulse);
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

        slashAnimator.SetInteger(SlashComboHash, slashCombo);
        playerAnimator.SetBool(StartAttackHash, attacking);
        slashAnimator.SetBool(StartAttackHash, attacking);
    }

    public bool IsAttacking() => attacking;
  

    public bool CanAttack() => canAttack;
    

    public string GetCurrentDirectionAsString() => direction.ToString();
    
}