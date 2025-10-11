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
        // Create detection collider object
        detectionColliderObject = new GameObject("DetectionCollider");
        detectionColliderObject.transform.parent = transform;
        detectionColliderObject.transform.localPosition = Vector3.zero;
     
        detectionCollider = detectionColliderObject.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        // Set ignore layer for detection collider
        if (ignoreLayer != 0)
        {
            detectionColliderObject.layer = (int)Mathf.Log(ignoreLayer.value, 2);
        }

        // Create attack collider object
        attackColliderObject = new GameObject("AttackCollider");
        attackColliderObject.transform.parent = transform;
        attackColliderObject.transform.localPosition = Vector3.zero;
        // Set ignore layer for detection collider
        if (ignoreLayer != 0)
        {
            attackColliderObject.layer = (int)Mathf.Log(ignoreLayer.value, 2);
        }

        attackCollider = attackColliderObject.AddComponent<CircleCollider2D>();
        attackCollider.isTrigger = true;
        attackCollider.radius = attackRadius;
        attackCollider.enabled = false;

       
    }

    private GameObject CreateVisual(string name, float radius, Color color)
    {
        GameObject circle = new GameObject(name);
        circle.transform.parent = transform;
        circle.transform.localPosition = Vector3.zero;
        circle.transform.localScale = Vector3.one * radius * 2f;

        // Set visual to ignore layer
        if (ignoreLayer != 0)
        {
            circle.layer = (int)Mathf.Log(ignoreLayer.value, 2);
        }

        var sr = circle.AddComponent<SpriteRenderer>();
        sr.sprite = detectionSprite;
        sr.color = color;
        sr.sortingOrder = 10;

        return circle;
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
        // Check if object is in target layer
        if ((targetLayer & (1 << other.gameObject.layer)) == 0) return;

        // Determine which collider was triggered
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

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == target)
        {
            target = null;
            // Disable attack collider when target leaves detection
            attackCollider.enabled = false;
        }
    }

    private void Update()
    {
        // Enable attack collider only when we have a target
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