using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavingUI : MonoBehaviour
{
    public Image savingCircle;
    public Image savingCompleted;
    public TMP_Text savingText;

    public GameObject parentObj;

    public static SavingUI Instance;

    public float speed = 1f;
    public float flipDelay = 0.2f;
    public float textChangeInterval = 0.5f;

    private bool filling = true;
    private bool waitingToFlip = false;
    private float delayTimer = 0f;
    private float textTimer = 0f;
    private int dotCount = 0;

    public bool completed = false;
    private bool _saving = false;
    private bool hasCompletedFilled = false;
    private bool initializedForSaving = false; 

    public bool saving
    {
        get => _saving;
        set
        {
            if (!_saving && value) // first time saving is true
            {
                ActivateSavingImages();
            }
            _saving = value;
        }
    }

    void Start()
    {
        Instance = this;

        if (savingCircle != null)
        {
            savingCircle.type = Image.Type.Filled;
            savingCircle.fillMethod = Image.FillMethod.Radial360;
            savingCircle.fillClockwise = true;
            savingCircle.fillOrigin = (int)Image.Origin360.Top;
            savingCircle.fillAmount = 0f;
            savingCircle.gameObject.SetActive(true); 
        }

        if (savingCompleted != null)
        {
           
            savingCompleted.fillAmount = 0f;
            savingCompleted.gameObject.SetActive(false); 
        }

        if (savingText != null)
        {
            savingText.text = "Saving Progress";
        }
    }

    void Update()
    {
        if (!saving) return;

        if (completed)
        {
            Completed();
        }
        else
        {
            AnimateCircle();
            AnimateText();
        }
    }

    void ActivateSavingImages()
    {
        if (initializedForSaving) return; // ensure it runs only once

        if (savingCircle != null)
            savingCircle.gameObject.SetActive(true);

        if (savingCompleted != null)
            savingCompleted.gameObject.SetActive(false);
        if (parentObj != null)
            parentObj.SetActive(true);
        initializedForSaving = true;
    }

    void Completed()
    {
        if (savingCircle != null)
            savingCircle.gameObject.SetActive(false);

        if (savingCompleted != null)
        {
            savingCompleted.gameObject.SetActive(true);

            if (!hasCompletedFilled)
            {
                savingCompleted.fillAmount = 0f;
                hasCompletedFilled = true;
            }

            if (savingCompleted.fillAmount < 1f)
            {
                savingCompleted.fillAmount += (speed * 2.7f) * Time.deltaTime;
                if (savingCompleted.fillAmount >= 1f)
                {
                    savingCompleted.fillAmount = 1f;
                    StartCoroutine(ResetAfterDelay(2f));
                }
            }

            Vector3 rot = savingCompleted.rectTransform.eulerAngles;
            rot.y = 0;
            savingCompleted.rectTransform.eulerAngles = rot;
        }

        if (savingText != null)
        {
            savingText.text = "Saving Completed!";
        }
    }

    private System.Collections.IEnumerator ResetAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        parentObj.SetActive(false);
        savingCompleted.fillAmount = 0f;
        savingCompleted.gameObject.SetActive(false);
        savingCircle.gameObject.SetActive(true);
        completed = false;
        _saving = false;
        initializedForSaving = false;
        hasCompletedFilled = false;
    }


    void AnimateCircle()
    {
        if (savingCircle == null) return;

        if (waitingToFlip)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= flipDelay)
            {
                Vector3 rot = savingCircle.rectTransform.eulerAngles;
                rot.y += 180f;
                savingCircle.rectTransform.eulerAngles = rot;

                delayTimer = 0f;
                waitingToFlip = false;
                filling = !filling;
            }
            return;
        }

        if (filling)
        {
            savingCircle.fillAmount += speed * Time.deltaTime;
            if (savingCircle.fillAmount >= 1f)
            {
                savingCircle.fillAmount = 1f;
                waitingToFlip = true;
            }
        }
        else
        {
            savingCircle.fillAmount -= speed * Time.deltaTime;
            if (savingCircle.fillAmount <= 0f)
            {
                savingCircle.fillAmount = 0f;
                waitingToFlip = true;
            }
        }
    }

    void AnimateText()
    {
        if (savingText == null || completed) return;

        textTimer += Time.deltaTime;
        if (textTimer >= textChangeInterval)
        {
            textTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            savingText.text = "Saving Progress" + new string('.', dotCount);
        }
    }
}
