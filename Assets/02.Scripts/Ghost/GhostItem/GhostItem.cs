using Fusion;
using UnityEngine;

//코드 담당자 : 최은주
//모든 귀신 아이템에게 들어가는 스크립트
public class GhostItem : NetworkBehaviour, IGhostItem
{
    [SerializeField] GameObject magicRoot;
    [SerializeField] ParticleSystem magicZone;
    TutorialManager tm;
    public bool isFounded = false;
    public bool isRoomReact = false;
    public bool isGhostItemInstall = false;

    void Start()
    {
        tm = FindFirstObjectByType<TutorialManager>();
    }

    void Update()
    {

    }

    public void OnChangedState(bool isFind)
    {
        isFounded = isFind;
        Debug.Log($"isFounded = {isFounded}");
        tm.OnFindGhostItem();
    }

    //은주 추가
    //귀신의 방에 귀신 아이템 놓는 경우 마법진 나타나게 수정
    public void GhostRoomReaction()
    {
        if(isGhostItemInstall)
        {
            isRoomReact = true;
            magicRoot.SetActive(true);
            magicZone.Play();
            tm.OnFindGhostRoom();
            //설치 됐을 때만 반응하도록

        }
    }

    public void GetInstallValue(bool isInstall)
    {
        isGhostItemInstall = isInstall;
    }

}



