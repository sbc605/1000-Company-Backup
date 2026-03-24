using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
public class CartLineView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI quantityText; // 수량
    [SerializeField] private TextMeshProUGUI linePriceText; // 금액
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;
    [SerializeField] private Button removeButton;

    public ShopItemData Item { get; private set; }
    public int Quantity { get; private set; } = 1;
    public int UnitPrice => Item.itemPrice;
    public int LineTotal => UnitPrice * Quantity;

    public event Action<CartLineView> OnRemoveClicked;
    public event Action<CartLineView> OnChanged;  // 수량 변경 시 총액 갱신용

    void Awake()
    {
        plusButton.onClick.AddListener(() => SetQuantity(Quantity + 1));
        minusButton.onClick.AddListener(() => SetQuantity(Mathf.Max(1, Quantity - 1)));
        removeButton.onClick.AddListener(() => OnRemoveClicked?.Invoke(this));
    }

    public void Init(ShopItemData item, int startQty = 1)
    {
        Item = item;
        itemName.text = item.data.itemName;
        SetQuantity(Mathf.Max(1, startQty), priceChanged: true);
    }

    public void SetQuantity(int q, bool priceChanged = false)
    {
        Quantity = q; // 수량
        quantityText.text = Quantity.ToString();
        linePriceText.text = $"{LineTotal:N0}원"; // 금액
        if (!priceChanged) OnChanged?.Invoke(this);
    }
}
