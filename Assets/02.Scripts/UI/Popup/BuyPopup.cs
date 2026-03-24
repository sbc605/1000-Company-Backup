using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class BuyPopup : ItemPopupBase
{
    [SerializeField] private Button agreeButton;
    [SerializeField] private TextMeshProUGUI requestAmountText;

    public Action RequestConfirm;

    
    public void Open(Action onConfirm)
    {
        RequestConfirm = onConfirm;
        gameObject.SetActive(true);
    }
    public void Show(ShopItemData item, string requestText = "구매하시겠습니까?")
    {
        if (requestAmountText)
            requestAmountText.text = requestText;

        // 아이템 관련 UI 업데이트 코드 추가
    }

    public void OnClickAgree()
    {
        RequestConfirm?.Invoke();
        Hide();
    }
}
