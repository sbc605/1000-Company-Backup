using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : NetworkBehaviour, INetworkRunnerCallbacks
{
    //코드 담당자 : 최은주
    //의뢰맵에서 스폰될 때 위치 조정 및 사운드 재생

    public bool isOffice = false;
    public bool isChange = false;
    [SerializeField] Transform[] spawnPoints;

    void Awake()
    {
        if (FindAnyObjectByType<NetworkRunner>() is NetworkRunner runner)
        {

            runner.AddCallbacks(this);
        }
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (runner == null)
        {
            Debug.LogWarning("[SpawnManager] runner is null → skip OnSceneLoadDone");
            return;
        }

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        switch (sceneIndex)
        {
            case 0:
                SoundManager.Instance.BgmSoundPlay("office");
                break;
            case 1:
                SoundManager.Instance.BgmSoundPlay("req1 bgm");
                break;
        }

        Debug.Log("[request1] OnSceneLoadDone 호출됨!");
        bool isReqTrue = true;

        if (runner.IsServer && isReqTrue)
        {
            foreach (var playerRef in runner.ActivePlayers)
            {
                var playerObj = runner.GetPlayerObject(playerRef);
                if (playerObj != null)
                {
                    var index = playerRef.PlayerId % spawnPoints.Length;
                    var spawnPos = spawnPoints[index].position;

                    if (playerObj.TryGetComponent(out NetworkCharacterController ncc))
                    {
                        ncc.Teleport(spawnPos);
                        Debug.Log($"플레이어 {playerRef.PlayerId} ncc 위치 이동 -> {spawnPoints[index].position}");
                        isReqTrue = false;
                    }
                    else
                    {
                        Debug.Log($"플레이어 {playerRef.PlayerId} NCC 없음");
                    }

                }
            }
        }
        foreach (var playerRef in runner.ActivePlayers)
        {
            var playerObj = runner.GetPlayerObject(playerRef);
            if (playerObj != null)
            {
                if (playerObj.TryGetComponent(out PlayerReadyState state))
                {
                    state.isOffice = true;
                    Debug.Log($"{state.isOffice}");
                }
                else
                {
                    Debug.Log($"플레이어에게서 PlayerReadyState 못 찾음");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (FindAnyObjectByType<NetworkRunner>() is NetworkRunner runner)
        {
            runner.RemoveCallbacks(this);
            Debug.Log("[spaneManager] Runner콜백 해제");
        }
    }

    public void GetChangeBool(bool isEnd)
    {
        isChange = isEnd;
        Debug.Log("GetChangeBool 함수 진입");
    }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
