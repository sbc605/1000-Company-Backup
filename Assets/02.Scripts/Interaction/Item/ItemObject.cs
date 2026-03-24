//코드 담당자: 유호정
using UnityEngine;
using Fusion;


public class ItemObject : NetworkBehaviour, IInteractable
{
    [Header("Data (Prefab에 할당)")]
    public ItemData itemData;

    [Header("Network State")]
    [Networked] public int NetworkItemID { get; set; }
    [Networked] public int NetworkUseCount { get; set; }

    private Outline outline;
    private int animationID = 1;
    private int _cachedItemID = -1;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = false;
        }

        if (itemData != null)
        {
            animationID = itemData.animationID;
        }
    }

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            if (itemData != null)
            {
                NetworkItemID = itemData.itemID;
                NetworkUseCount = itemData.maxUseCount;
            }
        }

        RefreshItemData();
    }


    public override void Render()
    {
        if (_cachedItemID != NetworkItemID)
        {
            RefreshItemData();
            _cachedItemID = NetworkItemID;
        }
    }


    private void RefreshItemData()
    {
        if (NetworkItemID == 0)
        {
            itemData = null;
            return;
        }

        if (ItemDatabase.Instance != null)
        {
            itemData = ItemDatabase.GetItemDataFromID(NetworkItemID);
            if (itemData != null)
            {
                animationID = itemData.animationID;
            }
        }
        else
        {
            Debug.LogError("ItemDatabase가 씬에 없거나 아직 로드되지 않았습니다");
        }
    }


    public void Interact(GameObject interactor)
    {

        InventoryManager inventory = interactor.GetComponent<InventoryManager>();
        if (inventory != null)
        {
            inventory.Server_TryPickUpItem(Object.Id);
        }
    }

    public void EnableOutline()
    {
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    public void DisableOutline()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
}