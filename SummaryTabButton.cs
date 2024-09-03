using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SummaryTabButton: UIButton
{
    [SerializeField] private SummaryTab thisTabType;

    public override void OnEnable()
    {
        GetComponents();
        GetDefaultValues();
    }

    public override void GetComponents()
    {
        _bg = transform.Find("BG").gameObject.GetComponent<Image>();
        _iconObject = transform.Find("ICON").gameObject;
        _icon = _iconObject.GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!BeastSummaryController.Instance.CurrentlyEvolving) BeastSummaryController.Instance.LoadPage(thisTabType);
    }

    public override void ToggleHoverStyle()
    {
        if (_state == ButtonState.A_State) return;
        if (_isBeingHovered)
        {
            float newBGAlpha = _stateToDimensions[ButtonState.Hovered][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
        else
        {
            float newBGAlpha = _stateToDimensions[ButtonState.B_State][0];
            _bg.color = new Color(_bg.color.r, _bg.color.g, _bg.color.b, newBGAlpha);
        }
    }
}
