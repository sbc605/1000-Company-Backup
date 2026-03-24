using Fusion;
using System.Collections;
using UnityEngine;

public class PlayerReadyState : NetworkBehaviour
{
    [Networked]
    public NetworkBool IsReady { get; set; }

    public bool isOffice = false;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            IsReady = false;
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetReady(bool ready)
    {
        IsReady = ready;
        Debug.Log($"[서버] 플레이어 {Object.InputAuthority.PlayerId} 레디 상태 = {ready}");
    }
}