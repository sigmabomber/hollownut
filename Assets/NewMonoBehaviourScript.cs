using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (gameObject.name == "green")
        {
            HealthModule health = collision.gameObject.GetComponent<HealthModule>();

            if (health != null)
            {
                health.Heal(10);
            }


        }
        else
        {
            HealthModule health = collision.gameObject.GetComponent<HealthModule>();

            if (health != null)
            {
                health.TakeDamage(10);
            }
        }
    }
}
