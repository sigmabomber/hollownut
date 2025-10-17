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

    [Header("Heartbeat and Pulse")]
    [SerializeField] private float heartbeatScaleAmount = 0.15f;
    [SerializeField] private float heartbeatDuration = 0.3f;
    [SerializeField] private float baseHeartbeatInterval = 1.2f;
    [SerializeField] private float fastestHeartbeatInterval = 0.3f;
    [SerializeField] private float damageShakeAmount = 15f;
    [SerializeField] private float damagePulseScale = 0.3f;
    [SerializeField] private float damagePulseDuration = 0.15f;
    [SerializeField] private float healPulseScale = 0.2f;
    [SerializeField] private float healPulseDuration = 0.2f;

    private Vector3 originalScale;
    private Quaternion originalRotation;
    private Coroutine heartbeatCoroutine;
    void Start()
    {
        InitializeComponents();
        healthModule.Initialize(100f);
        currentHealth = healthModule.currentHealth;
        healthModule.onHealthChanged += OnHealthChanged;

        originalScale = healthImage.transform.localScale;
        originalRotation = healthImage.transform.localRotation;

        StartHeartbeat();
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

        if (newCurrent < currentHealth)
        {
            StartCoroutine(HitFreeze());
            StartCoroutine(DamagePulse());
            SoundManager.Instance.PlaySFX("plrTakingDmg");
            if (gameObject.TryGetComponent(out IKnockback knockbackable))
            {
                Vector2 knockbackDir = new Vector2(-transform.localScale.x, 0f);
            }
        }
        else
        {
            StartCoroutine(FlashColor(Color.green));
            StartCoroutine(HealPulse());
        }

        currentHealth = newCurrent;

        RestartHeartbeat();
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

            if (newHP <= 30 && newHP > 20)
                lowHPList = stage3;
            else if (newHP <= 20 && newHP > 10)
                lowHPList = stage2;
            else if (newHP <= 10 && newHP > 5)
                lowHPList = stage1;
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



    void StartHeartbeat()
    {
        if (heartbeatCoroutine != null)
            StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
    }

    void RestartHeartbeat()
    {
        if (heartbeatCoroutine != null)
            StopCoroutine(heartbeatCoroutine);
        heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
    }

    IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            float interval = GetHeartbeatInterval();
            yield return new WaitForSeconds(interval);
            yield return Heartbeat();
        }
    }

    float GetHeartbeatInterval()
    {
        float healthPercent = currentHealth / 100f;

        return Mathf.Lerp(fastestHeartbeatInterval, baseHeartbeatInterval, healthPercent);
    }

    IEnumerator Heartbeat()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * (1f + heartbeatScaleAmount);

        while (elapsed < heartbeatDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (heartbeatDuration / 2f);
            healthImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < heartbeatDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (heartbeatDuration / 2f);
            healthImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        healthImage.transform.localScale = originalScale;
    }

    IEnumerator DamagePulse()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * (1f + damagePulseScale);

        float shakeElapsed = 0f;
        float shakeDuration = damagePulseDuration;

        while (elapsed < damagePulseDuration)
        {
            elapsed += Time.deltaTime;
            shakeElapsed += Time.deltaTime;

            float scaleT = elapsed / damagePulseDuration;
            float scaleCurve = Mathf.Sin(scaleT * Mathf.PI);
            healthImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, scaleCurve);

            float angle = Mathf.Sin(shakeElapsed * 60f) * damageShakeAmount * (1f - scaleT);
            healthImage.transform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);

            yield return null;
        }

        healthImage.transform.localScale = originalScale;
        healthImage.transform.localRotation = originalRotation;
    }

    IEnumerator HealPulse()
    {
        float elapsed = 0f;
        Vector3 targetScale = originalScale * (1f + healPulseScale);

        while (elapsed < healPulseDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (healPulseDuration / 2f);
            float smoothT = Mathf.SmoothStep(0, 1, t);
            healthImage.transform.localScale = Vector3.Lerp(originalScale, targetScale, smoothT);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < healPulseDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (healPulseDuration / 2f);
            float smoothT = Mathf.SmoothStep(0, 1, t);
            healthImage.transform.localScale = Vector3.Lerp(targetScale, originalScale, smoothT);
            yield return null;
        }

        healthImage.transform.localScale = originalScale;
    }

    void OnDestroy()
    {
        if (healthModule != null)
            healthModule.onHealthChanged -= OnHealthChanged;
    }
}