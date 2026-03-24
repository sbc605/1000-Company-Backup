using System.Collections;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

public class GhostStateIdle : GhostBaseState
{
    private TickTimer _idleTimer;

    public GhostStateIdle(GhostController ghostController) : base(ghostController) { }

    public override GhostController.EGhostState State => GhostController.EGhostState.Idle;

    public override void EnterState()
    {
        ghost.Animator.SetBool(GhostAnimParams.GhostIdle, true);
        ghost.Animator.SetBool(GhostAnimParams.GhostWalk, false);

        float duration = Random.Range(3f, 5f);
        _idleTimer = TickTimer.CreateFromSeconds(ghost.Runner, duration);
        ghost.Agent.isStopped = true;
    }

    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        // 1. 플레이어를 찾으면 Chase 상태로 전환
        if (ghost.FindPlayerRegisteredPlayer(out Transform player))
        {
            ghost.TargetPlayer = player;
            ghost.ChangeState(GhostController.EGhostState.Chase);
            return;
        }

        if (GhostSpawner.Instance.ExorcismState != GhostSpawner.EExorcismState.Failed)
        {
            // 일정 시간 동안 플레이어를 찾지 못하면 사라짐
            if (_idleTimer.Expired(ghost.Runner))
            {
                ghost.Disappear();
                return;
            }
        }
    }

    public override void ExitState()
    {
        ghost.ResetAllAnimation();
    }
}
