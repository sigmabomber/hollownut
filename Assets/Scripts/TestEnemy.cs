using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyModule))]
public class Enemy : MonoBehaviour
{
    private EnemyModule enemyModule;
    private Rigidbody2D rb;
    private Transform target;
    private bool isSticking = false;
    private bool isWaiting = false;
    private bool canStick = false;
    private bool isInAttackSequence = false;
    private bool isAvoidingPlayer = false;
    private bool isHypingUp = false;

    private Vector3 lastContactPoint;
    private Transform lastContactTarget;
    private Vector3 localStickOffset;

    public Sprite detectionSprite;
    private BoxCollider2D boxCollider;

    [Header("Attack Settings")]
    public float jumpForce = 10f;
    public float stickDuration = 0.5f;
    public float jumpWindup = 0.2f;
    public float missWaitTime = 0.5f;
    public float jumpArcHeight = 2f;

    [Header("Hype Up Jumps")]
    public int maxHypeJumps = 3;
    public float hypeJumpForce = 3f;
    public float hypeJumpArcHeight = 1f;
    public float hypeJumpInterval = 0.3f;
    public float hypeWindup = 0.1f;

    [Header("Avoidance Behavior")]
    public float avoidanceTime = 1f;
    public float avoidanceJumpForce = 6f;

    [Header("Idle Movement")]
    public float idleJumpForce = 5f;
    public float idleJumpIntervalMin = 0.5f;
    public float idleJumpIntervalMax = 1.5f;
    public float groundCheckDistance = 2f;
    public LayerMask groundLayerMask = 1; // Default layer

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        enemyModule = GetComponent<EnemyModule>();
        enemyModule.Initialize(
            cooldown: 1.5f,
            detectionRange: 5f,
            attackRange: 4f,
            layerMask: LayerMask.GetMask("Player"),
            detectionCol: new Color(0f, 1f, 0f, 0.2f),
            attackCol: new Color(1f, 0f, 0f, 0.3f),
            sprite: detectionSprite
        );

        enemyModule.OnTargetDetected += targetObj =>
        {
            target = targetObj.transform;
            if (!isWaiting && !isSticking && !isInAttackSequence && !isAvoidingPlayer && !isHypingUp)
                StartCoroutine(AttackSequence());
        };

        enemyModule.OnStartAttack += StartAttackingEnemy;

