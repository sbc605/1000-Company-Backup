using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
public class TapController : MonoBehaviour
{
    [Header("Left Tap")]
    [SerializeField] private GameObject leftTap;
    [SerializeField] private ScrollRect leftScroll;
    [SerializeField] private List<ShopItemData> leftItems;

    [Header("Right Tap")]
    [SerializeField] private GameObject rightTap;
    [SerializeField] private ScrollRect rightScroll;
    [SerializeField] private List<ShopItemData> rightItems;

    [Header("Button")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [SerializeField] private KioskUIController leftKiosk;
    [SerializeField] private KioskUIController rightKiosk;
    [SerializeField] private KioskCartController cart;

    void Awake()
    {
        leftButton.onClick.AddListener(() => SwitchTap(false));
        rightButton.onClick.AddListener(() => SwitchTap(true));
    }

    void Start()
    {
        SwitchTap(false);
    }

    private void SwitchTap(bool showRight)
    {
        // 장바구니가 열려 있으면 데이터 비우고 즉시 닫기
        if (cart.IsOpen)
            cart.ClearAll(true);

        leftTap.SetActive(!showRight);
        rightTap.SetActive(showRight);

        if (showRight)
        {
            // 오른쪽 탭 활성
            rightKiosk.SetData(rightItems, rightScroll);
            rightKiosk.BuildFirst();
            rightKiosk.UpdateVisibleCells(true);

            // 왼쪽 탭 비활
            leftKiosk.RemoveListeners();
        }
        else
        {
            // 왼쪽 탭 활성
            leftKiosk.SetData(leftItems, leftScroll);
            leftKiosk.BuildFirst();
            leftKiosk.UpdateVisibleCells(true);

            // 오른쪽 탭 비활
            rightKiosk.RemoveListeners();
        }
    }
}
