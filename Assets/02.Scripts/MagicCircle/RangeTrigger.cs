using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

public class RangeTrigger : NetworkedTriggerEventSupporter
{
    public event Action<int> OnEnterServer;
    public event Action<int> OnExitServer;
    // 로컬 플레이어가 들어오면 true, 나가면 false
    public event Action<bool> OnLocalToggle;
    // 아이템 감지
    public event Action<int> OnItemEnterServer;

    public int CurrentCount => playerInside.Count;
    private HashSet<int> playerInside = new();

    private Collider _trigger;
    private bool IsServer => Object && Object.HasStateAuthority;

    public bool LocalPlayerInside { get; private set; }


    private void OnEnable()
    {
        _trigger = GetComponent<Collider>();
        if (_trigger) _trigger.isTrigger = true;
    }

    private bool IsLocalPlayer(Collider other)
    {
        var obj = other.GetComponent<NetworkObject>();
        if (obj != null && obj.IsValid && Object != null && Object.Runner != null)
        {
            return obj.InputAuthority == Object.Runner.LocalPlayer;
        }

        return false;
    }

    private int GetPlayerId(Collider other)
    {
        return other.transform.root.gameObject.GetInstanceID();
    }

    #region Player 감지
    protected override void OnTargetEnter(Collider other)
    {
        int id = GetPlayerId(other);

        if (playerInside.Contains(id)) return;
        playerInside.Add(id);

        // 서버 집계
        if (IsServer)
            OnEnterServer?.Invoke(id);

        // 클라 로컬 UI 토글
        if (IsLocalPlayer(other))
        {
            LocalPlayerInside = true;
            OnLocalToggle?.Invoke(true);
        }
    }

    protected override void OnTargetExit(Collider other)
    {
        int id = GetPlayerId(other);
        if (!playerInside.Contains(id)) return;

        playerInside.Remove(id);

        // 서버 집계
        if (IsServer)
            OnExitServer?.Invoke(id);

        // 클라 로컬 UI 토글
        if (IsLocalPlayer(other))
        {
            LocalPlayerInside = false;
            OnLocalToggle?.Invoke(false);
        }
    }

    public void CheckLocalPlayerInsideOnStart()
    {
        if (_trigger == null) return;

        var cols = Physics.OverlapBox(_trigger.bounds.center, _trigger.bounds.extents, Quaternion.identity);

        foreach (var col in cols)
        {
            if (IsLocalPlayer(col))
            {
                LocalPlayerInside = true;
                OnLocalToggle?.Invoke(true);
                Debug.Log("[RangeTrigger] 초기 상태: 로컬 플레이어가 이미 트리거 안에 있음");
                return;
            }
        }
        LocalPlayerInside = false;
        OnLocalToggle?.Invoke(false);
    }

    #endregion

    #region 아이템 감지
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var io = other.GetComponent<ItemObject>();
        if (io && io.itemData)
        {
            int itemId = io.NetworkItemID;
            if (itemId >= 0)
                OnItemEnterServer?.Invoke(itemId);

            Debug.Log($"[TriggerEnter] 현재 있는 아이템 감지: ID={io.NetworkItemID}");

            return;

        }
    }
    /// <summary>
    /// 마법진 시작시, 기존에 범위 내에 아이템 있으면 스캔 후 서버로 전달
    /// </summary>
    public void Server_ScanExorcismItems()
    {
        if (!IsServer) return;

        var cols = Physics.OverlapBox(_trigger.bounds.center, _trigger.bounds.extents, Quaternion.identity);

        foreach (var col in cols)
        {
            if (!col || !col.gameObject.activeInHierarchy) continue;

            var io = col.GetComponent<ItemObject>();
            if (io && io.itemData)
            {
                OnItemEnterServer?.Invoke(io.NetworkItemID);

                Debug.Log($"[RangeTrigger] 현재 있는 아이템 감지: ID={io.NetworkItemID}");
            }
        }
    }
    #endregion
}
