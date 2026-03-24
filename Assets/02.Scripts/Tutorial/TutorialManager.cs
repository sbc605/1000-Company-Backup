using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//코드 담당자: 최은주 
//튜토리얼이 시작했으면 태블릿에 정보 연동 및 UI에서 정보 관리

[System.Serializable]
public struct TutorialItemSpawnData
{
    public int itemID;
    public Vector3 position;
    public Vector3 eulerRotation;
}

public enum TutorialState
{
    EnterPlayer,
    StartTutorial,
    CompleteTutorial,
    InitTutorial,
    CancelTutorial
}

public class TutorialManager : NetworkBehaviour
{
    public TutorialState currState;
    [SerializeField] TutorialItemSpawnData[] tutorialItems;
    public TutorialManager Instance { get; set; }

    //순서 
    // 귀신 아이템 찾기 -> 무구로 귀신 특성 알아내기 -> 알아낸 귀신 버튼 누르기 -> 귀신방 찾기 -> 제령 아이템 구매해서 제령하기 -> 결과 제출하기

    #region 참조 필드
    [Header("Ui 관리")]
    [SerializeField] GameObject WholeTutorialUi; //가장 최상위 튜토리얼 ui 오브젝트
    [SerializeField] Button startTutoYes; //튜토리얼 시작 예 버튼 (startCanvas -> yes)
    [SerializeField] Button cancelTutobtn; //중도 포기 예 버튼 (cancelCanvas -> yes)
    [SerializeField] Button cancelTutoNo; //중도 포기 아니오 버튼 (cancelCanvas -> no)
    [SerializeField] GameObject completeTuto; //튜토리얼 끝나면 나타나는 캔버스
    [SerializeField] GameObject noticeUI; //오브젝트들에 달린 F를 눌러 상호작용 WS 캔버스
    [SerializeField] GameObject helpUi;
    [SerializeField] GameObject firstUi; //문 들어갔을 때 나오는 Ui

    [Header("관련 오브젝트 관리")]    
    [SerializeField] GameObject boxVolume;
    [SerializeField] GameObject enterDoor; //문
    [SerializeField] BoxCollider enterColl;
    [SerializeField] BoxCollider itemColl;
    [SerializeField] BoxCollider startColl;
    [SerializeField] BoxCollider initColl; //문 밖의 초기화 콜라이더
    [SerializeField] Door[] wholeDoors; //맵 안에 있는 모든 문 (초기화용)
    [SerializeField] Drawer[] wholeDrawers; //맵 안에 있는 모든 서랍 (초기화용)
    Outline doorLine;
    NetworkObject playerObj;
 
    [Header("토글 관리")]
    [SerializeField] Toggle findGhostItem; //귀신 아이템 찾았나
    [SerializeField] Toggle findGhostTrait; //무구를 통해 귀신 특성 밝혀내 버튼 누름
    [SerializeField] Toggle figureOutGhost; //태블릿에서 귀신 버튼 누름
    [SerializeField] Toggle findGhostRoom; //귀신방 찾았는지
    [SerializeField] Toggle succeedExorcism; //제령 성공했는지
    [SerializeField] Toggle submitResult; //결과 제출했는지

    [Header("참조 스크립트")]
    [SerializeField] TutorialStartUI stu;
    [SerializeField] TutorialUI tu;
    [SerializeField] ItemNoticeUI inu;   
    [SerializeField] GhostItem gi;
    [SerializeField] GhostSpawner gs;
    [SerializeField] MissionTrigger mt;
    public GhostGuessDataUIController gguc;
    public SubmitResultUIController sruc;
    InventoryManager playerInven;
    PlayerController playerControl;
    Door door;

    private int toggleTrueValue = 0;
    //튜토리얼 시작했으면 의뢰 정보 태블릿에 연동해주고 
    //각각 함수 토글에 연결해서 helpUI에 연동까지
    #endregion

    public override void Spawned()
    {
        StartCoroutine(BindLocalUIWhendReady());
    }

    public void Start()
    {
        //doorLine = enterDoor.GetComponent<Outline>();
        //door = enterDoor.GetComponent<Door>();

        //startTutoYes.onClick.AddListener(StartTutorial);
        //cancelTutobtn.onClick.AddListener(OnCancelTutorial);
    }

