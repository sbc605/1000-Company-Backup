using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
/// <summary>
/// Local.Spawned로 PlayerName을 할당
/// PlayerRef를 사용해 PlayerName을 받아와야하는 경우엔 전부 사용 가능
/// </summary>
public class PlayerNameSync : NetworkBehaviour
{
    public static event Action<TeamMemberData> OnPlayerRegistered; // 플레이어 정보 등록 이벤트
    public static event Action<string> OnPlayerUnregistered;

    // 네트워크로 동기화되는 이름
    [Networked] public string PlayerName { get; private set; }

    public static PlayerNameSync Local;

    // DisplayName → PlayerNameSync 매핑
    public static Dictionary<string, PlayerNameSync> playerNameSlots = new();

    public override void Spawned()
    {
        // 레지스트리 유지(모든 클라이언트가 PlayerNameSync 등록)
        if (!string.IsNullOrEmpty(PlayerName))
            playerNameSlots[PlayerName] = this;

        // 로컬 플레이어만 이름 요청
        if (Object.HasInputAuthority)
        {
            Local = this;

            // 스팀 닉네임 가져오기 시도
            string nickname = TryGetSteamNickname();

            // 실패하면 기본명사용
            if (string.IsNullOrEmpty(nickname))
                nickname = $"Player {Object.InputAuthority.PlayerId}";

            RPC_RequestSetName(nickname);
        }
    }

    /// <summary>
    /// 클라 -> 서버
    /// StateAuthority에서 값 세팅 후 모든 클라에 전파
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestSetName(string name)
    {
        string uniqueName = MakeUniqueName(name);
        PlayerName = uniqueName;
        RPC_BroadcastName(uniqueName);
    }

    /// <summary>
    /// 서버 -> 클라
    /// 모든 클라가 동일한 순서로 이름 받음
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogWarning("[PlayerNameSync] 이름이 비어 있어 Vital UI 등록 생략");
            return;
        }

        PlayerName = name;
        RegisterName(name);

        // PlayerLevel / PlayerCondition 가져오기
        var playerLevel = GetComponent<PlayerLevel>();
        var condition = GetComponent<PlayerCondition>();

        // 기본값
        int level = 1;
        EGrade grade = EGrade.인턴;
        int mental = 100;

        if (playerLevel != null)
        {
            level = playerLevel.Level;
            grade = GradeHelper.GetGradeByLevel(level);
        }

        if (condition != null)
        {
            // MCurrentSanity를 int로 반올림해서 사용
            mental = Mathf.RoundToInt(condition.CurrentSanity);
        }

        // Home UI 등록 이벤트
        TeamMemberData data = new TeamMemberData()
        {
            nickName = PlayerName,
            grade = grade,
            CurrentMental = mental
        };

        Debug.Log($"[PlayerNameSync] Registered player '{name}' → Vital UI 이벤트 발행");
        OnPlayerRegistered?.Invoke(data);

        // UI 강제 갱신
        VivoxManager.Instance.OnParticipantChangedEvent?.Invoke(VivoxManager.Instance.participantsList);
    }

    private void RegisterName(string name)
    {
        playerNameSlots[name] = this;
    }

    private string TryGetSteamNickname()
    {
#if STEAMWORKS_NET
        try
        {
            if (SteamManager.Initialized)
            {
                return Steamworks.SteamFriends.GetPersonaName();
            }
        }
        catch { }
#endif
        return null;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (playerNameSlots.ContainsKey(PlayerName))
            playerNameSlots.Remove(PlayerName);

        Debug.Log($"[PlayerNameSync] Unregistered player '{PlayerName}' → Vital UI 제거 이벤트 발행");
        OnPlayerUnregistered?.Invoke(PlayerName); // nickName 기준으로 제거 요청
    }

    /// <summary>
    /// 중복 닉네임이 있을 경우 뒤에 _1 이런 식으로 인덱스를 붙여 구분
    /// UI쪽에선 _1 이런 인덱스가 안 보이도록 되어있음
    /// </summary>
    private string MakeUniqueName(string baseName)
    {
        string finalName = baseName;
        int index = 1;

        while (playerNameSlots.ContainsKey(finalName))
        {
            finalName = $"{baseName}_{index}";
            index++;
        }

        return finalName;
    }

}
