using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
[RequireComponent(typeof(ScrollRect))]
[RequireComponent(typeof(RectTransform))]
public class ShopUIController : MonoBehaviour
{
    [Header("연결 필요")]
    [SerializeField] private ShopItemSpawner itemSpawner;
    [SerializeField] private ShopCell cellPrefab;
    [SerializeField] private BuyPopup coinBuyWarningPrefab;
    [SerializeField] private BuyPopup mileageBuyWarningPrefab;

    [SerializeField] private int columns = 2;
    [SerializeField] private float cellWidth = 150f;
    [SerializeField] private float cellHeight = 150f;
    [SerializeField] private float spacingX = 20f;
    [SerializeField] private float spacingY = 20f;
    [SerializeField] private float paddingTop = 25f;
    [SerializeField] private float paddingLeft = 20f;

    [SerializeField] private List<ShopItemData> shopItems;
    private NetworkRunner _runner;

    // 재사용하는 Cell 리스트
    private List<ShopCell> cellPool = new List<ShopCell>();

    [SerializeField] private ScrollRect _scrollRect;

    // 한 번에 화면에 보이는 셀의 개수
    private int visibleCount = 4;

    void Start()
    {
        itemSpawner = FindFirstObjectByType<ShopItemSpawner>();

        if (itemSpawner == null)
            Debug.LogError("ShopItemSpawner가 인스펙터에 연결되지 않음", gameObject);

        _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner == null)
            Debug.LogError("NetworkRunner를 찾을 수 없음");

        InitializePool();

        UpdateContentHeight();

        // 강제로 레이아웃 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.GetComponent<RectTransform>());

        // 시작 시 스크롤 위치를 맨 위로 고정
        _scrollRect.content.anchoredPosition = Vector2.zero;
        // 스크롤 속도 조절
        _scrollRect.scrollSensitivity = 10.0f;

        UpdateVisibleCells();

        // 스크롤 시 화면에 보이는 셀 갱신하도록 함수 연결
        _scrollRect.onValueChanged.AddListener((pos) => UpdateVisibleCells());

        ResetAllItems();
    }

    // 초기 셀 풀 구성 : 화면에 보이는 셀 수보다 조금 더 생성
    void InitializePool()
    {
        for (int i = 0; i < visibleCount + 2; i++)
        {
            ShopCell cell = Instantiate(cellPrefab, _scrollRect.content);

            cell.OnBuyButtonClickedRequest += HandleBuyButtonClicked;
            cellPool.Add(cell);
        }
    }

    // 전체 콘텐츠 영역의 높이 계산
    private void UpdateContentHeight()
    {
        // 총 행 개수 계산
        int totalRows = Mathf.CeilToInt(shopItems.Count / (float)columns);

        // 행 개수 × 셀 높이 + 간격 + 패딩
        float contentHeight = totalRows * (cellHeight + spacingY) + paddingTop;
        contentHeight = Mathf.Max(contentHeight, _scrollRect.viewport.rect.height);

        // Content의 세로 크기 갱신
        _scrollRect.content.sizeDelta = new Vector2(_scrollRect.content.sizeDelta.x, contentHeight);
    }

    // 화면에 보여야 할 셀만 표시 및 위치 갱신
    private void UpdateVisibleCells()
    {
        float scrollY = Mathf.Max(0, _scrollRect.content.anchoredPosition.y);
        int startRow = Mathf.FloorToInt(scrollY / (cellHeight + spacingY));
        int startIndex = startRow * columns;

        // 현재 보이는 셀 인덱스 범위
        int endIndex = Mathf.Min(startIndex + visibleCount + columns, shopItems.Count);
        int poolIndex = 0;

        for (int dataIndex = startIndex; dataIndex < endIndex; dataIndex++)
        {
            // 재사용 가능한 셀 가져오기
            ShopCell cell = cellPool[poolIndex];

            cell.gameObject.SetActive(true);
            cell.SetItem(shopItems[dataIndex], dataIndex);

            int row = dataIndex / columns;
            int col = dataIndex % columns;

            float x = col * (cellWidth + spacingX) + paddingLeft;
            float y = -row * (cellHeight + spacingY) - paddingTop;
            cell.transform.localPosition = new Vector3(x, y, 0f);

            poolIndex++;
        }

        // 나머지는 비활성화
        for (; poolIndex < cellPool.Count; poolIndex++)
            cellPool[poolIndex].gameObject.SetActive(false);
    }

    private void HandleBuyButtonClicked(ShopItemData item)
    {
        if (itemSpawner == null)
        {
            Debug.LogError("ShopItemSpawner 참조가 없음(인스펙터 연결 확인)");
            return;
        }

        if (item.data.dropPrefab == null)
        {
            Debug.LogError($"아이템 {item.data.itemName}에 networkPrefab이 할당되지 않음");
            return;
        }

        if (!item.freeAvailable)
        {
            ShowBuyWarningByMileage(item);
            mileageBuyWarningPrefab.Open(() =>
            {
                item.isSoldOut = true;
                Debug.Log($"마일리지로 구매: {item.data.itemName}");

                RequestSpawnItemOnNetwork(item);

                UpdateVisibleCells();
            });
        }
        else
        {
            ShowBuyWarningByCoin(item);
            coinBuyWarningPrefab.Open(() =>
            {
                item.freeAvailable = false;
                Debug.Log($"구매: {item.data.itemName}");

                RequestSpawnItemOnNetwork(item);

                UpdateVisibleCells();
            });
        }
    }
    private void RequestSpawnItemOnNetwork(ShopItemData item)
    {
        if (_runner == null || itemSpawner == null) 
            return;

        itemSpawner.Rpc_RequestSpawnItem
        (
            item.data.dropPrefab,
            _runner.LocalPlayer
        );
    }

    private void ShowBuyWarningByCoin(ShopItemData item)
    {
        coinBuyWarningPrefab.Show(item);
        coinBuyWarningPrefab.RequestConfirm = () => { };
    }

    private void ShowBuyWarningByMileage(ShopItemData item)
    {
        mileageBuyWarningPrefab.Show(item, $"구매 시 마일리지 {item.requiredMileage}개가 필요합니다");
        mileageBuyWarningPrefab.RequestConfirm = () => { };
    }

    // 상점 내 모든 아이템 초기화
    public void ResetAllItems()
    {
        foreach (var item in shopItems)
        {
            item.freeAvailable = true;
            item.isSoldOut = false;
        }
        UpdateVisibleCells();
    }
}