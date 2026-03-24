using Fusion;
using UnityEngine;
public class ReturnTrigger : NetworkedTriggerEventSupporter, IInteractable
{
    public bool isTrigger = false;
    [SerializeField] Canvas WsCanvas;
    protected override void OnTargetEnter(Collider other)
    {
        isTrigger = true;
        WsCanvas.gameObject.SetActive(true);
        Debug.Log("플레이어 트리거 들어옴");
    }
    protected override void OnTargetExit(Collider other)
    {
        isTrigger = false;
        WsCanvas.gameObject.SetActive(false);
        Debug.Log("플레이어 트리거 나감");
    }

    void Start()
    {      
    }

    public void Interact(GameObject interactor)
    {
        if (!isTrigger) return;       
        NetworkRunner runner = null;

        var interactorNetObj = interactor.GetComponent<NetworkObject>();
        if (interactorNetObj != null)
            runner = interactorNetObj.Runner;

        if (runner == null)
            runner = NetworkRunner.GetRunnerForScene(gameObject.scene);

        if (runner == null)
        {
            Debug.LogWarning("Runner를 찾을 수 없음 (씬 이동 직후일 수 있음)");
            return;
        }


        // Runner.LocalPlayer → PlayerRef 타입
        var localPlayerRef = runner.LocalPlayer;

        // PlayerRef → 실제 GameObject (NetworkObject)
        var playerNetworkObj = runner.GetPlayerObject(localPlayerRef);

        if (playerNetworkObj != null && playerNetworkObj.TryGetComponent(out CheckRequestEnd reqCheck))
        {
            reqCheck.TurnOnCanvas();
            Debug.Log("로컬 플레이어 UI 켜짐");
        }
        else
        {
            Debug.LogWarning("로컬 플레이어 오브젝트를 찾을 수 없음!");
            foreach (var player in runner.ActivePlayers)
            {
                var playerObj = runner.GetPlayerObject(player);
                if (playerObj != null && playerObj.TryGetComponent(out PlayerController p))
                {
                    if (p.Object.HasInputAuthority)
                    {
                        var ui = p.GetComponentInChildren<CheckRequestEnd>(true);
                        if (ui != null)
                        {
                            ui.TurnOnCanvas();
                            Debug.Log("Runner.ActivePlayers 기준으로 UI 켜짐");
                            return;
                        }
                    }
                }
            }
        }
    }
    


    public void EnableOutline()
    {

    }

    public void DisableOutline()
    {

    }
}
