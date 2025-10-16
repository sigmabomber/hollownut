using UnityEngine;
using System;
using System.Collections;

public class EnemyModule : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public float attackRadius = 2f;
    public LayerMask targetLayer = ~0;
    public LayerMask ignoreLayer = 0;
    public Color detectionColor = new Color(1f, 0f, 0f, 0.3f);
    public Color attackColor = new Color(1f, 0.5f, 0f, 0.3f);
    public Sprite detectionSprite;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;

    public Action<GameObject> OnTargetDetected;
    public Action OnStartAttack;

    private bool canAttack = true;
    public GameObject target;
    private GameObject detectionColliderObject, attackColliderObject;
    private CircleCollider2D detectionCollider, attackCollider;

    public void Initialize(
        float cooldown,
        float detectionRange,
        float attackRange,
        LayerMask layerMask,
        LayerMask ignoreMask = default,
        Color? detectionCol = null,
        Color? attackCol = null,
        Sprite sprite = null
    )
    {
        attackCooldown = cooldown;
        detectionRadius = detectionRange;
        attackRadius = attackRange;
        targetLayer = layerMask;
        ignoreLayer = ignoreMask;

        if (detectionCol.HasValue) detectionColor = detectionCol.Value;
        if (attackCol.HasValue) attackColor = attackCol.Value;
        if (sprite != null) detectionSprite = sprite;

        SetupDetection();
    }

    private void Awake()
    {
        if (detectionCollider == null) SetupDetection();
    }

    private void SetupDetection()
    {
        detectionColliderObject = new GameObject("DetectionCollider");
        detectionColliderObject.transform.parent = transform;
        detectionColliderObject.transform.localPosition = Vector3.zero;
     
        detectionCollider = detectionColliderObject.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        if (ignoreLayer != 0)
        {
            detectionColliderObject.layer = (int)Mathf.Log(ignoreLayer.value, 2);
        }

        attackColliderObject = new GameObject("AttackCollider");
        attackColliderObject.transform.parent = transform;
        attackColliderObject.transform.localPosition = Vector3.zero;
        
        if (ignoreLayer != 0)
        {
            attackColliderObject.layer = (int)Mathf.Log(ignoreLayer.value, 2);
        }

        attackCollider = attackColliderObject.AddComponent<CircleCollider2D>();
        attackCollider.isTrigger = true;
        attackCollider.radius = attackRadius;
        attackCollider.enabled = false;

       
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider2D other)
    {
        if ((targetLayer & (1 << other.gameObject.layer)) == 0) return;

        bool isDetectionCollider = other.IsTouching(detectionCollider);
        bool isAttackCollider = other.IsTouching(attackCollider);

        if (isDetectionCollider)
        {
            target = other.gameObject;
            OnTargetDetected?.Invoke(target);
        }

        if (isAttackCollider && target != null)
        {
            TryStartAttack();
        }
    }


    public bool IsTargetInRange(Transform enemy, Transform player)
    {

        float distanceToCollision = Vector2.Distance(enemy.position, player.position);

        return distanceToCollision < 1.5f;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        // Do not clear target when leaving range
        // Only disable attack collider if needed
        if (other.gameObject == target)
        {
            attackCollider.enabled = false;
            // target remains set
        }
    }

    private void Update()
    {
        if (target != null)
        {
            attackCollider.enabled = true;
        }
        else
        {
            attackCollider.enabled = false;
        }
    }

    public void TryStartAttack()
    {
        if (!canAttack || target == null) return;

        canAttack = false;
        OnStartAttack?.Invoke();
        StartCoroutine(AttackCooldownRoutine());
    }

    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}