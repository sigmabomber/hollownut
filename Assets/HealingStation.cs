using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class HealingStation : MonoBehaviour
{
    private static readonly int GoDownHash = Animator.StringToHash("GoDown");
    private static readonly int GoUpHash = Animator.StringToHash("GoUp");

    [Header("References")]
    public Transform player;

    [Header("Settings")]
    public int CheckPointID = 1;
    public float distance = 0.1f;
    public float moveSpeed = 2f;
    public Vector3 defaultOffset = Vector3.zero;
    public Vector3 goDownOffset = new Vector3(0, -0.5f, 0);
    public Vector3 localRotation = Vector3.zero;

    [Header("Debug Controls")]
    public bool goDown = false;
    public bool goUp = false;

    private Transform follow;
    private Animator animator;
    private PlayerHealth plrHealth;
    private PlayerMovement move;
    private Vector3 currentOffset;
    private bool isHealing = false;
    private Coroutine currentAnimation;

    void Start()
    {
        InitializeComponents();
        currentOffset = defaultOffset;
    }

    void Update()
    {
        HandleDebugInputs();
    }

    void LateUpdate()
    {
        if (isHealing && player != null && follow != null)
        {
            player.localPosition = currentOffset;
            player.localRotation = Quaternion.Euler(localRotation);
        }
    }

    private void InitializeComponents()
    {
        if (player == null)
        {
            Debug.LogError("Player reference not set in HealingStation!");
            return;
        }

        move = player.GetComponent<PlayerMovement>();
        animator = GetComponent<Animator>();
        plrHealth = player.GetComponent<PlayerHealth>();

        follow = transform.Find("Follow");
        if (follow == null)
        {
            Debug.LogError("Follow object not found under HealingStation!");
        }
    }

    private void HandleDebugInputs()
    {
        if (goDown && currentAnimation == null)
        {
            currentAnimation = StartCoroutine(GoDownRoutine());
            goDown = false;
        }

        if (goUp && currentAnimation == null)
        {
            currentAnimation = StartCoroutine(GoUpRoutine());
            goUp = false;
        }
    }

    public IEnumerator GoDownRoutine()
    {
        if (!ValidateComponents()) yield break;

        SetPlayerMovement(false);
        yield return MovePlayerToOffset();

        PlayAnimation(GoDownHash, GoUpHash);
        yield return new WaitForSeconds(1f);

        AttachPlayerToStation();
        currentOffset = goDownOffset;
        isHealing = true;

        yield return new WaitForSeconds(1f);
        StartCoroutine(GoUpRoutine());
    }

    public IEnumerator GoUpRoutine()
    {
        if (!ValidateComponents()) yield break;

        PlayAnimation(GoUpHash, GoDownHash);
        yield return new WaitForSeconds(1f);

        yield return PerformHealingAndSave();

        PlayAnimation(GoDownHash, GoUpHash);
        yield return new WaitForSeconds(1f);

        DetachPlayerFromStation();
        SetPlayerMovement(true);

        currentAnimation = null;

        yield return new WaitForSeconds(0.5f);
        PlayAnimation(GoUpHash, GoDownHash);
    }

    private bool ValidateComponents()
    {
        if (animator == null || player == null || follow == null)
        {
            Debug.LogWarning("HealingStation: Required components are missing!");
            return false;
        }
        return true;
    }

    private void SetPlayerMovement(bool enabled)
    {
        if (move != null)
        {
            move.canAnimate = enabled;
            move.canJump = enabled;
            move.canMove = enabled;
        }
    }

    private IEnumerator MovePlayerToOffset()
    {
        Vector3 localTargetOffset = new Vector3(0.64f, -0.2f, 0f);
        Vector3 targetWorldPos = follow.TransformPoint(localTargetOffset);
        float sqrDistance = distance * distance;

        while ((player.position - targetWorldPos).sqrMagnitude > sqrDistance)
        {
            player.position = Vector3.MoveTowards(
                player.position,
                targetWorldPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        currentOffset = localTargetOffset;
    }

    private void PlayAnimation(int triggerHash, int resetHash)
    {
        animator.SetBool(resetHash, false);
        animator.SetBool(triggerHash, true);
    }

    private void AttachPlayerToStation()
    {
        player.SetParent(follow, true);
    }

    private void DetachPlayerFromStation()
    {
        player.SetParent(null);
        DontDestroyOnLoad(player);
        isHealing = false;
    }

    private IEnumerator PerformHealingAndSave()
    {
        // Heal player
        player.GetComponent<HealthModule>()?.Heal(1000);
       

        // Save game state
        Scene currentScene = SceneManager.GetActiveScene();
        GameManager.Instance.CurrentPlayer.Set("SceneName", currentScene.name);
        GameManager.Instance.CurrentPlayer.Set("Checkpoint", CheckPointID);
        GameManager.Instance.CurrentPlayer.Set("HP", 100f);

        // Show saving UI
        SavingUI.Instance.saving = true;

        // Save asynchronously
        Task saveTask = GameManager.Instance.SavePlayer();
        yield return new WaitUntil(() => saveTask.IsCompleted);

        // Complete saving process
        SavingUI.Instance.completed = true;
    }

    // Public method to trigger healing station from other scripts
    public void ActivateHealingStation()
    {
        if (currentAnimation == null)
        {
            currentAnimation = StartCoroutine(GoDownRoutine());
        }
    }

    // Clean up coroutines when disabled
    void OnDisable()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
}