        StartCoroutine(IdleRoutine());
    }

    private void StartAttackingEnemy()
    {
        if (target == null || isSticking || isWaiting || isInAttackSequence || isAvoidingPlayer || isHypingUp) return;
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        isInAttackSequence = true;

        // Random chance to perform hype jumps (0 to maxHypeJumps)
        int numHypeJumps = Random.Range(0, maxHypeJumps + 1);


        // Perform small "hype up" jumps in place
        if (numHypeJumps > 0)
        {
            yield return StartCoroutine(PerformHypeJumps(numHypeJumps));
        }

        // Final attack jump (only if we still have a target)
        if (target != null)
        {
            yield return StartCoroutine(FinalAttackRoutine());
        }

        isInAttackSequence = false;
    }

    private IEnumerator PerformHypeJumps(int numJumps)
    {
        isHypingUp = true;

        for (int i = 0; i < numJumps; i++)
        {
            if (target == null) break;


            // Brief windup for the hype jump
            yield return new WaitForSeconds(hypeWindup);

            // Small vertical jump in place
            Vector2 force = Vector2.up * hypeJumpForce;
            Jump(force);

            // Wait for next jump
            if (i < numJumps - 1)
            {
                yield return new WaitForSeconds(hypeJumpInterval);
            }
        }

        // Brief pause after hype jumps before attacking
        yield return new WaitForSeconds(0.2f);
        isHypingUp = false;
    }

    private IEnumerator FinalAttackRoutine()
    {
        isWaiting = true;
        yield return new WaitForSeconds(jumpWindup);

        if (target == null)
        {
            isWaiting = false;
            yield break;
        }

        // Horizontal direction toward player
        Vector2 dir = target.position - transform.position;
        dir.y = 0f;
        dir.Normalize();

        // Add a small upward arc
        Vector2 force = dir * jumpForce + Vector2.up * jumpArcHeight;
        Jump(force);

        canStick = true;
        yield return new WaitForSeconds(0.6f);
        canStick = false;

        isWaiting = false;
    }

    private Vector2 GetAvoidanceDirection()
    {
        if (target == null)
        {
            // If no target, just jump randomly away but check for safe ground
            return GetSafeDirection(new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 0.8f)).normalized);
        }

        Vector2 awayFromPlayer = (transform.position - target.position).normalized;

        // Add some randomness to avoidance
        awayFromPlayer += new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(0.1f, 0.5f));
        awayFromPlayer.Normalize();

        return GetSafeDirection(awayFromPlayer);
    }

    private Vector2 GetSafeDirection(Vector2 desiredDirection)
    {
        // Check if the desired direction would lead off a platform
        Vector2 checkPosition = (Vector2)transform.position + desiredDirection * 2f;

        // Perform ground check to see if there's ground in the desired direction
        RaycastHit2D groundCheck = Physics2D.Raycast(
            transform.position,
            desiredDirection,
            2f,
            groundLayerMask
        );

        // Also check for ground below the target position
        RaycastHit2D groundBelow = Physics2D.Raycast(
            checkPosition + Vector2.up * 0.5f,
            Vector2.down,
            groundCheckDistance,
            groundLayerMask
        );

        // If no ground in desired direction or ground is too far below, find a safer direction
        if (groundCheck.collider == null || groundBelow.collider == null)
        {

            // Try alternative directions
            Vector2[] alternativeDirections = {
                new Vector2(-desiredDirection.x, desiredDirection.y), // Opposite horizontal
                new Vector2(desiredDirection.x * 0.5f, desiredDirection.y), // Slower horizontal
                new Vector2(0f, 0.5f), // Mostly upward
                new Vector2(Random.Range(-0.3f, 0.3f), 0.7f) // Random upward
            };

            // Find first safe alternative direction
            foreach (Vector2 altDir in alternativeDirections)
            {
                Vector2 altCheckPos = (Vector2)transform.position + altDir.normalized * 2f;
                RaycastHit2D altGroundCheck = Physics2D.Raycast(
                    transform.position,
                    altDir.normalized,
                    2f,
                    groundLayerMask
                );
                RaycastHit2D altGroundBelow = Physics2D.Raycast(
                    altCheckPos + Vector2.up * 0.5f,
                    Vector2.down,
                    groundCheckDistance,
                    groundLayerMask
                );

                if (altGroundCheck.collider != null && altGroundBelow.collider != null)
                {
                    return altDir.normalized;
                }
            }

            return Vector2.up;
        }

        return desiredDirection;
    }

    private IEnumerator AvoidanceBehavior()
    {
        isAvoidingPlayer = true;

        // Perform 1-2 avoidance jumps
        int numAvoidanceJumps = Random.Range(1, 3);

        for (int i = 0; i < numAvoidanceJumps; i++)
        {
            if (target == null) break;

            yield return new WaitForSeconds(0.3f);

            Vector2 avoidanceDir = GetAvoidanceDirection();
            Vector2 force = avoidanceDir * avoidanceJumpForce + Vector2.up * (jumpArcHeight * 0.7f);
            Jump(force);

            yield return new WaitForSeconds(0.4f);
        }

        // Wait a bit before considering attacking again
        yield return new WaitForSeconds(avoidanceTime);

        isAvoidingPlayer = false;
    }

    private void Jump(Vector2 force)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!canStick || isSticking) return;

        if (collision.collider.CompareTag("Player"))
        {
            ContactPoint2D contact = collision.GetContact(0);
            lastContactPoint = contact.point;
            lastContactTarget = collision.transform;

            localStickOffset = lastContactTarget.InverseTransformPoint(lastContactPoint);

            StartCoroutine(StickToPlayerAtPoint());
        }
    }

    private void FixedUpdate()
    {
        if (canStick && !isSticking && target != null)
        {
            Collider2D playerCollider = Physics2D.OverlapBox(rb.position, boxCollider.size, 0f, LayerMask.GetMask("Player"));
            if (playerCollider != null)
            {
                lastContactTarget = playerCollider.transform;
                Vector3 closestPoint = playerCollider.ClosestPoint(rb.position);
                lastContactPoint = closestPoint;
                localStickOffset = lastContactTarget.InverseTransformPoint(lastContactPoint);

                StartCoroutine(StickToPlayerAtPoint());
            }
        }

        if (isSticking && lastContactTarget != null)
        {
            Vector3 worldStickPosition = lastContactTarget.TransformPoint(localStickOffset);
            transform.position = worldStickPosition;
        }
    }

    private IEnumerator StickToPlayerAtPoint()
    {
        if (lastContactTarget == null) yield break;

        isSticking = true;
        canStick = false;

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.isKinematic = true;
        boxCollider.isTrigger = true;

        transform.SetParent(lastContactTarget);

        if (localStickOffset == Vector3.zero)
        {
            localStickOffset = lastContactTarget.InverseTransformPoint(lastContactPoint);
        }

        float stickTimer = 0f;
        while (stickTimer < stickDuration && lastContactTarget != null)
        {
            Vector3 worldStickPosition = lastContactTarget.TransformPoint(localStickOffset);
            transform.position = worldStickPosition;

            stickTimer += Time.deltaTime;
            yield return null;
        }

        if (lastContactTarget != null)
        {
            transform.SetParent(null);
        }

        rb.isKinematic = false;
        boxCollider.isTrigger = false;

        // Jump off in a direction that avoids the player and is safe
        Vector2 jumpOffDir = GetAvoidanceDirection();
        rb.AddForce(jumpOffDir * (jumpForce * 0.6f), ForceMode2D.Impulse);

        isSticking = false;
        localStickOffset = Vector3.zero;

        // Start avoidance behavior after unsticking
        StartCoroutine(AvoidanceBehavior());

        isWaiting = true;
        yield return new WaitForSeconds(0.3f);
        isWaiting = false;
    }

    private IEnumerator IdleRoutine()
    {
        while (true)
        {
            if (!isWaiting && !isSticking && target == null && !isAvoidingPlayer && !isHypingUp)
            {
                // Get a safe direction for idle movement
                Vector2 desiredDir = new Vector2(Random.Range(-1f, 1f), 0f).normalized;
                Vector2 safeDir = GetSafeDirection(desiredDir);

                Jump(safeDir * idleJumpForce + Vector2.up * (jumpArcHeight / 2f));
            }

            yield return new WaitForSeconds(Random.Range(idleJumpIntervalMin, idleJumpIntervalMax));
        }
    }

    // Visual debugging for ground checks
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // Draw ground check distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);

            // Draw current movement direction if any
            if (rb != null && rb.linearVelocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + (Vector3)rb.linearVelocity.normalized * 2f);
            }
        }
    }
}