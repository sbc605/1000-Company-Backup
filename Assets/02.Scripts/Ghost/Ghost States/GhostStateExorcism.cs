using UnityEngine;

// 코드 담당자: 김수아

public class GhostStateExorcism : GhostBaseState
{
    public GhostStateExorcism(GhostController ghostController) : base(ghostController) { }

    public override GhostController.EGhostState State => GhostController.EGhostState.Exorcism;

    public override void EnterState()
    {
        if (!ghost.Object.HasStateAuthority) return;
        ghost.Sound?.Rpc_PlayLoop(EGhostSound.ExorcismLoop, 0.3f);
    }

    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;
    }

    public override void ExitState()
    {
        if (!ghost.Object.HasStateAuthority) return;
        ghost.Sound?.Rpc_StopLoop(1f);
    }
}
