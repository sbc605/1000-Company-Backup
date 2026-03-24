using Fusion;
using UnityEngine;

public class TutorialUI : NetworkedTriggerEventSupporter
{
    //코드 담당자 : 최은주
    //튜토리얼 룸에 들어가면 나오는 UI

    public GameObject tutorialCanvas; //안내 ui 
    public GameObject cancelCanvas; //취소 ui

    [SerializeField] TutorialManager tm;

    NetworkObject playerNobj;
    PlayerController playerControl;

    protected override void OnTargetEnter(Collider other)
    {
        playerNobj = other.GetComponent<NetworkObject>();
        playerControl = other.GetComponent<PlayerController>();

        tm.GetPlayerInfo(playerNobj, playerControl);

        if (tm.currState == TutorialState.StartTutorial)
        {
            tm.SetPlayerPaused(true);
            CursorManager.Instance.OpenPushUI();
            cancelCanvas.SetActive(true);               
        }
        else
        {
            tm.SetPlayerPaused(true);
            CursorManager.Instance.OpenPushUI();
            tutorialCanvas.SetActive(true);
            SoundManager.Instance.BgmSoundStop();
            tm.ChangeState(TutorialState.EnterPlayer);
        }
        
    }

    protected override void OnTargetExit(Collider other)
    {
        CursorManager.Instance.ClosePopUI();
    }  
}
