using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private StickAttack stickAttack;

    private void Start()
    {
        stickAttack = GetComponentInParent<StickAttack>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (stickAttack != null && stickAttack.IsAttacking())
        {
            stickAttack.OnHitboxTriggerEnter(other);
        }
    }
}