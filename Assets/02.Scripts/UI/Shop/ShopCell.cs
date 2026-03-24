using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum PurchaseState
{
    CoinBuy,
    MileageBuy,
    SoldOut
}

// 작성자 : 정하윤
public class ShopCell : Cell
{
    [SerializeField] private Button buyButton;              // 일반 무료 구매
    [SerializeField] private TextMeshProUGUI buttonText;

    [SerializeField] private Image icon;
    [SerializeField] private Image buttonIcon;

    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI description;    // 아이템 설명

    // 구매 버튼에 들어가는 이미지
    [SerializeField] private Sprite coinIcon;               
    [SerializeField] private Sprite mileageIcon;
    [SerializeField] private Sprite soldOutIcon;

    private ShopItemData currentItem;
    private PurchaseState currentState = PurchaseState.CoinBuy;

    public Action<ShopItemData> OnBuyButtonClickedRequest;

    private void Awake()
    {
        buyButton.onClick.AddListener(() => OnClicked());
    }

    // 셀 초기화
    public void Initialize(ShopItemData item)
    {
        if (item == null)
            Debug.Log("아이템이 null");

        currentItem = item;
        icon.sprite = item.data.icon;
        itemName.text = item.data.itemName;
        description.text = item.data.description;
    }

    private void SetState(PurchaseState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case PurchaseState.CoinBuy:
                buttonText.text = "구    매";
                buttonIcon.sprite = coinIcon;
                buyButton.interactable = true;
                break;

            case PurchaseState.MileageBuy:
                buttonText.text = "구    매";
                buttonIcon.sprite = mileageIcon;
                buyButton.interactable = true;
                break;

            case PurchaseState.SoldOut:
                buttonText.text = "품    절";
                buttonIcon.sprite = soldOutIcon;
                buyButton.interactable = false;
                break;
        }
    }

    public void OnClicked()
    {
        OnBuyButtonClickedRequest?.Invoke(currentItem);
    }

    public void OnBuyButtonClicked()
    {
        Debug.Log($"구매: {currentItem.data.itemName}");
        currentItem.freeAvailable = false;
    }

    public void OnMileageBuyButtonClicked()
    {
        Debug.Log($"마일리지로 구매: {currentItem.data.itemName}");
        currentItem.isSoldOut = true;
    }

    public void SetItem(ShopItemData item, int index)
    {
        currentItem = item;
        Initialize(item);

        // 초기화 및 상태 반영
        if (item.isSoldOut)
            SetState(PurchaseState.SoldOut);
        else if (!item.freeAvailable)
            SetState(PurchaseState.MileageBuy);
        else
            SetState(PurchaseState.CoinBuy);
    }
}
