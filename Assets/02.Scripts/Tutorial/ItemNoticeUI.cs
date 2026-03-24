using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class ItemNoticeUI : NetworkedTriggerEventSupporter
{
    //무구 설명
    [SerializeField] TutorialManager tm;
    public GameObject objectExplainUI;

    public bool isEnter = true;

    protected override void OnTargetEnter(Collider other)
    {
        if(tm.currState == TutorialState.EnterPlayer && isEnter)
        {
            tm.SetPlayerPaused(true);
            CursorManager.Instance.OpenPushUI();
            objectExplainUI.SetActive(true);            
        }
    }
    protected override void OnTargetExit(Collider other)
    {
        objectExplainUI.SetActive(false);
        CursorManager.Instance.ClosePopUI();
        isEnter = false;
    }
}
