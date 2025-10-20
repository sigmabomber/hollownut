using System.Buffers.Text;
using UnityEngine;

public class StickHolder : MonoBehaviour
{
    public Sprite HolderWithStick;
    public Sprite Holder;
    SpriteRenderer SpriteRenderer;
    private bool canInteract = false;

    public LayerMask playerLayer;
    private BaseUI baseUI;
    private void Start()
    {
        SpriteRenderer = GetComponent<SpriteRenderer>();

        baseUI = transform.Find("InteractionUI").GetComponent<BaseUI>();
    }
    void LateUpdate()
    {
        if ( !GameManager.Instance.CurrentPlayer.Get<bool>("StickUnlocked"))
        {
            SpriteRenderer.sprite = HolderWithStick;
            var keybinds = GameManager.Instance.CurrentSettings.GetKeybindsDictionary();
            if (Input.GetKey(keybinds["interact"]) && canInteract)
            {
                GameManager.Instance.CurrentPlayer.Set("StickUnlocked", true);
                UIManager.Instance.CloseUI(baseUI);
            }
        }
        else
        {
            SpriteRenderer.sprite = Holder;
        }


       
    }

    private bool CanInteract(GameObject collidingObject)
    {
        if ((playerLayer.value & (1 << collidingObject.layer)) == 0) return false;


        bool StickUnlocked = GameManager.Instance.CurrentPlayer.Get<bool>("StickUnlocked");



        return !StickUnlocked;
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
}
