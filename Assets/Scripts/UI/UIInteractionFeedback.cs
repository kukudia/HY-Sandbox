using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UIInteractionFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, Range(1f, 1.1f)] private float _hoverScale = 1.035f;
    [SerializeField, Range(0.9f, 1f)] private float _pressedScale = 0.97f;
    [SerializeField, Min(1f)] private float _response = 18f;

    private Vector3 _baseScale;
    private Coroutine _animation;
    private bool _hovered;
    private bool _selected;
    private bool _pressed;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (_animation != null) StopCoroutine(_animation);
        _animation = null;
        _hovered = _selected = _pressed = false;
        if (_baseScale == Vector3.zero) _baseScale = transform.localScale;
        transform.localScale = _baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData) { _hovered = true; Animate(); }
    public void OnPointerExit(PointerEventData eventData) { _hovered = false; _pressed = false; Animate(); }
    public void OnPointerDown(PointerEventData eventData) { _pressed = true; Animate(); }
    public void OnPointerUp(PointerEventData eventData) { _pressed = false; Animate(); }
    public void OnSelect(BaseEventData eventData) { _selected = true; Animate(); }
    public void OnDeselect(BaseEventData eventData) { _selected = false; Animate(); }

    private void Animate()
    {
        if (_animation != null) StopCoroutine(_animation);
        _animation = StartCoroutine(AnimateScale());
    }

    private IEnumerator AnimateScale()
    {
        float scale = _pressed ? _pressedScale : (_hovered || _selected ? _hoverScale : 1f);
        Vector3 target = _baseScale * scale;
        while ((transform.localScale - target).sqrMagnitude > 0.000001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-_response * Time.unscaledDeltaTime));
            yield return null;
        }
        transform.localScale = target;
        _animation = null;
    }
}