    IEnumerator BindLocalUIWhendReady()
    {
        // LocalPlayer 준비 대기
        while (Runner == null) yield return null;

        // 내 PlayerObject 준비 대기
        NetworkObject playerObj = null;
        while (playerObj == null)
        {
            playerObj = Runner.GetPlayerObject(Runner.LocalPlayer);
            yield return null;
        }

        // 중요: root 기준으로 찾기 (형제/상위 문제 회피)
        var root = playerObj.transform.root;

        var tablet = root.Find("Tablet Canvas");
        while (tablet == null)
        {
            // 혹시 생성/활성 타이밍 늦으면 계속 대기
            tablet = root.Find("Tablet Canvas");
            yield return null;
        }

        gguc = tablet.GetComponentInChildren<GhostGuessDataUIController>(true);
        sruc = tablet.GetComponentInChildren<SubmitResultUIController>(true);

        doorLine = enterDoor.GetComponent<Outline>();
        door = enterDoor.GetComponent<Door>();

        startTutoYes.onClick.AddListener(StartTutorial);
        cancelTutobtn.onClick.AddListener(OnCancelTutorial);
        cancelTutoNo.onClick.AddListener(NoCancelTutorial);
        Debug.Log($"[TutorialManager] bind ok. root={root.name}, tablet={tablet.name}, gguc={(gguc != null)}, sruc={(sruc != null)}");

    }

    private void Update()
    {
        if(currState == TutorialState.StartTutorial && toggleTrueValue >= 6)
        {
            ChangeState(TutorialState.CompleteTutorial);
        }            
    }

    #region 기본 상태 및 플레이어 정보 함수
    public void ChangeState(TutorialState state)
    {
        currState = state;

        switch (currState)
        {
            case TutorialState.EnterPlayer:
                EnterPlayer();
                Debug.Log("플레이어 들어옴");
                break;
            case TutorialState.StartTutorial:
                //StartTutorial(); 버튼 누름과 동시에 실행됨                
                break;
            case TutorialState.CompleteTutorial:
                CompletedTutorial();
                break;
            case TutorialState.InitTutorial:
                InitAllSystem();
                break;
            case TutorialState.CancelTutorial:
                //OnCancelTutorial(); 버튼 누름과 동시에 실행
                break;

        }
    }
    
    public void GetPlayerInfo(NetworkObject player = null, PlayerController playerCtrl = null)
    {
        playerObj = player;
        playerInven = playerObj.GetComponent<InventoryManager>();
        playerControl = playerObj.GetComponent<PlayerController>();
    }

    public void SetPlayerPaused(bool isPaused)
    {
        playerControl.SetPaused(isPaused);
    }

    #endregion

    #region 토글 연결 함수

    //귀신 아이템 찾기
    public void OnFindGhostItem()
    {        
        if(gi.isFounded && !findGhostItem.isOn)
        {
            findGhostItem.isOn = true;
            toggleTrueValue++;
        }
    }

    //귀신 특성 버튼 3개 이상 누름
    public void OnClickedGhostTrait()
    {
        if(!findGhostTrait.isOn)
        {
            findGhostTrait.isOn = true;
            toggleTrueValue++;
        }
    }

    //귀신 추리 버튼 누름
    public void OnGhostFigureOut() 
    {
        if(!figureOutGhost.isOn)
        {
            figureOutGhost.isOn = true;
            toggleTrueValue++;
        }
    }

    //귀신 방 찾음 
    public void OnFindGhostRoom()
    {       
        if(!findGhostRoom.isOn)
        {
            findGhostRoom.isOn = true;
            toggleTrueValue++;
        }
    }

    //퇴마 성공 
    public void OnSucceedExorcism()
    {
        if(!succeedExorcism.isOn)
        {
            succeedExorcism.isOn = true;
            toggleTrueValue++;
        }
    }

    //결과 보고
    public void OnSubmitResult()
    {
        if(!submitResult.isOn)
        {
            submitResult.isOn = true;
            toggleTrueValue++;
        }
    }

    #endregion

    #region 튜토리얼 CASE 함수
    
