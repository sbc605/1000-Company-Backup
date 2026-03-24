using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    //아래 두 프리팹 은주 추가
    [SerializeField] Transform playerSlotParent; //UI슬롯 부모 
    [SerializeField] GameObject playerSlotPrefab;
    [SerializeField] Transform spawnPoint;

    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    public Dictionary<PlayerRef, PlayerSlotUi> playerSlots = new(); //플레이어 생성 UI 

    [SerializeField] private GameObject voiceChatCanvas; // 수아 추가

    public static bool isPlayerEnter { get; private set; }

    private bool joinedMain = false;


    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("spawnPoint가 할당되지 않았습니다!");
            return;
        }

        if (runner.IsServer || runner.IsSharedModeMasterClient) // 네트워크 플레이어 생성(서버만)
        {
            Transform spawnPosition = spawnPoint;
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition.transform.position, Quaternion.identity, player);

            _spawnedCharacters.Add(player, networkPlayerObject);
            runner.SetPlayerObject(player, networkPlayerObject);
            networkPlayerObject.AssignInputAuthority(player); // ✅ InputAuthority 확실히 명시            
            isPlayerEnter = true;

            Debug.Log($"플레이어 {player.PlayerId} 스폰");

            SeatManager.Instance.AssignSeat(player);
            SeatManager.Instance.CreatePlayerUI_Server(player);

            // 늦게 들어온 플레이어에게 기존 슬롯 정보 일괄 전송
            var existing = runner.ActivePlayers.Where(p => p != player).ToArray();
            if (existing.Length > 0)
            {
                SeatManager.Instance.RPC_SyncExistingSlots_Filtered(player, existing);
            }

            //Ready 상태 초기화
            if (networkPlayerObject.TryGetComponent(out PlayerReadyState prs))
                prs.IsReady = false;
        }

        // 좌석 배정
        if (player == runner.LocalPlayer)
        {
            // 모든 클라이언트에게 새 슬롯 생성 알림
            SeatManager.Instance.CreateLocalSlot(player, runner);
        }
    }

    // 코드 추가: 수아
    private async Task ConnectVivox()
    {
        if (joinedMain) return;
        joinedMain = true;

        // PlayerNameSync.Local이 준비될 때까지 기다리기
        while (PlayerNameSync.Local == null || string.IsNullOrEmpty(PlayerNameSync.Local.PlayerName))
        {
            await Task.Yield(); // 다음 프레임까지 기다림
        }

        // DisplayName을 PlayerRef ID로 지정
        string playerName = PlayerNameSync.Local.PlayerName;

        await VivoxManager.Instance.InitVivox();
        await VivoxManager.Instance.LoginVivox(playerName); // Vivox 로그인
        await VivoxManager.Instance.JoinMainChannel(_runner.SessionInfo.Name); // Vivox 초기화/로그인은 이미 Intro에서 한 번 실행됨

        if (voiceChatCanvas != null)
            voiceChatCanvas.SetActive(true);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
            Debug.Log($"플레이어 {player.PlayerId} 퇴장");

            if (SeatManager.Instance != null)
            {
                SeatManager.Instance.RemovePlayerUI(player);
                SeatManager.Instance.UnassignSeat(player);
            }
        }

        //서버와 클라이언트 동일하게 UI 제거 시도
        if (playerSlots.TryGetValue(player, out var slot))
        {
            Destroy(slot.gameObject);
            playerSlots.Remove(player);
            Debug.Log($"플레이어 {player.PlayerId} UI 삭제 완료");
        }
    }


    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        bool isInputLock = false;
        bool isPaused = false;

        if (PlayerController.Local != null)
        {
            isPaused = PlayerController.Local.IsPaused;
            isInputLock = PlayerController.Local.isInputLocked;
        }

        if (InputManager.Instance != null && InputManager.Instance.playerControls != null && !isPaused && !isInputLock)
        {
            var playerActions = InputManager.Instance.playerControls.Player;

            data.moveDirection = playerActions.Move.ReadValue<Vector2>();

            data.lookDelta = playerActions.Look.ReadValue<Vector2>();

            NetworkButtons buttons = default;
            if (playerActions.Crouch.IsPressed())
            {
                buttons.Set(MyButtons.Crouch, true);
            }

            if (playerActions.Run.IsPressed()) buttons.Set(MyButtons.Run, true);
            data.buttons = buttons;
        }

        input.Set(data);
    }

    public void CreatePlayerUI(PlayerRef player, NetworkRunner runner)
    {
        if (playerSlots.ContainsKey(player))
        {
            Debug.LogWarning($"이미 {player.PlayerId}의 UI가 존재합니다. 중복 안됨");
            return;
        }

        var ui = Instantiate(playerSlotPrefab, playerSlotParent);
        var playerSlot = ui.GetComponent<PlayerSlotUi>();
        playerSlot.Init(player, runner); //플레이어 정보 넘겨주기

        ui.transform.SetSiblingIndex(player.PlayerId);

        playerSlots[player] = playerSlot;

        Debug.Log($"[SeatManager] UI 생성 : Player {player.PlayerId} -> 슬롯 인덱스 {player.PlayerId - 1}");
    }

    public void RemovePlayerUI(PlayerRef player)
    {
        if (playerSlots.TryGetValue(player, out var slot))
        {
            Destroy(slot.gameObject);
            playerSlots.Remove(player);
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // SoundManager.Instance.BgmSoundPlay("office");

        foreach (var playerRef in runner.ActivePlayers)
        {
            Debug.Log("foreach 시작");
            var playerObj = runner.GetPlayerObject(playerRef);
            if (playerObj != null)
            {
                var spawnPos = new Vector3(-8.11f, 0.1f, 15.7f);

                if (playerObj.TryGetComponent(out NetworkCharacterController ncc))
                {
                    ncc.Teleport(spawnPos);
                    Debug.Log($"플레이어 {playerRef.PlayerId} ncc 위치 이동 -> {spawnPos}");
                }
            }
        }

        var ui = GameObject.FindFirstObjectByType<IntroManager>();
        if (ui == null) return;

        foreach (var playerRef in runner.ActivePlayers)
        {
            Debug.Log("foreach 시작");
            var playerObj = runner.GetPlayerObject(playerRef);
            if (playerObj != null)
            {
                if (playerObj.TryGetComponent(out PlayerReadyState state))
                {
                    Debug.Log("플레이어 ReadyState 찾음");
                    if (RequestManager.Instance.nowState == RequestState.EndReq || state.isOffice)
                    {
                        //RequestManager 네트워크비헤비어로 변해서 불값 동기화 되면 클라이언트도 가능해짐
                        //근데 아직 RequestManager가 네트워크비헤비어가 아니라서 그냥 플레이어한테 bool값 전달 
                        ui.gameObject.SetActive(false);
                        Debug.Log("UI꺼짐");
                        // SoundManager.Instance.BgmSoundPlay("office");
                    }
                }
            }
        }
        // SoundManager.Instance.BgmSoundPlay("office");
    }

    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    private NetworkRunner _runner;

    async void StartGame(GameMode mode, string sessionName)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);
        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();

        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = sessionName,
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        // Vivox 연결
        _ = ConnectVivox();
    }


    // 함수 수정 : 정하윤
    private void OnGUI()
    {
        if (_runner == null)
        {
            float buttonWidth = 200;
            float buttonHeight = 40;
            float currentY = 0;

            // Host 버튼 : 항상 새로운 방 생성
            if (GUI.Button(new Rect(0, currentY, buttonWidth, buttonHeight), "새로운 방 생성"))
            {
                string newSessionName = Guid.NewGuid().ToString();
                Debug.Log($"새로운 방 생성 시도: {newSessionName}");
                StartGame(GameMode.Host, newSessionName);
            }

            currentY += buttonHeight;

            // Join 버튼: TestRoom에 참가 시도
            if (GUI.Button(new Rect(0, currentY, buttonWidth, buttonHeight), "참가 or 테스트 방 생성"))
            {
                Debug.Log("TestRoom 참가 또는 생성 시도");
                StartGame(GameMode.AutoHostOrClient, "TestRoom");
            }

            currentY += buttonHeight;

            string[] customRoomNames =
            {
                   "Room_1",
                   "Room_2",
                   "Room_3",
                   "Room_4",
                   "Room_5",
                   "Room_6"
               };

            for (int i = 0; i < customRoomNames.Length; i++)
            {
                string roomName = customRoomNames[i];

                if (GUI.Button(new Rect(0, currentY, buttonWidth, buttonHeight), $"{roomName} (참가/생성)"))
                {
                    Debug.Log($"{roomName} 참가 또는 생성 시도");
                    StartGame(GameMode.AutoHostOrClient, roomName);
                }
                currentY += buttonHeight;
            }
        }
    }

    void StartSingleMode()
    {
        StartGame(GameMode.Single, "newSession");
    }
    public void CreateRoom() //버튼에 연결해줄 함수
    {
        Debug.Log("호스트 모드 전환 시도");
        StartGame(GameMode.Host, "방 만들기");
    }
    public void JoinRoom() //버튼에 연결해줄 함수
    {
        Debug.Log("클라이언트 모드 전환 시도");
        StartGame(GameMode.Client, "방 만들기");
    }
}