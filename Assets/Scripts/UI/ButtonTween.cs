using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;

public class ButtonTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleFactor = 1.2f;
    public float duration = 0.3f;
    public Ease easeType = Ease.OutBack;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Tween currentTween;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale * scaleFactor;
    }

    public void OnPointerEnter(PointerEventData data)
    {
        SoundManager.Instance.PlaySFX("hover");
        currentTween?.Kill();
        currentTween = transform
            .DOScale(targetScale, duration)
            .SetEase(easeType)
            .SetUpdate(true); 
    }

    public void OnPointerExit(PointerEventData data)
    {
        currentTween?.Kill();
        currentTween = transform
            .DOScale(originalScale, duration)
            .SetEase(easeType)
            .SetUpdate(true); 
    }
}
