using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
public class KioskCartController : MonoBehaviour
{
    private CollectionUIController collectionUI;

    [Header("Panel & Animation")]
    [SerializeField] private RectTransform basketPanel;
    [SerializeField] private float showY = 120f;
    [SerializeField] private float hiddenY = -420f; // 숨김 상태 y (화면 아래)
    [SerializeField] private float slideDuration = 0.2f;
    [SerializeField] private float lineSpacing = 81f;

    [Header("List")]
    [SerializeField] private RectTransform content; // cartLineView 부모 역할
    [SerializeField] private CartLineView cartLineView;

    [Header("Option")]
    [SerializeField] private TextMeshProUGUI totalAmountText;
    [SerializeField] private Button clearAllButton;
    [SerializeField] private Button orderButton;
    [SerializeField] private BuyPopup buyPopup;

    private readonly Dictionary<int, CartLineView> _lines = new(); // 같은 아이템 중복 클릭 시 수량 증가
    private Coroutine _slideCo;

    private bool isOpen;
    public bool IsOpen => isOpen;

    void Awake()
    {
        clearAllButton.onClick.AddListener(() => ClearAll());
        orderButton.onClick.AddListener(OnOrder);
        SetPanelY(hiddenY);
        isOpen = false;
        UpdateTotal();

        collectionUI = FindAnyObjectByType<CollectionUIController>(FindObjectsInactive.Include); // 타블렛이 열려 있든 닫혀 있든 항상 찾음
    }

    #region Line 새로 추가할때 위치 조정

    private void ResizeContentH(int count)
    {
        // Content 높이를 라인 수에 맞게 조정
        var size = content.sizeDelta;
        size.y = lineSpacing * count;
        content.sizeDelta = size;
    }

    /// <summary>
    /// 삭제 시에 라인 재정렬
    /// </summary>
    private IEnumerator RebuildLayoutRoutine()
    {
        yield return null;

        int childCount = content.childCount;
        if (childCount == 0) yield break;

        // 첫 라인 위치 고정
        float baseX = 0f;
        float baseY = 0f;

        var first = (RectTransform)content.GetChild(0);
        first.anchorMin = new Vector2(0f, 1f);
        first.anchorMax = new Vector2(1f, 1f);
        first.pivot = new Vector2(0.5f, 1f);

        first.anchoredPosition = new Vector2(baseX, baseY);

        // 첫 라인은 그대로, 2번째부터 간격 적용
        for (int i = 1; i < childCount; i++)
        {
            var rt = (RectTransform)content.GetChild(i);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);

            rt.anchoredPosition = new Vector2(baseX, baseY - lineSpacing * i);
        }

        ResizeContentH(childCount);
    }
    #endregion

    #region 아이템 장바구니에 추가됨
    public void AddItem(ShopItemData item, int qty = 1)
    {
        // 패널 열기
        ShowBasket();

        int key = item.data.itemID;

        // 장바구니에 같은 상품이 있는지 찾음. 있으면 수량 늘리고 종료
        if (_lines.TryGetValue(key, out var cartLine))
        {
            if (cartLine.Item.data.itemID == item.data.itemID)
            {
                cartLine.SetQuantity(cartLine.Quantity + qty);
                return;
            }

            // 같은 itemID키인데 값이 다른 경우 새 키 만들어서 라인 추가(충돌 방지)
            key = MakeUniqueKey(key);
        }

        // 없으면 라인 추가
        var line = Instantiate(cartLineView, content);
        line.Init(item, qty);
        line.OnChanged += _ => UpdateTotal();
        line.OnRemoveClicked += RemoveLine;

        _lines[key] = line;

        // 새로 추가된 라인 위치
        var rt = (RectTransform)line.transform;

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, rt.offsetMin.y); // Left
        rt.offsetMax = new Vector2(0f, rt.offsetMax.y); // Right        

        int index = content.childCount - 1;

        if (index != 0)
        {
            var first = (RectTransform)content.GetChild(0);
            float baseY = first.anchoredPosition.y;

            rt.anchoredPosition = new Vector2(first.anchoredPosition.x, baseY - lineSpacing * index);
        }

        ResizeContentH(content.childCount);

        UpdateTotal();
    }

    /// <summary>
    /// 주어진 key가 이미 사용 중이면 비어있는 숫자를 찾아 반환
    /// </summary>
    private int MakeUniqueKey(int baseKey)
    {
        int tryKey = baseKey;
        while (_lines.ContainsKey(tryKey))
            tryKey++;

        return tryKey;
    }
    #endregion


    private void UpdateTotal()
    {
        int total = 0;
        foreach (var l in _lines)
            total += l.Value.LineTotal;

        totalAmountText.text = $"{total:N0}원";
    }

    #region 버튼 기능
    private void OnOrder()
    {
        if (_lines.Count == 0) return;

        buyPopup.Show(null, "구매하시겠습니까?");
        buyPopup.Open(() =>
    {
        CompleteOrder();
        ClearAll();
    });
    }

    private void SetPanelY(float y)
    {
        var anchorPos = basketPanel.anchoredPosition;
        anchorPos.y = y;
        basketPanel.anchoredPosition = anchorPos;
    }

    public void ShowBasket()
    {
        if (isOpen) return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        SlideTo(showY);
        isOpen = true;
    }

    public void HideBasket()
    {
        if (!isOpen) return;
        SlideTo(hiddenY);
        isOpen = false;
    }

    // 애니메이션x 즉시 숨김(탭 전환)
    public void HideBasketImmediate()
    {
        if (_slideCo != null) StopCoroutine(_slideCo);
        SetPanelY(hiddenY);
        isOpen = false;
        gameObject.SetActive(false);
    }

    public void ClearAll(bool immediate = false)
    {
        foreach (var l in _lines)
            Destroy(l.Value.gameObject);

        _lines.Clear();
        UpdateTotal();

        if (immediate)
            HideBasketImmediate();
        else
            HideBasket();
    }

    private void RemoveLine(CartLineView line)
    {
        int key = line.Item.data.itemID;

        if (_lines.ContainsKey(key)) _lines.Remove(key);

        Destroy(line.gameObject);
        UpdateTotal();

        StartCoroutine(RebuildLayoutRoutine());

        if (_lines.Count == 0)
            HideBasket();
    }

    private void SlideTo(float targetY)
    {
        if (_slideCo != null) StopCoroutine(_slideCo);
        _slideCo = StartCoroutine(PanelSlideRoutine(targetY));
    }

    private IEnumerator PanelSlideRoutine(float targetY)
    {
        float startPos = basketPanel.anchoredPosition.y;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / slideDuration;
            float y = Mathf.Lerp(startPos, targetY, Mathf.SmoothStep(0f, 1f, t));
            SetPanelY(y);
            yield return null;
        }
        SetPanelY(targetY);

        if (Mathf.Approximately(targetY, hiddenY)) // Y값이 hiddenY에 가까우면 끄기
            gameObject.SetActive(false);
    }
    #endregion

    /// <summary>
    /// 실제 구매 처리 함수
    /// </summary>
    private void CompleteOrder()
    {
        if (collectionUI == null)
        {
            Debug.LogError("CollectionUIController가 연결되지 않았습니다.");
            return;
        }

        foreach (var pair in _lines)
        {
            var line = pair.Value;
            ItemData itemData = line.Item.data; // ShopItemData → ItemData
            int count = line.Quantity;

            for (int i = 0; i < count; i++)
            {
                collectionUI.AddItem(itemData);
            }
        }

        Debug.Log("구매 아이템 Collection에 추가 완료");
    }
}
