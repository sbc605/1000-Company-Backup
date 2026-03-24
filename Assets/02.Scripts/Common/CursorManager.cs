// 코드 담당자: 김수아
using UnityEngine;

public enum CursorMode
{
    Gameplay,   // FPS 이동 상태
    UI          // 메뉴/키오스크/타블렛
}

/// <summary>
/// UI스크립트에서 열 때는 CursorManager.Instance.OpenPushUI(); 닫을 땐 CursorManager.Instance.ClosePopUI();를 넣는다.
/// UI가 여러개 열려있어도 1개 이상이기만 하면 커서 on, UI 다 닫히면 커서 off 됩니다.
/// </summary>
public class CursorManager : SingleTon<CursorManager>
{
    private int uiRequestCount = 0;

    /// <summary>
    /// UI 열 때
    /// </summary>
    public void OpenPushUI()
    {
        uiRequestCount++;
        Apply();
    }

    /// <summary>
    /// UI 닫을 때
    /// </summary>
    public void ClosePopUI()
    {
        uiRequestCount = Mathf.Max(0, uiRequestCount - 1);
        Apply();
    }

    private void Apply()
    {
        bool uiMode = uiRequestCount > 0;

        Cursor.visible = uiMode;
        Cursor.lockState = uiMode ? CursorLockMode.Confined : CursorLockMode.Locked;
    }

    public bool IsUI => uiRequestCount > 0;
}
