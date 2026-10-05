using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 한 칸의 UI. 클릭하면 우측 상세 패널이 갱신되고 선택 하이라이트가 켜진다.
/// 프리팹으로 쓰면 인스펙터에서, 런타임 자동 생성이면 Configure()로 참조가 연결된다.
/// </summary>
public class UI_InventorySlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    private InventorySlot slot;
    private Color normalColor = new Color(0.18f, 0.18f, 0.22f, 1f);
    private static readonly Color SelectedColor = new Color(0.45f, 0.35f, 0.2f, 1f);

    public void Configure(Image background, Image icon, TextMeshProUGUI nameText, TextMeshProUGUI countText)
    {
        this.background = background;
        this.icon = icon;
        this.nameText = nameText;
        this.countText = countText;
        if (background != null) normalColor = background.color;
    }

    public void Set(InventorySlot slot)
    {
        this.slot = slot;

        if (icon != null)
        {
            icon.sprite = slot.item != null ? slot.item.icon : null;
            icon.enabled = icon.sprite != null;
        }

        if (nameText != null)
            nameText.text = slot.item != null ? slot.item.itemName : "";

        if (countText != null)
            countText.text = slot.count > 1 ? "x" + slot.count : "";
    }

    public void SetSelected(bool on)
    {
        if (background != null)
            background.color = on ? SelectedColor : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Inventory.Instance != null)
            Inventory.Instance.SelectSlot(this, slot);
    }
}
