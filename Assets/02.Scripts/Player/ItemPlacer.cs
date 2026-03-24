//코드 담당자: 유호정
using UnityEngine;
using System.Collections.Generic;

public class ItemPlacer : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Placement Settings")]
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private Color validPlacementColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color invalidPlacementColor = new Color(1f, 0f, 0f, 0.5f);

    private Dictionary<Renderer, Color[]> originalColors = new Dictionary<Renderer, Color[]>();
    private MaterialPropertyBlock propBlock;

    private GameObject currentPreviewObject;
    private ItemData currentPreviewItemData = null;
    private bool canPlaceCurrentItem = false;

    private void Awake()
    {
        if (playerInteraction == null)
            playerInteraction = GetComponent<PlayerInteraction>();
        if (inventoryManager == null)
            inventoryManager = GetComponent<InventoryManager>();

        propBlock = new MaterialPropertyBlock();
    }

    private void Update()
    {
        NetworkInventorySlot currentSlot = inventoryManager.SyncedSlots[inventoryManager.CurrentSlotIndex];
        ItemData data = null;
        bool isPlacing = false;

        if (!currentSlot.IsEmpty())
        {
            data = ItemDatabase.GetItemDataFromID(currentSlot.ItemID);


            isPlacing = (data != null && data.isInstallable && data.previewPrefab != null);
        }

        HandlePreviewObject(isPlacing, data);


        if (isPlacing && currentPreviewObject != null)
        {
            UpdatePreviewPlacement(data);
        }
    }

    private void HandlePreviewObject(bool isPlacing, ItemData newItemData)
    {
        if (isPlacing)
        {
            if (currentPreviewObject == null || currentPreviewItemData != newItemData)
            {
                if (currentPreviewObject != null)
                {
                    RestoreOriginalColors();
                    Destroy(currentPreviewObject);
                }

                currentPreviewObject = Instantiate(newItemData.previewPrefab);
                currentPreviewItemData = newItemData;


                int viewModelLayerID = LayerMask.NameToLayer(inventoryManager.viewModelLayer);
                SetLayerRecursively(currentPreviewObject, viewModelLayerID);


                originalColors.Clear();
                Renderer[] renderers = currentPreviewObject.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer rend in renderers)
                {
                    originalColors.Add(rend, null);
                }
            }
        }
        else
        {
            if (currentPreviewObject != null)
            {
                RestoreOriginalColors();
                Destroy(currentPreviewObject);
                currentPreviewObject = null;
                currentPreviewItemData = null;
                originalColors.Clear();
                canPlaceCurrentItem = false;
            }
        }
    }

    private void UpdatePreviewPlacement(ItemData currentItemData)
    {

        if (!playerInteraction.HasValidHit || playerInteraction.HitInfo.collider == null)
        {
            currentPreviewObject.SetActive(false);
            canPlaceCurrentItem = false;
            return;
        }

        RaycastHit hit = playerInteraction.HitInfo;
        LayerMask placementMask = currentItemData.placementLayerMask;

        bool isValidSurface = (placementMask.value & (1 << hit.collider.gameObject.layer)) > 0;
        bool isNormalUpwards = hit.normal.y > 0.01f;

        if (isValidSurface && isNormalUpwards)
        {
            currentPreviewObject.SetActive(true);

            currentPreviewObject.transform.position = hit.point + (hit.normal * currentItemData.placementOffset);
            Vector3 playerPosition = transform.position;
            Vector3 previewPosition = currentPreviewObject.transform.position;
            Vector3 directionToPlayer = playerPosition - previewPosition;
            directionToPlayer.y = 0;
            Quaternion facePlayerRotation = Quaternion.identity;
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                facePlayerRotation = Quaternion.LookRotation(directionToPlayer);
            }
            currentPreviewObject.transform.rotation = facePlayerRotation;
            // currentPreviewObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);


            bool isSpaceClear = !Physics.CheckSphere(
                currentPreviewObject.transform.position,
                currentItemData.placementCheckRadius,
                obstacleLayerMask
            );


            canPlaceCurrentItem = isSpaceClear;
            SetPlacementColor(canPlaceCurrentItem);
        }
        else
        {
            currentPreviewObject.SetActive(false);
            canPlaceCurrentItem = false;
        }
    }


    public void AttemptInstall(int itemID, int useCount)
    {

        if (canPlaceCurrentItem)
        {

            Vector3 installPosition = currentPreviewObject.transform.position;
            Vector3 playerPosition = transform.position;
            Vector3 directionToPlayer = playerPosition - installPosition;
            directionToPlayer.y = 0;
            Quaternion facePlayerRotation = Quaternion.identity;
            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                facePlayerRotation = Quaternion.LookRotation(directionToPlayer);
            }

            inventoryManager.RPC_PlaceItem(
                itemID,
                useCount,
                installPosition,
                facePlayerRotation,
                inventoryManager.CurrentSlotIndex
            );
        }
        else
        {
            Debug.Log("여기에 설치할 수 없습니다.");
        }
    }

    private void SetPlacementColor(bool isValid)
    {
        Color targetColor = isValid ? validPlacementColor : invalidPlacementColor;
        propBlock.SetColor("_BaseColor", targetColor);
        foreach (Renderer rend in originalColors.Keys)
        {
            if (rend == null) continue;
            rend.SetPropertyBlock(propBlock);
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (Renderer rend in originalColors.Keys)
        {
            if (rend == null) continue;
            rend.SetPropertyBlock(null);
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
}