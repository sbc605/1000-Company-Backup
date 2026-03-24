using System.Collections;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
public class GhostStateDead : GhostBaseState
{
    private TickTimer _dieTimer;
    private float _dieLength;
    private bool _dissolveStart;

    public GhostStateDead(GhostController ghostController) : base(ghostController) { }

    public override GhostController.EGhostState State => GhostController.EGhostState.Dead;

    public override void EnterState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        if (ghost.Agent)
        {
            ghost.Agent.isStopped = true;
            ghost.Agent.enabled = false;
        }

        ghost.Animator.applyRootMotion = true;
        ghost.Rigidbody.isKinematic = true;
        ghost.Rpc_PlayDeadAnim();

        _dieLength = ghost.GetCurrentStateLength();

        _dieTimer = TickTimer.CreateFromSeconds(ghost.Runner, _dieLength);
        _dissolveStart = false;

        ghost.Sound?.Rpc_PlayOneShot(EGhostSound.DeathOneShot, 0.3f);
    }


    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        if (!_dissolveStart && _dieTimer.Expired(ghost.Runner))
        {
            ghost.FX.BeginDissolve(_dieLength);
            _dissolveStart = true;
            return;
        }

        if (_dissolveStart && ghost.FX.GetDissolveT() >= 0.999f)
            ghost.Disappear();
    }

    public override void ExitState()
    {
        ghost.Animator.applyRootMotion = false;
        ghost.Rigidbody.isKinematic = false;
    }
}
