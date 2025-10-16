using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    private SpriteRenderer playerSprite;
    private HealthModule healthModule;
    private float currentHealth;
    private Rigidbody2D rb;


    [SerializeField] private float hitFreezeDuration = 0.1f;
    [SerializeField] private float timeScaleDuringFreeze = 0.01f;


    [Header("UI")]

    public List<Sprite> stages = new();
    public List<Sprite> stage3 = new();
    public List<Sprite> stage2 = new();
    public List<Sprite> stage1 = new();
    public List<Sprite> stage0 = new();

    public float rotationSpeed = 5f;
    public int currentFrame = 0;
    public Animator uiAnimator;
    public Image healthImage;

    private static readonly int HealthHash = Animator.StringToHash("Health");
    void Start()
    {
        InitializeComponents();

        healthModule.Initialize(100f);
        currentHealth = healthModule.currentHealth;
        healthModule.onHealthChanged += OnHealthChanged;
    }

    void InitializeComponents()
    {
        healthModule = GetComponent<HealthModule>();
        playerSprite = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    IEnumerator FlashColor(Color color)
    {
        playerSprite.color = color;
        yield return new WaitForSeconds(0.1f);
        playerSprite.color = Color.white;
    }

    IEnumerator HitFreeze()
    {
        Time.timeScale = timeScaleDuringFreeze;

        yield return new WaitForSecondsRealtime(hitFreezeDuration);

        Time.timeScale = 1f;
    }

    void OnHealthChanged(float newCurrent, float max)
    {
        UpdateUI(newCurrent);

        uiAnimator.SetFloat(HealthHash, newCurrent);

        if (newCurrent < currentHealth)
        {

            StartCoroutine(FlashColor(Color.red));
            StartCoroutine(HitFreeze()); 

            if (gameObject.TryGetComponent(out IKnockback knockbackable))
            {
                Vector2 knockbackDir = new Vector2(-transform.localScale.x, 0f);
            }
        }
        else
        {
            StartCoroutine(FlashColor(Color.green));
        }

        currentHealth = newCurrent;
    }

    void UpdateUI(float newHP)
    {
        if (newHP > 30)
        {
            int index = Mathf.Clamp((int)(newHP / 10), 0, stages.Count - 1);
            healthImage.sprite = stages[index];
            StopAllCoroutines();
        }
        else
        {
            List<Sprite> lowHPList = null;
            bool singleCycle = false;

            if (newHP <= 30 && newHP > 20) lowHPList = stage3;
            else if (newHP <= 20 && newHP > 10) lowHPList = stage2;
            else if (newHP <= 10 && newHP > 5) lowHPList = stage1;
            else
            {
                lowHPList = stage0;
                singleCycle = true; 
            }

            StopAllCoroutines();
            StartCoroutine(CycleImages(lowHPList, singleCycle));
        }
    }

    IEnumerator CycleImages(List<Sprite> textures, bool singleCycle = false)
    {
        if (textures == null || textures.Count == 0) yield break;

        do
        {
            for (int i = 0; i < textures.Count; i++)
            {
                healthImage.sprite = textures[i];
                currentFrame = i;
                yield return new WaitForSeconds(1f / rotationSpeed);
            }

            if (singleCycle) break;

        } while (!singleCycle); 
    }


}