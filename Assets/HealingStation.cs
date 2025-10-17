using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

public class HealingStation : MonoBehaviour
{
    private static readonly int GoDownHash = Animator.StringToHash("GoDown");
    private static readonly int GoUpHash = Animator.StringToHash("GoUp");

    public Transform player;
    private Transform follow;
    private Animator animator;

    public bool goUp = false;
    public bool goDown = false;
    public bool isHealing = false;
    public float distance = 0.1f;
    public Vector3 defaultOffset = Vector3.zero;
    public Vector3 goDownOffset = new Vector3(0, -0.5f, 0);
    public Vector3 localRotation = Vector3.zero;

    public float moveSpeed = 2f; // speed at which the player walks to offset

    private Vector3 currentOffset;

    void Start()
    {
        animator = GetComponent<Animator>();
        follow = transform.Find("Follow");
        if (follow == null)
        {
            Debug.LogError("Follow object not found under HealingStation!");
        }

        currentOffset = defaultOffset;
    }

    void Update()
    {
        if (goDown)
        {
          StartCoroutine(  GoDown());
            goDown = false;
        }
        if (goUp)
        {
            GoUp();
            goUp = false;
        }
    }

    void LateUpdate()
    {
        if (player != null && follow != null && isHealing)
        {
            player.localPosition = currentOffset;
            player.localRotation = Quaternion.Euler(localRotation);
        }
    }

    public IEnumerator GoDown()
    {
        if (animator == null || player == null || follow == null) yield break ;
        PlayerMovement move = player.GetComponent<PlayerMovement>();
        if (move != null)
        {
            move.canAnimate = false;
            move.canJump = false;
            move.canMove = false;
        }
        yield return  StartCoroutine(MovePlayerToOffset());
        animator.SetBool(GoDownHash, true);

        animator.SetBool(GoUpHash, false);

        yield return new WaitForSeconds(1);
        player.SetParent(follow, worldPositionStays: true);
        currentOffset = goDownOffset;
        isHealing = true;

        yield return new WaitForSeconds(1);
        GoUp();

    }

    private IEnumerator MovePlayerToOffset()
    {
        Vector3 localTargetOffset = new Vector3(0.64f, -0.2f, 0f);

        Vector3 targetWorldPos = follow.TransformPoint(localTargetOffset);

        while (Vector3.Distance(player.position, targetWorldPos) > distance)
        {
            player.position = Vector3.MoveTowards(player.position, targetWorldPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        currentOffset = localTargetOffset;
    }



    public void GoUp()
    {
        if (animator == null) return;

        animator.SetBool(GoDownHash, false);
        animator.SetBool(GoUpHash, true);

    }
}
