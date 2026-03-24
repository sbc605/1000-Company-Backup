using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

/// <summary>
/// 동/서/남/북 트리거 4개 + 범위 트리거 1개 관리
/// 서버에서만 계산 진행
/// </summary>
public class MagicCircleDetector : MonoBehaviour
{
    public event Action<int, bool> Server_OnSectorChanged;

    [Header("Setup")]
    [SerializeField, Range(1, 4)] private int requiredSectors = 1;

    [Header("Triggers")]
    [SerializeField] private SectorTrigger north;
    [SerializeField] private SectorTrigger east;
    [SerializeField] private SectorTrigger south;
    [SerializeField] private SectorTrigger west;
    [SerializeField] private RangeTrigger rangeTrigger;

    private readonly HashSet<int> _north = new();
    private readonly HashSet<int> _east = new();
    private readonly HashSet<int> _south = new();
    private readonly HashSet<int> _west = new();

    public int ApproachSectorCount { get; private set; } = 0;
    public bool IsSectorRequired { get; private set; } = false;

    private NetworkObject _netObj;

    void Awake()
    {
        _netObj = GetComponentInParent<NetworkObject>();

        // 섹터 트리거 콜백 묶기
        BindSector(north, _north);
        BindSector(east, _east);
        BindSector(south, _south);
        BindSector(west, _west);
    }

    private void BindSector(SectorTrigger trig, HashSet<int> bucket)
    {
        if (!trig) return;
        trig.OnEnterServer += (id) => { bucket.Add(id); RecalcSectors(); };
        trig.OnExitServer += (id) => { bucket.Remove(id); RecalcSectors(); };
    }

    private bool IsServer => _netObj && _netObj.HasStateAuthority;

    private void RecalcSectors()
    {
        if (!IsServer) return;
        int count = 0;
        if (_north.Count > 0) count++;
        if (_east.Count > 0) count++;
        if (_south.Count > 0) count++;
        if (_west.Count > 0) count++;

        ApproachSectorCount = count;
        IsSectorRequired = count >= Mathf.Clamp(requiredSectors, 1, 4);
        Server_OnSectorChanged?.Invoke(ApproachSectorCount, IsSectorRequired);
    }
}
