using Fusion;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TVscreen : NetworkedTriggerEventSupporter
{
    [Header("티비 의뢰창")]
    [SerializeField] TextMeshProUGUI danger;
    [SerializeField] TextMeshProUGUI weirdpm;
    [SerializeField] TextMeshProUGUI information;
    [SerializeField] TextMeshProUGUI title;

    [Header("레디 상태")]
    [SerializeField] TextMeshProUGUI readyCountText;
    [SerializeField] Button startButton;

    [SerializeField] GoToReqMap startGame;

    [Header("멀티 입장")]
    [SerializeField] Button createRoomBtn;
    [SerializeField] Button joinRoomBtn;
    [SerializeField] TMP_InputField roomCodeInput;
    [SerializeField] Image roomCodeImage;

    public Camera playerCam;
    Canvas screenCanvas;
    bool findPlayer;

    public static TVscreen Instance { get; private set; }

    private int _lastKnownReadyCount = -1;
    private int _lastKnownTotalCount = -1;

    // private bool isLocalInRange = false;
    private bool cursorPushed = false;

    private string reqScene;

    public override void Spawned()
    {
        base.Spawned();
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Runner.Despawn(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        screenCanvas = GetComponent<Canvas>();
        title.text = "의뢰 정보 : ";
        startButton.interactable = false;
        //startButton.onClick.AddListener(StartButton); 
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (Runner == null || ReadyManager.Instance == null || ReadyManager.Instance.Runner == null)
        {
            return;
        }

        int currentReady = ReadyManager.Instance.ReadyCount;
        int currentTotal = ReadyManager.Instance.TotalCount;

        if (currentReady != _lastKnownReadyCount || currentTotal != _lastKnownTotalCount)
        {
            UpdateReadyCountText(currentReady, currentTotal);
            _lastKnownReadyCount = currentReady;
            _lastKnownTotalCount = currentTotal;
        }
    }

    protected override void OnTargetEnter(Collider other)
    {
        if (other.CompareTag("Player") &&
            other.TryGetComponent<NetworkObject>(out var netObj) &&
            netObj.HasInputAuthority)
        {
            // isLocalInRange = true;

            if (!findPlayer)
            {
                FindPlayerCamera(netObj.gameObject);
            }

            TryCursorOn();
        }
    }

    protected override void OnTargetExit(Collider other)
    {
        if (other.CompareTag("Player") &&
            other.TryGetComponent<NetworkObject>(out var netObj) &&
            netObj.HasInputAuthority)
        {
            Debug.Log("TV Trigger Exit - Cursor Off");
            // isLocalInRange = false;
            TryCursorOff();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowNowRequest(string requestName, int dangerness, int weirdPM, string content, string sceneName)
    {
        title.text = "의뢰 정보:" + requestName;
        danger.text = "의뢰 난이도 : " + dangerness;
        weirdpm.text = "이상 현상 :" + weirdPM + "개";
        information.text = content;

        reqScene = sceneName;
        startGame.GetReqName(reqScene);
    }

    public void UpdateReadyCountText(int ready, int total)
    {
        if (readyCountText != null)
        {
            readyCountText.text = $"레디 인원 : {ready} / {total}";
        }

        bool allReady = (ready == total) && total > 0;
        if (startButton != null)
        {

            startButton.interactable = allReady && Runner.IsServer;
            if (allReady)
            {
                RequestManager.Instance.SetReadyState(ReadyState.AllReady);
            }

        }
    }

    void FindPlayerCamera(GameObject myPlayerObject)
    {
        if (screenCanvas.worldCamera == null)
        {
            playerCam = myPlayerObject.GetComponentInChildren<Camera>();
            if (playerCam != null)
            {
                screenCanvas.worldCamera = playerCam;
                findPlayer = true;
            }
            else
            {
                Debug.LogError("Player 객체에서 Camera를 찾지 못했습니다.");
            }
        }
    }

    private void TryCursorOn()
    {
        if (cursorPushed) return;

        cursorPushed = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CursorManager.Instance.OpenPushUI();
    }

    private void TryCursorOff()
    {
        if (!cursorPushed) return;

        cursorPushed = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CursorManager.Instance.ClosePopUI();
    }
}