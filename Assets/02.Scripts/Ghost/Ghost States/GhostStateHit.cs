using Fusion;

// 코드 담당자: 김수아

public class GhostStateHit : GhostBaseState
{
    private TickTimer _hitTimer;

    public GhostStateHit(GhostController ghostController) : base(ghostController) { }

    public override GhostController.EGhostState State => GhostController.EGhostState.Hit;

    public override void EnterState()
    {
        ghost.Agent.isStopped = true;

        ghost.Animator.SetTrigger(GhostAnimParams.GhostHit);

        _hitTimer = TickTimer.CreateFromSeconds(ghost.Runner, ghost.GetCurrentStateLength());
    }

    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        if (_hitTimer.Expired(ghost.Runner))
        {
            // ghost.ChangeState(GhostController.EGhostState.Patrol);
            ghost.Disappear();
        }
    }

    public override void ExitState()
    {
        ghost.ResetAllAnimation();
    }
}
