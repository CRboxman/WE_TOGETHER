using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickShrink : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Transform pressTarget;
    [SerializeField] private float pressScale = 0.8f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.12f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Vector3 originScale;
    private Tween scaleTween;
    private bool isPressed;
    private Transform Target => pressTarget != null ? pressTarget : transform;

    private Button btn;

    private void Awake()
    {
        CaptureOriginScale();
        btn = GetComponent<Button>();
    }

    private void OnDisable()
    {
        isPressed = false;
        scaleTween?.Kill();
        Target.localScale = originScale;
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Press();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void OnMouseDown()
    {
        Press();
    }

    private void OnMouseUp()
    {
        Release();
    }

    private void Press()
    {
        if (isPressed || !CanAnimate())
        {
            return;
        }

        isPressed = true;
        PlayScale(originScale * pressScale, pressDuration, pressEase);
    }

    private void Release()
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        if (!CanAnimate())
        {
            ResetScale();
            return;
        }

        PlayScale(originScale, releaseDuration, releaseEase);
    }

    private bool CanAnimate()
    {
        return btn == null || btn.interactable;
    }

    public void SetPressTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (pressTarget == target)
        {
            return;
        }
        
        CaptureOriginScale();
        pressTarget = target;
        ResetScale();
    }

    private void ResetScale()
    {
        scaleTween?.Kill();
        Target.localScale = originScale;
    }

    private void PlayScale(Vector3 targetScale, float duration, Ease ease)
    {
        scaleTween?.Kill();
        scaleTween = Target.DOScale(targetScale, duration).SetEase(ease);
    }

    private void CaptureOriginScale()
    {
        originScale = Target.localScale;
    }
}
