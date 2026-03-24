using Fusion;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonitorInteract : NetworkedTriggerEventSupporter, IInteractable
{
    //코드 담당자: 최은주 
    //모니터에 달린 콜라이더 트리거에 들어오면 임무 선택 UI 뜨고 F 누르면 
    //자리에 앉고 모니터 화면으로 전환

    [SerializeField] Canvas tasknoticeUI;
    [SerializeField] Canvas monitorUI;
    [SerializeField] private Transform sitTarget;//앉기 애니메이션 위치
    [Networked] public PlayerRef CurrentUser { get; private set; }

    private ChangeDetector _changeDetector;
    private bool _isLocalUser = false;

    [Networked] public PlayerRef AssignedPlayer { get; private set; }

    public bool isTrigger;
    public bool isMonitor = false;
    private PlayerController _currentPlayerUsing;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        CurrentUser = PlayerRef.None;
    }

    public void AssignPlayer(PlayerRef player)
    {
        AssignedPlayer = player;
    }


    protected override void OnTargetEnter(Collider other)
    {        
        if(other.TryGetComponent(out NetworkObject netObj)) //네트워크 오브제 확인
        {
            Debug.Log("들어옴");
            var player = netObj.InputAuthority; //자기 자신에 할당

            if(AssignedPlayer == netObj.InputAuthority && netObj.InputAuthority == Runner.LocalPlayer)//자리 확인
            {
                isTrigger = true;
                tasknoticeUI.gameObject.SetActive(true);    
               
                if(other.TryGetComponent(out TabletController playerTablet))
                {
                    Debug.Log("타블렛컨트롤러 받아오기");
                    playerTablet.monitor = this;
                }
            }
        }
    }

    protected override void OnTargetExit(Collider other)
    {
        if (isTrigger)
        {
            isTrigger = false;
            tasknoticeUI.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Debug.Log("나감");
        }
    }

    public void Interact(GameObject player)
    {
        var netObj = player.GetComponent<NetworkObject>();
        if (netObj == null) return;

        if (netObj.InputAuthority != AssignedPlayer || CurrentUser != PlayerRef.None) return;

        Debug.Log("F키 눌림 (서버)");

        if (player.TryGetComponent(out PlayerController pc))
        {
            pc.Server_StartComputerInteraction(sitTarget.position, sitTarget.rotation);
            CurrentUser = netObj.InputAuthority;
            _currentPlayerUsing = pc;
            Debug.Log("앉는 자리");
        }
    }
    public override void Render()
    {

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentUser))
            {
                HandleUserChanged();
            }
        }
    }

    private void HandleUserChanged()
    {
        if (CurrentUser == Runner.LocalPlayer)
        {
            _isLocalUser = true;
            isMonitor = true;
            StartCoroutine(IntoMonitor());
        }
        else if (_isLocalUser)
        {
            _isLocalUser = false;
        }
    }
    public void StopInteraction()
    {
        if (!isMonitor) return;

        StartCoroutine(OutMonitor());

        if (_currentPlayerUsing != null)
        {
            _currentPlayerUsing.Client_StopUsingComputer();

        }
        else
    {
        if (PlayerController.Local != null)
            {
                PlayerController.Local.Client_StopUsingComputer();

            }
    }
        Rpc_RequestStopInteraction();
        _currentPlayerUsing = null;
    }

    IEnumerator IntoMonitor() //모니터로 들어갈 때
    {
        Fade.onFadeAction(1f, Color.black, true, null);        
        yield return new WaitForSeconds(2.5f);
        monitorUI.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    public IEnumerator OutMonitor() //모니터에서 나갈 때 
    {
        if (isMonitor)
        {
            Debug.Log("ESC키 모니터에서 나가기");
            yield return new WaitForSeconds(1.5f);
            Fade.onFadeAction(1f, Color.black, false, null);
            monitorUI.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            isMonitor = false;
        }
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestStopInteraction()
    {
        //if (CurrentUser == Object.InputAuthority) // 주석처리 해서 다시 F눌러도 상호작용 가능하게 함
        //{
            CurrentUser = PlayerRef.None;
        //}
    }
    public void EnableOutline()
    {
       
    }

    public void DisableOutline()
    {
        
    }

}
