using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 코드 담당자: 김수아

// 버튼 클릭시 장바구니에 넣음
public class KioskItemCell : Cell, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Item UI")]
    [SerializeField] private Button itemButton;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemPrice;

    [Header("Tooltip")]
    [SerializeField] TooltipUI tooltip;
    private string cashedText;

    private ShopItemData currentItem;
    private KioskCartController cart;

    private void Awake()
    {
        if (itemButton != null)
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.onClick.AddListener(OnClicked);
        }

        if (!tooltip)
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas)
                tooltip = canvas.GetComponentInChildren<TooltipUI>(true);
        }
    }

    public void Bind(ShopItemData item, KioskCartController cartRef)
    {
        currentItem = item;
        cart = cartRef;

        bool hasItem = currentItem != null && currentItem.data != null;

        if (hasItem)
        {
            if (itemIcon) { itemIcon.enabled = true; itemIcon.sprite = currentItem.data.icon; }
            if (itemName) itemName.text = currentItem.data.itemName;
            if (itemPrice) itemPrice.text = $"{currentItem.itemPrice:N0}원";
            cashedText = currentItem.data.description;
        }
        else
        {
            if (itemIcon) { itemIcon.enabled = false; itemIcon.sprite = null; }
            if (itemName) itemName.text = "Test";
            if (itemPrice) itemPrice.text = "0원";
            cashedText = "아이템 설명";
        }
    }

    public void OnClicked()
    {
        if (currentItem == null || cart == null) return;
        cart.AddItem(currentItem, 1);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltip == null) return;
        tooltip.SetText(cashedText);
        tooltip.Show();
        tooltip.BeginFollow(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (tooltip == null || !tooltip.gameObject.activeSelf) return;
        tooltip.StickyFollow(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltip == null) return;
        tooltip.Hide();
    }
}
