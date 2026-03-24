using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 코드 담당자: 김수아

public class KioskUIController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private KioskItemCell cellPrefab;
    [SerializeField] private KioskCartController cart;

    [Header("Grid")]
    private int columns = 4;
    [SerializeField] private Vector2 spacing = new Vector2(50, 50);
    [SerializeField] private RectOffset padding;
    private int visibleRows = 2; // 기본 화면에 보이는 행 수
    private int bufferRows = 1;  // 위,아래 버퍼 행

    // 주입받을 런타임 상태
    private List<ShopItemData> shopItems = new();
    private ScrollRect scroll;
    private RectTransform content;

    // 내부 상태
    private List<KioskItemCell> activeCells = new();
    private Vector2 cellSize;
    private int totalRows;
    private int firstVisibleRow = -1; // 가장 위에 있는 행 번호(스크롤에 따라 갱신됨)
    private int layoutCount; // 아이템이 부족해도 최소칸
    private bool built;

    private UnityAction<Vector2> onScroll;

    public void SetData(List<ShopItemData> items, ScrollRect sr)
    {
        shopItems = items ?? new List<ShopItemData>();
        scroll = sr;
        content = scroll ? scroll.content : null;
    }

    public void BuildFirst()
    {
        if (built) return;

        // 셀 크기 확보
        var prefabRT = cellPrefab.GetComponent<RectTransform>();
        cellSize = prefabRT.rect.size;
        if (cellSize == Vector2.zero)
            cellSize = new Vector2(300, 300);

        Init(); // content 높이 & 풀 생성

        // Scroll Listener
        if (onScroll == null) onScroll = _ => UpdateVisibleCells(true);
        scroll.onValueChanged.RemoveListener(onScroll); // 중복 방지
        scroll.onValueChanged.AddListener(onScroll);
        scroll.scrollSensitivity = 10f;

        built = true;
    }

    public void RemoveListeners()
    {
        if (scroll != null && onScroll != null)
            scroll.onValueChanged.RemoveListener(onScroll);
    }

    #if UNITY_EDITOR
        void Update()
        {
            UpdateVisibleCells(true);
        }
    #endif

    private void Init()
    {
        int minCells = visibleRows * columns;
        layoutCount = Mathf.Max(shopItems.Count, minCells);
        totalRows = Mathf.CeilToInt(layoutCount / (float)columns);

        // content 높이 계산
        float totalHeight = padding.top + padding.bottom + totalRows * cellSize.y + Mathf.Max(0, totalRows - 1) * spacing.y;

        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(totalHeight, scroll.viewport.rect.height));

        // 16개 셀 생성
        int createCount = (visibleRows + bufferRows * 2) * columns;
        for (int i = 0; i < createCount; i++)
        {
            var cell = Instantiate(cellPrefab, content);
            cell.gameObject.SetActive(true);
            activeCells.Add(cell);
        }
    }

    public void UpdateVisibleCells(bool update = false)
    {
        float scrollY = Mathf.Max(0, scroll.content.anchoredPosition.y);
        float rowHeight = cellSize.y + spacing.y;

        // 처음으로 보이는 행
        int startRow = Mathf.FloorToInt((scrollY - padding.top) / rowHeight);
        startRow = Mathf.Clamp(startRow, 0, Mathf.Max(0, totalRows - (visibleRows + bufferRows * 2)));

        if (!update && startRow == firstVisibleRow) return;
        firstVisibleRow = startRow;
        int startIndex = firstVisibleRow * columns;

        // 중앙 정렬용 폭 계산
        float totalWidth = padding.left + padding.right + columns * cellSize.x + (columns - 1) * spacing.x;
        float xCenterOffset = Mathf.Max(0, (scroll.viewport.rect.width - totalWidth) / 2f);

        // Content가 pivot (0.5, 1)인 상태에서 위치 계산
        float contentHalfHeight = content.rect.height * 0.5f;

        // 각 셀 데이터와 위치 갱신
        for (int i = 0; i < activeCells.Count; i++)
        {
            int poolIndex = startIndex + i;
            var cell = activeCells[i];

            // 위치 계산
            int row = poolIndex / columns;
            int col = poolIndex % columns;

            float x = padding.left + col * (cellSize.x + spacing.x) + xCenterOffset;
            float y = padding.top + row * (cellSize.y + spacing.y);
            var rt = (RectTransform)cell.transform;

            // 중앙 pivot 기준으로 위쪽 절반을 기준좌표로 사용     
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);

            if (poolIndex >= 0 && poolIndex < layoutCount)
            {
                // 인덱스가 실제 아이템 수를 넘으면 null 전달 → 빈 슬롯
                ShopItemData item = (poolIndex < shopItems.Count) ? shopItems[poolIndex] : null;
                cell.Bind(item, cart);
            }
        }
    }
}
