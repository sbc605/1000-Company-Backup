using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class SeatManager : NetworkBehaviour
{
    [SerializeField] GameObject playerSlotPrefab;
    [SerializeField] Transform playerSlotParent;
    [SerializeField] List<MonitorInteract> seats;

    Dictionary<PlayerRef, PlayerSlotUi> playerSlots = new();

    public static SeatManager Instance { get; private set; }

    private void Start()
    {
        SoundManager.Instance.BgmSoundPlay("office");
    }

    public override void Spawned()
    {
        Instance = this;

        seats = new List<MonitorInteract>(
            FindObjectsByType<MonitorInteract>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));

        Debug.Log("SeatManager Spawned - 좌석 재수집 완료");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region 좌석 할당 (서버 전용)
    public void AssignSeat(PlayerRef player)
    {
        if (!Runner.IsServer) return;

        foreach (var seat in seats)
        {
            if (!seat.Object)
                continue;

            if (seat.AssignedPlayer == default)
            {
                seat.AssignPlayer(player);
                Debug.Log($"플레이어 자리 배정 완료");
                return;
            }
        }
    }

    public void UnassignSeat(PlayerRef player)
    {
        if (!Runner.IsServer) return;
        foreach (var seat in seats)
        {
            if (seat.AssignedPlayer == player)
            {
                seat.AssignPlayer(default);
                return;
            }
        }
    }
    #endregion

    #region UI 생성/삭제 (RPC + 로컬)

    public void RemovePlayerUI(PlayerRef player)
    {
        if (playerSlots.TryGetValue(player, out var slot))
        {
            Debug.Log($"[SeatManager] Player {player.PlayerId} 슬롯 제거");
            Destroy(slot.gameObject);
            playerSlots.Remove(player);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SyncExistingSlots_Filtered(PlayerRef target, PlayerRef[] existingPlayers)
    {
        if (Runner.IsServer || Runner.LocalPlayer != target)
        {
            return;
        }
        foreach (var p in existingPlayers)
        {
            if (!playerSlots.ContainsKey(p))
            {
                CreateSlotInternal(p, Runner);
                Debug.Log($"[SeatManager] 기존 슬롯 동기화: Player {p.PlayerId}");
            }
        }
    }

    public void CreatePlayerUI_Server(PlayerRef player)
    {
        if (!Runner.IsServer) return;
        // if (player == Runner.LocalPlayer) return;
        RPC_CreatePlayerUI(player);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CreatePlayerUI(PlayerRef player)
    {
        if (playerSlots.ContainsKey(player)) return;
        CreateSlotInternal(player, Runner);
    }

    public void CreateLocalSlot(PlayerRef player, NetworkRunner runner)
    {
        if (playerSlots.ContainsKey(player)) return;
        CreateSlotInternal(player, runner);
    }

    private void CreateSlotInternal(PlayerRef player, NetworkRunner runner)
    {
        GameObject ui = Instantiate(playerSlotPrefab, playerSlotParent);
        var slot = ui.GetComponent<PlayerSlotUi>();
        slot.Init(player, runner);
        int siblingIndex = Mathf.Max(0, player.PlayerId - 1);
        ui.transform.SetSiblingIndex(siblingIndex);
        playerSlots[player] = slot;
        Debug.Log($"[SeatManager] 슬롯 생성 완료: P{player.PlayerId}");
    }
    #endregion
}