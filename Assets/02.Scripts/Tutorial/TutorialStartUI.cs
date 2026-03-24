using Fusion;
using UnityEngine;

public class TutorialStartUI : NetworkedTriggerEventSupporter
{
    //코드 담당자 : 최은주
    //트리거 안에 들어오면 게임 시작 묻는 UI 생성 

    public GameObject startCanvas;

    [SerializeField] TutorialManager tm;

    protected override void OnTargetEnter(Collider other)
    {                                  
        CursorManager.Instance.OpenPushUI();
        startCanvas.SetActive(true);
        tm.SetPlayerPaused(true);

        if(tm.currState == TutorialState.StartTutorial)
        {
            startCanvas.SetActive(false);
        }
    }

    protected override void OnTargetExit(Collider other)
    {
        startCanvas.SetActive(false);
        CursorManager.Instance.ClosePopUI();
    }   
}