    //플레이어가 들어왔을 때 실행
    public void EnterPlayer()
    {
        initColl.enabled = false;
        itemColl.enabled = true;
        startColl.enabled = true;
        noticeUI.SetActive(true);
        boxVolume.SetActive(false);
    }

    //플레이어가 튜토리얼 시작하기를 눌렀을 때 실행
    public void StartTutorial()
    {
        Debug.Log("버튼 누름");
        SetPlayerPaused(false);
        ChangeState(TutorialState.StartTutorial);
        itemColl.enabled = false;
        noticeUI.SetActive(false);
        boxVolume.SetActive(true);
        startColl.enabled = false;
        if (door.IsOpen) //문 열려있으면 닫기
        {
            door.IsOpen = false;                            
        }
        door.LockDoor();
        doorLine.enabled = false;
        helpUi.SetActive(true);    
        mt.OnStartGame(playerObj);
        //브금 틀기
    }

    //모든 목표를 완료하면 나오는 Ui  
    public void CompletedTutorial()
    {        
        helpUi.SetActive(false);
        completeTuto.SetActive(true);
        enterColl.enabled = false;
        itemColl.enabled = false;
        startColl.enabled = false;
        initColl.enabled = true; //초기화 콜라이더 켜기        
        boxVolume.SetActive(false);
        //브금 끄기 
    }

    //중도 포기 
    public void OnCancelTutorial()
    {
        ChangeState(TutorialState.CancelTutorial);  
        enterColl.enabled = false;
        itemColl.enabled = false;
        startColl.enabled = false;
        SetPlayerPaused(false);
        gs.DespawnCurrentGhost();       
        door.IsOpen = true;      
        doorLine.enabled = true;
        helpUi.SetActive(false);
    }

    //초기화 함수
    public void InitAllSystem()
    {
        WholeTutorialUi.SetActive(false);
        completeTuto.SetActive(false);       
        sruc.ResetPanel(); 
        InitToggle();
        ResetAllItem();
        ChangeState(TutorialState.EnterPlayer);
        enterColl.enabled = true;
        initColl.enabled = false;
        firstUi.SetActive(true);
    }

    #endregion

    #region CASE 참조 함수
    public void ResetAllItem()
    {
        if (!Runner.IsServer) return;

        //  모든 플레이어 인벤에서 제거
        List<InventoryManager> inventories = new();
        Runner.GetAllBehaviours(inventories);

        foreach (var inven in inventories)
        {
            foreach (var data in tutorialItems)
            {
                inven.RemoveItemByID(data.itemID);
            }
        }

        //  맵에 떨어진 튜토리얼 아이템 삭제
        List<ItemObject> worldItems = new();
        Runner.GetAllBehaviours(worldItems);

        foreach (var item in worldItems)
        {
            if (item.itemData == null) continue;

            foreach (var data in tutorialItems)
            {
                if (item.itemData.itemID == data.itemID)
                {
                    Runner.Despawn(item.Object);
                    break;
                }
            }
        }
        // 제자리 스폰
        foreach (var data in tutorialItems)
        {
            SpawnTutorialItem(data);
        }

        //문 및 서랍 초기화
        foreach (var d in wholeDoors)
            if (d != null && d.IsOpen) d.IsOpen = false;

        foreach (var dr in wholeDrawers)
            if (dr != null && dr.IsOpen) dr.IsOpen = false;

        Debug.Log("튜토리얼 아이템 초기화 완료");
    }


    //튜토리얼 아이템 재스폰
    private void SpawnTutorialItem(TutorialItemSpawnData data)
    {
        ItemData itemData = ItemDatabase.GetItemDataFromID(data.itemID);
        if (itemData == null || itemData.dropPrefab == null) return;

        Runner.Spawn(
            itemData.dropPrefab,
            data.position,
            Quaternion.Euler(data.eulerRotation)
        );
    }

    //토글 초기화
    public void InitToggle()
    {
        findGhostItem.isOn = false;
        findGhostTrait.isOn = false;
        findGhostRoom.isOn = false;
        figureOutGhost.isOn = false;
        succeedExorcism.isOn = false;
        submitResult.isOn = false;
        toggleTrueValue = 0;
    }


    public void NoCancelTutorial()
    {
        SetPlayerPaused(false);
    }

    #endregion

}
