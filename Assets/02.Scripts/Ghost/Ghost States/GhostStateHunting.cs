using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chase와 유사하지만 플레이어를 놓치면 사라지는 대신 Patrol 상태로 전환
/// </summary>
public class GhostStateHunting : GhostBaseMoveState
{
    private float _lostTimer;
    private float _lostDelay = 1f;

    public override GhostController.EGhostState State => GhostController.EGhostState.Hunting;

    public GhostStateHunting(GhostController ghost) : base(ghost) { }

    public override void EnterState()
    {
        if (!ghost.Object.HasStateAuthority) return;
        _lostTimer = 0f;

        if (ghost.Agent && ghost.Agent.enabled)
        {
            ghost.Agent.isStopped = false;
            ghost.Agent.speed = ghost.GhostMoveSpeed * 2f;
        }

        ghost.Sound?.Rpc_PlayLoop(EGhostSound.ChaseLoop);
        ghost.Animator.ResetTrigger(GhostAnimParams.GhostExorcism);
        ghost.Animator.SetBool(GhostAnimParams.GhostWalk, true);

        SetRandomDestination();
    }

    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        if (ghost.FindPlayerRegisteredPlayer(out Transform found))
        {
            ghost.TargetPlayer = found;

            ghost.ChangeState(GhostController.EGhostState.Chase);
            return;
        }

        _lostTimer += ghost.Runner.DeltaTime;
        if (_lostTimer >= _lostDelay)
        {
            ghost.ChangeState(GhostController.EGhostState.Patrol);
            return;
        }
    }

    public override void ExitState()
    {
        ghost.Sound?.Rpc_StopLoop();
    }
}

