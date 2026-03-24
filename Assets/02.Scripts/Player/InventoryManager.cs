//코드 담당자: 유호정
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using System.Collections.Generic;
using System.Collections;

public class InventoryManager : NetworkBehaviour
{
    [Header("Settings")]
    public int inventorySize = 3;
    public string viewModelLayer = "Inventory";

    [Header("View Model")]
    public GameObject viewModelHandsObject;

    [Header("Dependencies")]
    public Transform viewModelSocket;
    public Transform thirdPersonSocket;
    public Transform dropPosition;

    [Networked, Capacity(3)] public NetworkArray<NetworkInventorySlot> SyncedSlots { get; }
    [Networked] public int CurrentSlotIndex { get; set; }
    private GameObject[] _viewModelInstances;
    private GameObject[] _thirdPersonInstances;
    private NetworkInventorySlot[] _localSlotCache;
    private int _previousSlotIndex = -1;

    private Animator playerAnimator;
    private bool isPaused = false;
    private ItemPlacer itemPlacer;

    private bool isScrollPaused = false;
    public void SetScrollPaused(bool paused) => isScrollPaused = paused;

    // 변수 추가 : 정하윤
    private bool _previousLampState = false;
    [Networked]
    public NetworkBool NetworkedLampState { get; set; }

    // 변수 추가 : 최서영 > 무당방울 멀티용
    [Networked] public int MudangBellRingCount { get; set; }
    private int _prevMudangBellRingCount; // 새로운 입력인지 확인용

    // 변수 추가 : 최서영 > 아이템 사용/줍기 가능 여부
    private bool _isEquipBlocked = false;

    void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        itemPlacer = GetComponent<ItemPlacer>();


