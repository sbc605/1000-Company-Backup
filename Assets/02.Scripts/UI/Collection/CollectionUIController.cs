using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 작성자 : 정하윤
public class CollectionUIController : MonoBehaviour
{
    [SerializeField] private List<CollectionCell> cells;

    // 보기 버튼 클릭 시 나오는 설명창
    [SerializeField] private ItemExplainPopup explainPrefab;
    [SerializeField] private WarningPopup dropWarningPrefab;
    [SerializeField] private WarningPopup useWarningPrefab;

    // 수집한 아이템 리스트(기존 코드)
    // private List<ItemData> collectedItems = new List<ItemData>();

    // 수집한 아이템을 id 기준으로 중복 적용함
    private readonly Dictionary<int, int> itemCounts = new Dictionary<int, int>(); // 수량
    private readonly Dictionary<int, ItemData> itemRefs = new Dictionary<int, ItemData>(); // id -> ItemData(원본) 참조
    private readonly List<int> itemOrder = new(); // 구매 순서 유지용

    private void Start()
    {
        // 테스트용 아이템 3개 생성 후 추가
        // for (int i = 0; i < 5; i++)
        // {
        //     ItemData item = new ItemData
        //     {
        //         itemName = $"TestItem{i + 1}",
        //         icon = null,
        //         description = $"이것은 TestItem{i + 1}의 설명입니다.",
        //         canUse = true
        //     };

        //     AddItem(item);
        // }
    }

    // 아이템 추가
    public void AddItem(ItemData item, int qty = 1)
    {
        if (item == null) return;

        int id = item.itemID;

        // 신규 등록이면 슬롯 하나 추가(최대 9칸 제한)
        bool isNew = !itemCounts.ContainsKey(id);

        if (isNew)
        {
            if (itemCounts.Count >= 9)
                return;

            itemOrder.Add(id); // 순서 기록
            itemRefs[id] = item;
            itemCounts[id] = qty;
        }
        else
        {
            itemCounts[id] += qty;
        }

        // collectedItems.Add(item);

        UpdateCells();
    }

    // Cell UI 갱신
    private void UpdateCells()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (i < itemOrder.Count)
            {
                int id = itemOrder[i];
                ItemData item = itemRefs[id];
                int qty = itemCounts[id];

                cells[i].Initialize(item, qty, onViewClicked: ShowExplain, onUseClicked: ShowUseWarning, onDropClicked: ShowDropWarning);
            }
            else
            {
                // 아이템 데이터가 없으면 Cell 비활성화
                cells[i].Clear();
            }
        }
    }

    // View 버튼 클릭 시 나올 설명창 표시
    private void ShowExplain(ItemData item)
    {
        explainPrefab.Show(item);
    }

    // 버리기 버튼 클릭 시 나올 경고창 표시
    private void ShowDropWarning(ItemData item)
    {
        dropWarningPrefab.Show(item);
        dropWarningPrefab.Open(() =>
        {
            DropItem(item);
        });
    }

    // 사용 버튼 클릭 시 나올 경고창 표시
    private void ShowUseWarning(ItemData item)
    {
        useWarningPrefab.Show(item);
        useWarningPrefab.Open(() =>
        {
            UseItem(item);
        });
    }

    // Use 버튼 클릭 시 아이템 사용
    private void UseItem(ItemData item)
    {
        if (!item.canUse)
        {
            Debug.Log("사용 아이템이 아닙니다");
            return;
        }

        Debug.Log($"Use {item.itemName}");

        int id = item.itemID;

        if (!itemCounts.TryGetValue(id, out var qty)) return;

        qty -= 1;

        // 실제 아이템 사용 로직 추가
        if (qty <= 0)
        {
            itemCounts.Remove(id);
            itemRefs.Remove(id);
            itemOrder.Remove(id);
        }
        else
        {
            itemCounts[id] = qty;
        }
        UpdateCells();
    }

    // Drop 버튼 클릭 시 아이템 제거
    private void DropItem(ItemData item)
    {
        if (item == null) return;

        int id = item.itemID;

        Debug.Log($"{item.itemName}을 전부 버렸습니다");
        itemCounts.Remove(id);
        itemRefs.Remove(id);
        itemOrder.Remove(id);
        UpdateCells();
    }

    /* 제령 아이템만 수집품 목록에서 제거
    // 제령 아이템도 Collection에 들어가는 경우 있을 수 있으므로 우선 주석 처리, 필요시 해제할 것
    public void RemoveExorcismItems()
    {
        // 제거 대상 id 수집 (컬렉션 수정 중 순회 금지)
        var removeList = itemOrder
            .Where(id => itemRefs[id].itemType == ItemData.EItemType.Exorcism)
            .ToList();

        foreach (var id in removeList)
        {
            itemCounts.Remove(id);
            itemRefs.Remove(id);
            itemOrder.Remove(id);
        }
        UpdateCells();
    }
    */
}
