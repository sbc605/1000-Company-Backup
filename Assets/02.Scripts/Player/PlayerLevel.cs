// 코드 담당자: 김수아
using System;
using Fusion;
using UnityEngine;

/// <summary>
/// 레벨에 따른 직급 맵핑
/// 룰에 따라 if 조건만 변경하면 됨
/// </summary>
public static class GradeHelper
{
    public static EGrade GetGradeByLevel(int level)
    {
        if (level < 5) return EGrade.인턴;
        if (level < 10) return EGrade.사원;
        if (level < 20) return EGrade.대리;
        if (level < 30) return EGrade.과장;
        return EGrade.팀장;
    }
}

public class PlayerLevel : NetworkBehaviour
{
    [Networked] public int Level { get; private set; }
    [Networked] public int CurrentExp { get; private set; } // 누적 경험치
    
    public static event Action<string, int> OnLevelChanged;

    private PlayerCondition _playerCondition;

    public override void Spawned()
    {
        _playerCondition = GetComponent<PlayerCondition>();

        // 초기값 설정
        if (Runner.IsServer)
        {
            if (Level <= 0) Level = 1;
            if (CurrentExp < 0) CurrentExp = 0;
        }
    }

    /// <summary>
    /// 경험치 추가 (클라 → 서버)
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_AddExp(int amount)
    {
        if (amount <= 0) return;

        CurrentExp += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int oldLevel = Level;
        int newLevel = CalculateLevelFromExp(CurrentExp);

        if (newLevel != oldLevel)
        {
            Level = newLevel;

            string nickname = _playerCondition != null
                ? _playerCondition.Nickname.ToString()
                : null;

            if (!string.IsNullOrEmpty(nickname))
            {
                OnLevelChanged?.Invoke(nickname, Level);
                Debug.Log($"[PlayerLevel] {nickname} 레벨업: {oldLevel} → {newLevel}");
            }
        }
    }

    // 예시: 100exp당 레벨 1 증가 (나중에 룰에 따라 변경 필요)
    private int CalculateLevelFromExp(int exp)
    {
        return exp / 100 + 1;
    }
}
