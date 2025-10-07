using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
public class EnemyModule : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 5f;
    public float attackRadius = 2f;
    public LayerMask targetLayer = ~0;
    public Color detectionColor = new Color(1f, 0f, 0f, 0.3f);
    public Color attackColor = new Color(1f, 0.5f, 0f, 0.3f);
    public Sprite detectionSprite;

    [Header("Attack Settings")]
    public float attackCooldown = 1f;

    // Events
    public Action<GameObject> OnTargetDetected;
    public Action OnStartAttack;

    private bool canAttack = true;
    private GameObject target;
    private CircleCollider2D detectionCollider;
    private GameObject detectionVisual, attackVisual;

    public void Initialize(
        float cooldown,
        float detectionRange,
        float attackRange,
        LayerMask layerMask,
        Color? detectionCol = null,
        Color? attackCol = null,
        Sprite sprite = null
    )
    {
        attackCooldown = cooldown;
        detectionRadius = detectionRange;
        attackRadius = attackRange;
        targetLayer = layerMask;

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
        detectionCollider = GetComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        // Visuals (optional)
        if (detectionSprite != null)
        {
            detectionVisual = CreateVisual("DetectionCircle", detectionRadius, detectionColor);
            attackVisual = CreateVisual("AttackCircle", attackRadius, attackColor);
        }
    }

    private GameObject CreateVisual(string name, float radius, Color color)
    {
        GameObject circle = new GameObject(name);
        circle.transform.parent = transform;
        circle.transform.localPosition = Vector3.zero;
        circle.transform.localScale = Vector3.one * radius * 2f;

        var sr = circle.AddComponent<SpriteRenderer>();
        sr.sprite = detectionSprite;
        sr.color = color;
        sr.sortingOrder = 10;
        return circle;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if ((targetLayer & (1 << other.gameObject.layer)) == 0) return;

        float distance = Vector2.Distance(transform.position, other.transform.position);

        if (distance <= detectionRadius)
        {
            target = other.gameObject;
            OnTargetDetected?.Invoke(target);

            if (distance <= attackRadius)
                TryStartAttack();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject == target)
            target = null;
    }

    public void TryStartAttack()
    {
        if (!canAttack) return;

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
