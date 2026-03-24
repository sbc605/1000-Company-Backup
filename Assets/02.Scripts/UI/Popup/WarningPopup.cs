using System;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
// 재확인하는 팝업 관리하는 스크립트
public class WarningPopup : ItemPopupBase
{
    [SerializeField] private Button agreeButton;
    private Action onConfirm;
    public void Open(Action onConfirm)
    {
        this.onConfirm = onConfirm;
    }
    public void onClickDropAgree()
    {
        onConfirm?.Invoke();
        Hide();
    }
    public void onClickUseAgree()
    {
        onConfirm?.Invoke();
        Hide();
    }
}
