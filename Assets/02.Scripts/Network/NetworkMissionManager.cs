using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
// 의뢰맵에서 플레이어가 문을 넘을 때부터 귀신 소환을 시작합니다.
public class NetworkMissionManager : NetworkBehaviour
{
    [Networked] public bool IsMissionStarted { get; private set; }
    [Networked] private int InsideCount { get; set; } // 맵 내부 인원수
    public static NetworkMissionManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_StartMission()
    {
        InsideCount++;

        if (!IsMissionStarted && InsideCount > 0)
        {
            IsMissionStarted = true;
            Debug.Log("미션 시작");
        }
    }

    // === 종료/씬 전환 시 호출하는 정리 RPC ===
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RpC_StopMission()
    {
        if (!IsMissionStarted && InsideCount == 0) return;

        IsMissionStarted = false;
        InsideCount = 0;
        Debug.Log("미션 종료: 모든 플레이어 퇴장");
    }
}