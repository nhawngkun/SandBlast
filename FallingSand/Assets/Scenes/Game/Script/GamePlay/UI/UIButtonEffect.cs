using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Button _button;
    private Vector3 _originalScale;
    private Color _originalColor;

    public Color hoverColor = Color.cyan;
    public float clickScaleFactor = 0.9f;
    public float scaleFactor = 1.1f;
    public float duration = 0.2f;

    void Start()
    {
        _button = GetComponent<Button>();
        _originalScale = transform.localScale;
        _originalColor = _button.image.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.DOScale(_originalScale * scaleFactor, duration).SetEase(Ease.OutBack);
        _button.image.DOColor(hoverColor, duration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(_originalScale, duration).SetEase(Ease.InOutQuad);
        _button.image.DOColor(_originalColor, duration);
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(_originalScale * clickScaleFactor, duration * 0.5f).SetEase(Ease.InOutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(_originalScale * scaleFactor, duration * 0.5f).SetEase(Ease.OutBack);
    }
}
