using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheckRequestEnd : NetworkBehaviour
{
    //코드 담당자 : 최은주
    //의뢰가 끝났는지 확인하고 진행 중이라면 포기하겠냐는 문구
    [SerializeField] TextMeshProUGUI returnText;
    [SerializeField] GameObject returnCanvas;

    [SerializeField] Button yesBtn;
    [SerializeField] Button backBtn;

    public SpawnManager spManager;

    public bool isBack = false;

    //월드 스페이스로 F를 눌러 본부로 복귀 뛰우고 F 누르면 진짜 갈거냐고 물어보기

    void Awake()
    {
        yesBtn.onClick.AddListener(YesButton);
        backBtn.onClick.AddListener(BackButton);
    }

    private void OnEnable()
    {
        spManager = FindFirstObjectByType<SpawnManager>();
    }

    void CheckEndGame() //F눌렀을 때
    {
        spManager = FindFirstObjectByType<SpawnManager>();

        if (Runner.IsServer)
        {
            if (RequestManager.Instance.nowState == RequestState.EndReq)
            {//제령 완료 상태          
                ReturnText("본부로 돌아가시겠습니까?");
            }
            else if (RequestManager.Instance.nowState == RequestState.IngReq)
            {//제령 안 끝난 상태           
                ReturnText("임무를 포기하시겠습니까?");
            }
        }
        else
        {
            ReturnText("본부로 돌아가시겠습니까?");
        }
        Debug.Log($"{RequestManager.Instance.nowState}");
    }
    void YesButton() //씬 전환
    {
        isBack = true;
        spManager.GetChangeBool(isBack);
        CursorManager.Instance.ClosePopUI();
        StartCoroutine(StartSound());

        if (Runner.IsServer)
        {
            BFSceneManager.Instance.AssignRunner(this.Runner);
            RequestManager.Instance.nowState = RequestState.EndReq; //의뢰 종료로 변환
            returnCanvas.SetActive(false);
            BFSceneManager.Instance.WaitFade("Office"); //씬에 Fade 있어야함 캔버스에      
        }
        else
        {
            returnCanvas.SetActive(false);
            RPC_RequestYes();
        }

        Debug.Log("예스 버튼 누름");
    }
    void BackButton()
    {
        returnCanvas.SetActive(false);
        CursorManager.Instance.ClosePopUI();

    }
    public void TurnOnCanvas()
    {
        Debug.Log("함수 진입");
        CursorManager.Instance.OpenPushUI();
        returnCanvas.gameObject.SetActive(true);
        CheckEndGame();
    }

    void ReturnText(string msg)
    {
        returnText.text = msg;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_RequestYes()
    {
        Debug.Log("[Server] 클라이언트 요청 수신 → 씬 전환 실행");
        BFSceneManager.Instance.AssignRunner(this.Runner);
        BFSceneManager.Instance.WaitFade("Office");
        isBack = true;
        spManager.GetChangeBool(isBack);
    }

    IEnumerator StartSound()
    {
        yield return new WaitForSeconds(3f);

        SoundManager.Instance.BgmSoundPlay("office");
    }
}
