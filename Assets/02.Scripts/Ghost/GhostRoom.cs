using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

public class GhostRoom : NetworkedTriggerEventSupporter
{
    private BoxCollider _roomCollider;
    private MagicCircleCore _activeCircleServer;
  
    [Networked] public NetworkBool IsCircleSpawning { get; set; } // 동시 생성 방지

    void Awake()
    {
        _roomCollider = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        _roomCollider.isTrigger = true;
    }

    /// <summary>
    /// 맵에 생성된 후 필요한 초기화
    /// </summary>
    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            IsCircleSpawning = false;
            _activeCircleServer = null;
        }
    }

    /// <summary>
    /// 해당 위치가 귀신의 방인지 확인
    /// </summary>
    public bool IsInGhostRoom(Vector3 pos)
    {
        if (_roomCollider == null) return false;

        Bounds worldeBounds = _roomCollider.bounds;
        worldeBounds.Expand(0.2f);
        return worldeBounds.Contains(pos);
    }

    /// <summary>
    /// 활성화된 마법진 있는지 찾기
    /// </summary>
    public bool TryGetActiveCircle(out MagicCircleCore circle)
    {
        circle = (_activeCircleServer != null && _activeCircleServer.Object && _activeCircleServer.Object.IsValid) ? _activeCircleServer : null;

        return circle != null;
    }

    public void SetActiveCircle(MagicCircleCore circle)
    {
        if (!Object.HasStateAuthority) return;
        _activeCircleServer = circle;
        IsCircleSpawning = false;
    }

    public void ClearActiveCircle(MagicCircleCore circle)
    {
        if (!Object.HasStateAuthority) return;

        if (_activeCircleServer == circle)
            _activeCircleServer = null;
    }

    void OnDrawGizmosSelected()
    {
        if (_roomCollider == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.matrix = _roomCollider.transform.localToWorldMatrix;
        Gizmos.DrawCube(_roomCollider.center, _roomCollider.size);
    }

}
