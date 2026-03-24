using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
public class MissionTrigger : NetworkBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 내 플레이어의 NetworkObject를 가져와 로컬인지 확인
        var player = other.GetComponentInParent<NetworkObject>();
        if (player == null || !player.HasInputAuthority) return; // 내가 조종 중인 플레이어가 아니면 false

        // 미션 시작 매니저
        var missionManager = NetworkMissionManager.Instance;
        if (missionManager && missionManager.Object && missionManager.Object.IsValid)
            missionManager.Rpc_StartMission();
    }


    public void OnStartGame(NetworkObject player)
    {        

        // 내 플레이어의 NetworkObject를 가져와 로컬인지 확인
        if (player == null || !player.HasInputAuthority) return; // 내가 조종 중인 플레이어가 아니면 false

        // 미션 시작 매니저
        var missionManager = NetworkMissionManager.Instance;
        if (missionManager && missionManager.Object && missionManager.Object.IsValid)
            missionManager.Rpc_StartMission();
    }

    public void OnStopGame(NetworkObject player)
    {
        // 내 플레이어의 NetworkObject를 가져와 로컬인지 확인
        if (player == null || !player.HasInputAuthority) return; // 내가 조종 중인 플레이어가 아니면 false

        var missionManager = NetworkMissionManager.Instance;
        if (missionManager && missionManager.Object && missionManager.Object.IsValid)
            missionManager.RpC_StopMission();
    }
}
