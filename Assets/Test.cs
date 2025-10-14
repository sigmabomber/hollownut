using System.Collections;
using UnityEngine;

public class LogController : MonoBehaviour
{
    private Rigidbody2D rb;
    public bool isRolling = false;
    public float rollForce = 10f;
    public Vector2 rollDirection = new Vector2(1f, -0.5f); // Diagonal down-right
    public bool start = false;
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Log stays still until triggered
    }

    void Update()
    {
     
        if (start) 
        {
          StartCoroutine(  StartRolling());
            start = false;
        }
    }

    public IEnumerator StartRolling()
    {
        rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics
        yield return new WaitForSeconds(1);
        isRolling = true;
        rb.bodyType = RigidbodyType2D.Kinematic;

    }
}
