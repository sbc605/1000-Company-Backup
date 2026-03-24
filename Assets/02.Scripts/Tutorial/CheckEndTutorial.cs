using UnityEngine;

public class CheckEndTutorial : NetworkedTriggerEventSupporter
{
    //코드 담당자: 최은주
    //게임이 끝난 직후에 방을 나왔다면 초기화

    [SerializeField] TutorialManager tm;

    protected override void OnTargetEnter(Collider other)
    {
        if(tm.currState == TutorialState.CompleteTutorial || tm.currState == TutorialState.CancelTutorial)
        {
            tm.ChangeState(TutorialState.InitTutorial);
        }
    }
}
