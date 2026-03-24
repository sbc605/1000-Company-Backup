using System;
using UnityEngine;

// 코드 담당자: 김수아

public abstract class GhostBaseState
{
    protected readonly GhostController ghost;
    public abstract GhostController.EGhostState State { get; }

    protected GhostBaseState(GhostController ghostController)
    {
        ghost = ghostController;
    }

    public abstract void EnterState();
    public abstract void ExecuteState();
    public abstract void ExitState();
}