        _viewModelInstances = new GameObject[inventorySize];
        _thirdPersonInstances = new GameObject[inventorySize];
        _localSlotCache = new NetworkInventorySlot[inventorySize];
    }


    public override void Spawned()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            _localSlotCache[i] = new NetworkInventorySlot { ItemID = -1 };
        }


        RefreshAllSlots(true);
        UpdateHeldItemVisuals(_previousSlotIndex, CurrentSlotIndex);

        // 코드 추가 : 정하윤
        _previousLampState = NetworkedLampState;
        ApplyLampVisuals(_previousLampState);
    }


    public override void Render()
    {

        if (_previousSlotIndex != CurrentSlotIndex)
        {
            UpdateHeldItemVisuals(_previousSlotIndex, CurrentSlotIndex);
        }

        RefreshAllSlots(false);

        // 코드 추가 : 정하윤
        if (_previousLampState != NetworkedLampState)
        {
            ApplyLampVisuals(NetworkedLampState);
            _previousLampState = NetworkedLampState;
        }

        // 코드 추가 : 최서영
        if (_prevMudangBellRingCount != MudangBellRingCount)
        {
            _prevMudangBellRingCount = MudangBellRingCount;
            ApplyMudangBell();
        }
    }


    public void OnSwitchItem(InputValue value)
    {
        if (isPaused || isScrollPaused || !HasInputAuthority) return;

        Vector2 scrollVector = value.Get<Vector2>();
        float scrollValue = scrollVector.y;

        int newIndex = CurrentSlotIndex;
        if (scrollValue > 0) newIndex--;
        else if (scrollValue < 0) newIndex++;

        if (newIndex >= inventorySize) newIndex = 0;
        if (newIndex < 0) newIndex = inventorySize - 1;

        if (newIndex != CurrentSlotIndex)
        {
            RPC_SetCurrentSlotIndex(newIndex);
        }
    }

    public void OnDropItem(InputValue value)
    {
        if (isPaused || !HasInputAuthority) return;

        // 수정 : 최서영
        // 시체 내려놓기
        var corpseHandler = GetComponent<CorpseCarryHandler>();
        if (corpseHandler != null && corpseHandler.IsCarryingCorpse)
        {
            Vector3 dropPos = dropPosition.position;

            corpseHandler.TryRequestDrop(dropPos);
            return; // 시체만 내려놓고 아이템 드랍되지 않도록
        }

        Vector3 pos = dropPosition.position;
        Quaternion rot = dropPosition.rotation;
        Debug.Log($"[클라이언트] 아이템 드랍 시도. dropPosition: {pos}");
        RPC_DropItem(CurrentSlotIndex, pos, rot);
    }

    public void OnUseItem(InputValue value)
    {
        if (isPaused || !HasInputAuthority) return;

        // 추가 : 최서영
        // 손 사용이 막혀 있으면 아이템 사용 금지
        if (_isEquipBlocked) return;

        NetworkInventorySlot currentSlot = SyncedSlots[CurrentSlotIndex];
        if (currentSlot.IsEmpty()) return;

        ItemData data = ItemDatabase.GetItemDataFromID(currentSlot.ItemID);
        if (data == null) return;

        // 코드 추가 : 정하윤
        GameObject viewModel = _viewModelInstances[CurrentSlotIndex];

        // if 문 추가 : 정하윤
        if (data.itemName == "Lamp" && viewModel.TryGetComponent<IUsable>(out var usableItem))
        {
            usableItem.Use();
        }
        else if (data.itemName == "MudangBell")
        {
            RPC_RequestRingMudangBell();
        }
        else if (data.isInstallable && data.installPrefab != null)
        {
            itemPlacer.AttemptInstall(currentSlot.ItemID, currentSlot.UseCount);
        }
        else
        {
            RPC_UseItem(CurrentSlotIndex);
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetCurrentSlotIndex(int newIndex)
    {
        if (newIndex >= 0 && newIndex < inventorySize)
        {
            CurrentSlotIndex = newIndex;
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_DropItem(int slotIndex, Vector3 dropPos, Quaternion dropRot)
    {
        Debug.Log($"[서버] RPC 실행. 서버가 인식하는 dropPosition: {dropPos}");
        if (slotIndex < 0 || slotIndex >= inventorySize) return;
        NetworkInventorySlot slotToDrop = SyncedSlots[slotIndex];
        if (slotToDrop.IsEmpty()) return;

        ItemData data = ItemDatabase.GetItemDataFromID(slotToDrop.ItemID);
        if (data == null || data.dropPrefab == null) return;



        Debug.Log($"[서버] 최종 스폰 위치: {dropPos}");

        NetworkObject droppedItemObj = Runner.Spawn(
            data.dropPrefab,
            dropPos,
            dropRot
        );


        if (droppedItemObj.TryGetComponent<ItemObject>(out var itemObject))
        {
            itemObject.NetworkUseCount = slotToDrop.UseCount;
        }


        SyncedSlots.Set(slotIndex, new NetworkInventorySlot());
        Debug.Log($"서버: {data.itemName}을 버렸습니다.");
    }


    // [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Server_TryPickUpItem(NetworkId itemNetworkId)
    {
        // 추가 : 최서영
        // 손 사용이 막혀 있으면 아이템 사용 금지
        if (_isEquipBlocked) return;

        if (Runner.TryFindObject(itemNetworkId, out var itemNetworkObject) == false) return;
        if (!itemNetworkObject.TryGetComponent<ItemObject>(out var itemObject)) return;

        ItemData data = itemObject.itemData;
        if (data == null) return;        


        for (int i = 0; i < SyncedSlots.Length; i++)
        {
            if (SyncedSlots[i].IsEmpty())
            {

                SyncedSlots.Set(i, new NetworkInventorySlot
                {
                    ItemID = data.itemID,
                    UseCount = itemObject.NetworkUseCount
                });

                //추가 : 최은주 
                //고스트 아이템을 먹은 경우 찾은 상태로 만들어줌
                if (itemObject.TryGetComponent<GhostItem>(out var ghostItem))
                {
                    ghostItem.OnChangedState(true);
                    Debug.Log("귀신 아이템 찾음");
                }

                Runner.Despawn(itemNetworkObject);
                Debug.Log($"서버: {data.itemName} 획득");
                return;
            }
        }

    
        Debug.Log("서버: 인벤토리 가득 참");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_UseItem(int slotIndex)
    {
        // 추가 : 최서영
        // 손 사용이 막혀 있으면 아이템 사용 금지
        if (_isEquipBlocked) return;

        if (slotIndex < 0 || slotIndex >= inventorySize) return;
        NetworkInventorySlot currentSlot = SyncedSlots[slotIndex];
        if (currentSlot.IsEmpty() || currentSlot.UseCount <= 0) return;

        ItemData data = ItemDatabase.GetItemDataFromID(currentSlot.ItemID);
        if (data == null || data.isInstallable) return;


        RPC_OnItemUsed(slotIndex);

        if (currentSlot.UseCount < 99)
        {
            currentSlot.UseCount--;
        }

        if (currentSlot.UseCount <= 0)
        {
            currentSlot.Clear();
        }


        SyncedSlots.Set(slotIndex, currentSlot);
    }



    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnItemUsed(int slotIndex)
    {

        if (slotIndex != CurrentSlotIndex) return;
        if (!HasInputAuthority) return;

        GameObject viewModel = _viewModelInstances[slotIndex];
        if (viewModel != null && viewModel.TryGetComponent<IUsable>(out var usableItem))
        {
            usableItem.Use();
        }
    }


    private void RefreshAllSlots(bool isFirstSpawn)
    {
        for (int i = 0; i < inventorySize; i++)
        {
            NetworkInventorySlot netSlot = SyncedSlots[i];
            NetworkInventorySlot localSlot = _localSlotCache[i];

            if (isFirstSpawn || netSlot.ItemID != localSlot.ItemID)
            {
                if (_viewModelInstances[i] != null) Destroy(_viewModelInstances[i]);
                if (_thirdPersonInstances[i] != null) Destroy(_thirdPersonInstances[i]);

                bool isActiveSlot = (i == CurrentSlotIndex) && !_isEquipBlocked;

                if (!netSlot.IsEmpty())
                {
                    ItemData data = ItemDatabase.GetItemDataFromID(netSlot.ItemID);
                    if (data != null && data.itemPrefab != null)
                    {
                        _thirdPersonInstances[i] = Instantiate(data.itemPrefab, thirdPersonSocket);
                        _thirdPersonInstances[i].transform.localPosition = Vector3.zero;
                        _thirdPersonInstances[i].transform.localRotation = Quaternion.identity;
                        SetLayerRecursively(_thirdPersonInstances[i], thirdPersonSocket.gameObject.layer);
                        _thirdPersonInstances[i].SetActive(isActiveSlot);


                        if (HasInputAuthority)
                        {
                            _viewModelInstances[i] = Instantiate(data.itemPrefab, viewModelSocket);
                            _viewModelInstances[i].transform.localPosition = Vector3.zero;
                            _viewModelInstances[i].transform.localRotation = Quaternion.identity;
                            SetLayerRecursively(_viewModelInstances[i], LayerMask.NameToLayer(viewModelLayer));
                            SetShadowsRecursively(_viewModelInstances[i], UnityEngine.Rendering.ShadowCastingMode.Off);
                            _viewModelInstances[i].SetActive(isActiveSlot);

                            if (_viewModelInstances[i].TryGetComponent<IUsable>(out var usableScript))
                            {
                                usableScript.GetType().GetMethod("Initialize")?.Invoke(usableScript, new object[] { this, data });
                            }
                            _viewModelInstances[i].SetActive(isActiveSlot);//
                        }
                    }
                }


                _localSlotCache[i] = netSlot;
                if (isActiveSlot)
                {
                    UpdateAnimatorAndHands();
                }
            }
        }
    }


    private void UpdateHeldItemVisuals(int oldIndex, int newIndex)
    {

        if (oldIndex >= 0 && oldIndex < inventorySize)
        {
            if (_thirdPersonInstances[oldIndex] != null) _thirdPersonInstances[oldIndex].SetActive(false);
            if (HasInputAuthority && _viewModelInstances[oldIndex] != null) _viewModelInstances[oldIndex].SetActive(false);
        }

        // 손 사용이 막혀있으면 새 슬롯도 안 보이게
        bool showNew = !_isEquipBlocked;

        if (newIndex >= 0 && newIndex < inventorySize)
        {
            if (_thirdPersonInstances[newIndex] != null) _thirdPersonInstances[newIndex].SetActive(showNew);
            if (HasInputAuthority && _viewModelInstances[newIndex] != null) _viewModelInstances[newIndex].SetActive(showNew);
        }

        UpdateAnimatorAndHands();

        _previousSlotIndex = newIndex;
    }
    private void UpdateAnimatorAndHands()
    {

        NetworkInventorySlot currentSlot = SyncedSlots[CurrentSlotIndex];
        ItemData data = currentSlot.IsEmpty() ? null : ItemDatabase.GetItemDataFromID(currentSlot.ItemID);


        if (playerAnimator != null)
        {
            int animType = (data == null ? 0 : data.animationID);

            // 손이 막혀 있으면 애니메이션은 “맨손”처럼
            if (_isEquipBlocked)
                animType = 0;

            playerAnimator.SetInteger("EquippedItemType", animType);
        }


        if (viewModelHandsObject != null && HasInputAuthority)
        {
            bool hasItemInSlot = !currentSlot.IsEmpty();

            // 손이 막혀 있으면 Hand 모델 자체를 꺼버리기
            viewModelHandsObject.SetActive(hasItemInSlot && !_isEquipBlocked);
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_PlaceItem(int itemID, int useCount, Vector3 position, Quaternion rotation, int slotIndex)
    {
        Debug.Log($"[서버] RPC 수신: Pos={position}, Rot={rotation.eulerAngles}");

        if (slotIndex < 0 || slotIndex >= SyncedSlots.Length || SyncedSlots[slotIndex].ItemID != itemID)
        {
            Debug.LogWarning("서버: 유효하지 않은 아이템 설치 요청입니다.");
            return;
        }

        ItemData data = ItemDatabase.GetItemDataFromID(itemID);
        if (data == null || data.installPrefab == null)
        {
            Debug.LogError($"서버: ItemID {itemID}의 installPrefab이 없습니다.");
            return;
        }

        // 코드 추가 : 정하윤
        PlayerRef caller = Object.InputAuthority;
        NetworkObject installedItemObj = Runner.Spawn(data.installPrefab, position, rotation, caller);

        if (installedItemObj.TryGetComponent<ItemObject>(out var itemObject))
        {
            itemObject.NetworkUseCount = useCount;
        }

        // 제령 아이템인 경우 즉시 사용 처리(코드 추가: 김수아)
        if (installedItemObj.TryGetComponent<ExorcismItemBase>(out var exorcismItem))
        {
            Debug.Log($"[서버] 제령 아이템 감지: {data.itemName}, 위치={position}");
            // 스폰 후 프레임 대기를 위해 코루틴 사용
            StartCoroutine(ExorcismItemRoutine(exorcismItem, position, itemID));
        }

        //귀신 아이템인 경우 설치 여부 판단(코드 추가: 최은주)
        if(installedItemObj.TryGetComponent<GhostItem>(out var ghostItem))
        {
            Debug.Log("귀신 아이템 설치됨");
            ghostItem.GetInstallValue(true);
        }

        SyncedSlots.Set(slotIndex, new NetworkInventorySlot());

        Debug.Log($"서버: {data.itemName} 설치 완료.");
    }

    // 스폰 후 1프레임 대기 후 처리(코드 추가: 김수아)
    private IEnumerator ExorcismItemRoutine(ExorcismItemBase exorcismItem, Vector3 pos, int itemId)
    {
        yield return null; // 1프레임 대기

        if (exorcismItem != null && exorcismItem.Object != null && exorcismItem.Object.IsValid)
        {
            Debug.Log($"[서버] 제령 아이템 사용 처리 시작: itemId={itemId}");
            exorcismItem.Server_ProcessUse(pos, itemId);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= inventorySize) return;
        if (SyncedSlots[slotIndex].IsEmpty()) return;

        SyncedSlots.Set(slotIndex, new NetworkInventorySlot());
        Debug.Log($"서버: 슬롯 {slotIndex} 비워짐");
    }

    public void DropAllItems()
    {

        if (!Runner.IsServer)
        {
            Debug.LogWarning("Server_DropAllItems는 서버/호스트만 호출할 수 있습니다.");
            return;
        }

        Debug.Log($"서버: 플레이어 {Object.InputAuthority}가 모든 아이템을 드랍합니다.");

        for (int i = 0; i < SyncedSlots.Length; i++)
        {
            NetworkInventorySlot slotToDrop = SyncedSlots[i];

            if (slotToDrop.IsEmpty()) continue;

            ItemData data = ItemDatabase.GetItemDataFromID(slotToDrop.ItemID);
            if (data == null || data.dropPrefab == null) continue;

            NetworkObject droppedItemObj = Runner.Spawn(
                data.dropPrefab,
                dropPosition.position,
                Quaternion.identity
            );

            if (droppedItemObj.TryGetComponent<ItemObject>(out var itemObject))
            {
                itemObject.NetworkUseCount = slotToDrop.UseCount;
            }
            SyncedSlots.Set(i, new NetworkInventorySlot());
        }
    }


    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
    void SetShadowsRecursively(GameObject obj, UnityEngine.Rendering.ShadowCastingMode mode)
    {
        if (obj == null) return;
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.shadowCastingMode = mode;
            r.receiveShadows = (mode != UnityEngine.Rendering.ShadowCastingMode.Off);
        }
    }


    // 함수 추가 : 정하윤
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestToggleLamp()
    {
        NetworkedLampState = !NetworkedLampState;
    }

    // 함수 추가 : 정하윤
    private void ApplyLampVisuals(bool isOn)
    {
        var viewModel = _viewModelInstances[CurrentSlotIndex];
        var thirdPersonModel = _thirdPersonInstances[CurrentSlotIndex];

        // 1. 3인칭 모델 라이트 업데이트(모든 클라이언트)
        if (thirdPersonModel != null && thirdPersonModel.TryGetComponent<Lamp>(out var lamp3P))
        {
            lamp3P.SetLightVisual(isOn);
        }

        // 2. 1인칭 뷰모델 라이트 업데이트(소유자 클라이언트)
        if (HasInputAuthority)
        {
            if (viewModel != null && viewModel.TryGetComponent<Lamp>(out var lampVM))
            {
                lampVM.SetLightVisual(isOn);
            }
        }
    }

    // 함수 추가 : 최서영 > 무당방울
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_RequestRingMudangBell()
    {
        var slot = SyncedSlots[CurrentSlotIndex];

        var data = ItemDatabase.GetItemDataFromID(slot.ItemID);
        if (data == null || data.itemName != "MudangBell")
            return;

        // 같은 값으로는 이벤트 감지가 안되니까 카운터 증가 방식 사용
        MudangBellRingCount++;
    }

    private void ApplyMudangBell()
    {
        var viewModel = _viewModelInstances[CurrentSlotIndex];
        var thirdPersonModel = _thirdPersonInstances[CurrentSlotIndex];

        // 3인칭: 모든 클라이언트에서 보이는 무당방울
        if (thirdPersonModel != null && thirdPersonModel.TryGetComponent<MudangBell>(out var bell3P))
        {
            bell3P.PlayBell();
        }

        // 1인칭: 아이템 소유자에게만 보이는 뷰모델
        if (HasInputAuthority && viewModel != null && viewModel.TryGetComponent<MudangBell>(out var bellVM))
        {
            bellVM.PlayBell();
        }
    }

    /// <summary>
    /// 플레이어 손 사용 가능/불가 알리기
    /// 시체 들고있을 때에는 상호작용하지 못하도록 하기 위함
    /// </summary>
    public void SetEquipBlocked(bool blocked)
    {
        _isEquipBlocked = blocked;

        // 휠 동작
        SetScrollPaused(blocked);

        // 현재 슬롯의 시각적 상태 다시 계산
        UpdateHeldItemVisuals(CurrentSlotIndex, CurrentSlotIndex);
    }

    //튜토리얼 완료 시 아이템 제거 
    public void RemoveItemByID(int itemID)
    {
        if (!Runner.IsServer) return;

        for (int i = 0; i < SyncedSlots.Length; i++)
        {
            var slot = SyncedSlots[i];
            if (slot.IsEmpty()) continue;
            if (slot.ItemID != itemID) continue;

            SyncedSlots.Set(i, new NetworkInventorySlot());
            return;
        }
    }
}