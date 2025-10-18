using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System;

public class HealingStation : MonoBehaviour
{
    private static readonly int GoDownHash = Animator.StringToHash("GoDown");
    private static readonly int GoUpHash = Animator.StringToHash("GoUp");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
  

    [Header("References")]
    public Transform player;

    [Header("Settings")]
    public int CheckPointID = 1;
    public float distance = 0.1f;
    public float moveSpeed = 2f;
    public Vector3 defaultOffset = Vector3.zero;
    public Vector3 goDownOffset = new Vector3(0, -0.5f, 0);
    public Vector3 localRotation = Vector3.zero;



    private Transform follow;
    private Animator animator;
    private PlayerHealth plrHealth;
    private PlayerMovement move;
    private Animator plrAnimator;
    private Vector3 currentOffset;
    private bool isHealing = false;
    private Coroutine currentAnimation;

    public LayerMask playerLayer;

    public GameObject interactionUI;
    private BaseUI baseUI;
    public bool canInteract = false;

    private bool isInteracting = false;

    void Start()
    {
        InitializeComponents();
        currentOffset = defaultOffset;

        if (interactionUI != null)
        {

            baseUI = interactionUI.GetComponent<BaseUI>();
        }
    }


    private void Update()
    {
        if (canInteract && !isInteracting)
        {

            var keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();
         if (Input.GetKey(keybinds["interact"]))
            {
                isInteracting = true;
                StartCoroutine(GoDownRoutine());

                UIManager.Instance.CloseUI(baseUI);
            }

        }
    }


    void LateUpdate()
    {
        if (isHealing && player != null && follow != null)
        {
            player.localPosition = currentOffset;
            player.localRotation = Quaternion.Euler(localRotation);
        }
    }


    private bool CanInteract(GameObject collidingObject)
    {
        if ((playerLayer.value & (1 << collidingObject.layer)) == 0) return false;
        

        int currentCheckPointID = GameManager.Instance.CurrentPlayer.Get<int>("Checkpoint");

       
        

        return true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!CanInteract(collision.gameObject))
        {
          
            return;
        }

        UIManager.Instance.OpenUI(baseUI);

        canInteract = true;

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!canInteract) return;
        if (!CanInteract(collision.gameObject))
        {

            return;
        }

        UIManager.Instance.CloseUI(baseUI);

        canInteract = false;
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
        plrAnimator = player.GetComponent<Animator>();
        follow = transform.Find("Follow");
        if (follow == null)
        {
            Debug.LogError("Follow object not found under HealingStation!");
        }
    }

 

    public IEnumerator GoDownRoutine()
    {
        if (!ValidateComponents()) yield break;

        yield return MovePlayerToOffset();

        SetPlayerMovement(false);
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

        isInteracting = false;
        canInteract = false;
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

        while (Mathf.Abs(player.position.x - targetWorldPos.x) > distance)
        {
            float newX = Mathf.MoveTowards(player.position.x, targetWorldPos.x, moveSpeed * Time.deltaTime);
            player.position = new Vector3(newX, player.position.y, player.position.z);
            plrAnimator.SetFloat(SpeedHash, 0.95f);
            yield return null;
        }
        plrAnimator.SetFloat(SpeedHash, 0f);
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
        player.GetComponent<HealthModule>()?.Heal(1000);
       

        Scene currentScene = SceneManager.GetActiveScene();
        GameManager.Instance.CurrentPlayer.Set("SceneName", currentScene.name);
        GameManager.Instance.CurrentPlayer.Set("Checkpoint", CheckPointID);
        GameManager.Instance.CurrentPlayer.Set("HP", 100f);

        SavingUI.Instance.saving = true;

        Task saveTask = GameManager.Instance.SavePlayer();
        yield return new WaitUntil(() => saveTask.IsCompleted);

        SavingUI.Instance.completed = true;
    }
    public void ActivateHealingStation()
    {
        if (currentAnimation == null)
        {
            currentAnimation = StartCoroutine(GoDownRoutine());
        }
    }

    void OnDisable()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }
}