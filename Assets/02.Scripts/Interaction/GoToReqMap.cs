using Fusion;
using UnityEngine;

public class GoToReqMap : NetworkedTriggerEventSupporter
{
    //코드 담당자 : 최은주
    //문 앞에 다가가면 의뢰맵으로 가겠냐고 묻고 상호작용 시 감

    BoxCollider boxColl;
    [SerializeField] GameObject exitCanvas;
    [SerializeField] string str;

    private bool cursorPushed = false;

    protected override void OnTargetEnter(Collider other)
    {
        TryCursorOn();
        exitCanvas.SetActive(true);
    }

    protected override void OnTargetExit(Collider other)
    {
        TryCursorOff();
        exitCanvas.SetActive(false);
    }

    public void GetReqName(string mapName)
    {
        str = mapName;
    }

    public void GoToReq()
    {
        Debug.Log("GoToReq 진입");
        BFSceneManager.Instance.AssignRunner(this.Runner);

        if (RequestManager.Instance.readyState == ReadyState.AllReady && RequestManager.Instance.nowState == RequestState.IngReq)
        {
            BFSceneManager.Instance.WaitFade(str);
        }
        exitCanvas.SetActive(false);
    }

    private void TryCursorOn()
    {
        if (cursorPushed) return;

        cursorPushed = true;
        CursorManager.Instance.OpenPushUI();
    }

    private void TryCursorOff()
    {
        if (!cursorPushed) return;

        cursorPushed = false;
        CursorManager.Instance.ClosePopUI();
    }
}
