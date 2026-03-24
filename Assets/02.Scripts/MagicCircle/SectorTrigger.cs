using System;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

public class SectorTrigger : NetworkedTriggerEventSupporter
{
    public event Action<int> OnEnterServer;
    public event Action<int> OnExitServer;

    private bool IsServer => Object && Object.HasStateAuthority;

    private int GetPlayerId(Collider other)
    {
        var root = other.transform.root;
        return root.gameObject.GetInstanceID();
    }

    protected override void OnTargetEnter(Collider other)
    {
        int id = GetPlayerId(other);

        OnEnterServer?.Invoke(id);
    }

    protected override void OnTargetExit(Collider other)
    {
        int id = GetPlayerId(other);

        OnExitServer?.Invoke(id);
    }
}
