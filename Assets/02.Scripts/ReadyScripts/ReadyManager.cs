using UnityEngine;
using Fusion;
using System.Linq;

public class ReadyManager : NetworkBehaviour
{
    [Networked]
    public int ReadyCount { get; private set; }

    [Networked]
    public int TotalCount { get; private set; }

    public static ReadyManager Instance { get; private set; }

    public override void Spawned()
    {
        base.Spawned(); 
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Runner.Despawn(Object);
        }
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        int currentReadyCount = 0;
        var activePlayers = Runner.ActivePlayers;
        TotalCount = activePlayers.Count();

        foreach (var player in activePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out var playerObj) &&
                playerObj.TryGetComponent<PlayerReadyState>(out var readyState))
            {
                if (readyState.IsReady)
                {
                    currentReadyCount++;
                }
            }
        }
        
        ReadyCount = currentReadyCount;
    }

}