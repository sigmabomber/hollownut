using UnityEngine;





public interface SlowedDown
{
    void SlowedDown(SlowedDownData data);
    void UndoSlow(SlowedDownData data);
}
public struct SlowedDownData
{
    public float Weight;
    public Rigidbody2D Target;

    public SlowedDownData(float weight, Rigidbody2D target)
    {
        Weight = weight;

        Target = target;
    }
}



public interface TiedUp
{

}

public struct TiedUpData
{
    public float duration;

}

public struct KnockbackData
{
    public Vector2 Direction;
    public float Force;
    public Rigidbody2D Target;

    public KnockbackData(Vector2 direction, float force, Rigidbody2D target)
    {
        Direction = direction;
        Force = force;
        Target = target;
    }
}

public interface IKnockback
{
    void KnockBack(KnockbackData data);
}


public interface OnFire
{

}

public struct OnFireData
{
    public float duration;
    public float damage;
}

public class EffectsModule : MonoBehaviour
{
    public static EffectsModule Instance { get; private set; }




    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void KnockBack(KnockbackData data)
    {

        Vector2 knockbackDirection = data.Direction;
        Rigidbody2D targetRb = data.Target;

        targetRb.AddForce(knockbackDirection * data.Force, ForceMode2D.Impulse);
    }

    public void SlowedDown(SlowedDownData data)
    {
        Rigidbody2D target = data.Target;
        if (target == null) {print("hsfaed"); return; }
        PlayerMovement playerMovement = target.GetComponent<PlayerMovement>();
        if (playerMovement == null) { print("not very sigma2"); return; }

        playerMovement.weight += data.Weight;
        print(playerMovement.weight);

        

    }

    public void UndoSlow( SlowedDownData data)
    {
        Rigidbody2D target = data.Target;
        if (target == null) { print("ggd"); return; }
        PlayerMovement playerMovement = target.GetComponent<PlayerMovement>();
        if (playerMovement == null) { print("not very sigma"); return; }
     
        playerMovement.weight -= data.Weight;



    }

}
