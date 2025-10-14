using UnityEngine;

public class SimpleRepeater : MonoBehaviour
{
    public GameObject hillPrefab;
    public int numberOfHills = 3;
    public float scrollSpeed = 2f;

    private GameObject[] hills;
    private float hillWidth;

    void Start()
    {
        // Create array to store hill instances
        hills = new GameObject[numberOfHills];

        // Get the width of the hill sprite
        hillWidth = hillPrefab.GetComponent<SpriteRenderer>().bounds.size.x;

        // Create initial hills
        for (int i = 0; i < numberOfHills; i++)
        {
            hills[i] = Instantiate(hillPrefab, transform);
            hills[i].transform.position = new Vector3(i * hillWidth, 0, 0);
        }
    }

    void Update()
    {
        // Move all hills to the left
        for (int i = 0; i < numberOfHills; i++)
        {
            hills[i].transform.Translate(Vector3.left * scrollSpeed * Time.deltaTime);
        }

        // Check if first hill is completely off-screen
        if (hills[0].transform.position.x < -hillWidth)
        {
            // Move first hill to the end
            Vector3 newPos = hills[0].transform.position;
            newPos.x += hillWidth * numberOfHills;
            hills[0].transform.position = newPos;

            // Rearrange array
            GameObject temp = hills[0];
            for (int i = 0; i < numberOfHills - 1; i++)
            {
                hills[i] = hills[i + 1];
            }
            hills[numberOfHills - 1] = temp;
        }
    }
}