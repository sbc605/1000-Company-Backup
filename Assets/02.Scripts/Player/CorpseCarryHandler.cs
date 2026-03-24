// 코드 담당자 : 최서영
using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

/// <summary>
/// 플레이어가 죽은 다른 플레이어를 업어서 옮기는 기능을 담당.
/// - 상태(죽었냐, 들려 있냐)는 PlayerCondition이 가짐
/// - 이 스크립트는 살아있는 플레이어가 시체를 들고 다니는 행위만 담당
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerCondition))]
public class CorpseCarryHandler : NetworkBehaviour
{
    [SerializeField] private Transform carryPoint; // 시체를 붙일 위치

    private PlayerCondition _playerCondition;

    // 플레이어가 들고 있는 시체(죽은 PlayerCondition이 붙어 있는 NetworkObject)
    [Networked] public NetworkObject CarriedCorpse { get; set; }

    public bool IsCarryingCorpse => CarriedCorpse != null;

    private InventoryManager _inventory;
    private bool _prevIsCarrying = false;

    public override void Spawned()
    {
        _playerCondition = GetComponent<PlayerCondition>();

        _inventory = GetComponent<InventoryManager>();

        if (Runner.IsServer)
        {
            CarriedCorpse = null;
        }

        if (carryPoint == null)
        {
            Debug.Log($"[CorpseCarryHandler] {name} 의 carryPoint가 설정되지 않았습니다.");
        }

        _prevIsCarrying = IsCarryingCorpse;
        ApplyEquipBlock(IsCarryingCorpse);
    }

    public override void Render()
    {
        // 네트워크로 동기화된 IsCarryingCorpse 값이 바뀌었는지 체크
        bool nowCarrying = IsCarryingCorpse;

        if (nowCarrying != _prevIsCarrying)
        {
            _prevIsCarrying = nowCarrying;
            ApplyEquipBlock(nowCarrying);
        }
    }

    /// <summary>
    /// 시체 내려놓기 요청
    /// 드랍 키 입력에서 이 메서드를 호출
    /// </summary>
    public void TryRequestDrop(Vector3 dropPosition)
    {
        if (!Object || !Object.HasInputAuthority)
            return;

        if (!IsCarryingCorpse)
            return;

        // 들고 있는지 여부는 서버에서 다시 검사
        RPC_RequestStopCarry(dropPosition);
    }


    #region 시체 운반 멀티 적용
    public void StartCarry(NetworkObject corpseObject)
    {
        // 서버만 처리
        if (!Object || !Object.HasStateAuthority) return;

        if (_playerCondition == null)
            _playerCondition = GetComponent<PlayerCondition>();

        // 내가 시체이면 안 됨
        if (_playerCondition.IsDead) return;

        // 이미 다른 시체 들고 있으면 안 됨
        if (IsCarryingCorpse) return;

        // 대상이 실제로 죽은 상태인지, 이미 누가 들고 있는지 체크
        if (corpseObject == null) return;

        var corpseCondition = corpseObject.GetComponent<PlayerCondition>();
        if (corpseCondition == null)
            return;

        if (!corpseCondition.IsDead || corpseCondition.IsBeingCarried) return;

        // 상태 세팅
        CarriedCorpse = corpseObject;
        corpseCondition.IsBeingCarried = true;
        corpseCondition.Carrier = Object;   // PlayerCondition 쪽 Networked NetworkObject

        // 애니메이션 적용
        // 시체 업은 사람
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Debug.Log($"[Corpse] 시체 들기 true");
            playerController.CarriedPlayerId = corpseObject.Id;
            playerController.IsCarrying = true;
        }

        // 시체
        var corpseController = corpseObject.GetComponent<PlayerController>();
        if (corpseController != null)
        {
            // Debug.Log("$[Corpse] 시체 애니메이션 적용");
            corpseController.IsBeingCarried = true;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestStopCarry(Vector3 dropPosition)
    {
        if (!Object || !Object.HasStateAuthority)
            return;

        if (!IsCarryingCorpse)
            return;

        var corpseCondition = CarriedCorpse.GetComponent<PlayerCondition>();
        ClearCorpseLink(corpseCondition, dropPosition);
    }
    #endregion

    public override void FixedUpdateNetwork()
    {
        if (!Object || !Object.HasStateAuthority)
            return;

        if (!IsCarryingCorpse)
            return;

        if (carryPoint == null)
            return;

        var corpseCondition = CarriedCorpse.GetComponent<PlayerCondition>();

        if (corpseCondition == null)
        {
            // 레퍼런스 깨짐
            CarriedCorpse = null;
            return;
        }

        // 시체가 이제 더 이상 죽은 상태가 아니거나, 들려있지 않다고 표시되면 링크 해제
        if (!corpseCondition.IsDead || !corpseCondition.IsBeingCarried || corpseCondition.Carrier != Object)
        {
            ClearCorpseLink(corpseCondition);
            return;
        }

        // 실제 위치/회전 붙이기 (서버 기준)
        corpseCondition.transform.position = carryPoint.position;
        corpseCondition.transform.rotation = carryPoint.rotation;
    }

    private void ClearCorpseLink(PlayerCondition corpseCondition)
    {
        // 그냥 현재 위치에 냅두고 링크만 끊고 싶을 때
        Vector3 pos = corpseCondition != null
            ? corpseCondition.transform.position
            : transform.position;

        ClearCorpseLink(corpseCondition, pos);
    }

    /// <summary>
    /// 시체 상태 초기화
    /// 업은 사람 쪽 상태 초기화
    /// => 누가 누구를 들고 있다는 상태가 꼬이지 않도록 하기 위함
    /// </summary>
    private void ClearCorpseLink(PlayerCondition corpseCondition, Vector3 dropPosition)
    {
        // 시체 상태 정리
        if (corpseCondition != null)
        {
            corpseCondition.IsBeingCarried = false;
            corpseCondition.Carrier = null;

            // 시체 애니메이션 끄기
            var corpseController = corpseCondition.GetComponent<PlayerController>();
            if (corpseController != null)
            {
                corpseController.IsBeingCarried = false;
            }
        }

        // 업은사람 상태 정리
        var playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.IsCarrying = false;
            playerController.CarriedPlayerId = default;
        }

        CarriedCorpse = null;
    }

    /// <summary>
    /// 현재 시체 들고 있는 상태에 따라, 인벤토리 손 사용 가능 여부를 갱신
    /// </summary>
    private void ApplyEquipBlock(bool isCarrying)
    {
        if (_inventory == null)
            return;

        // 시체 들고 있으면 손 막기, 아니면 풀기
        _inventory.SetEquipBlocked(isCarrying);
    }

    public void ForceDrop(Vector3 dropPosition)
    {
        if (!Object.HasStateAuthority) return;
        if (!IsCarryingCorpse) return;

        var corpseCondition = CarriedCorpse.GetComponent<PlayerCondition>();
        ClearCorpseLink(corpseCondition, dropPosition);
    }
}
