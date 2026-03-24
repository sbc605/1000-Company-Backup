using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코드 담당자: 김수아
// 아이템 설명창의 위치만 관리
public class TooltipUI : MonoBehaviour
{
    private RectTransform canvasRect;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private LayoutElement layoutElement; // 자동 줄바꿈용
    [SerializeField] private TextMeshProUGUI textField;

    [Header("Follow Tuning")]
    [SerializeField] private float stickyRadiusPx = 25f; // 처음 위치에서 이만큼 벗어나기 전까진 고정
    [SerializeField] private float minMoveToUpdatePx = 10f; // 마지막 갱신 위치 대비 최소 이동 픽셀

    [Header("Sizing")]
    // [SerializeField] private float maxWidth = 420f; // 최대 가로
    [SerializeField] private float minWidth = 420f; // 최소 가로
    [SerializeField] private float minHeight = 220f; // 최소 세로
    [SerializeField] private float horizontalPadding = 24f; // 배경 좌우 패딩
    [SerializeField] private float verticalPadding = 16f;   // 배경 상하 패딩

    [Header("Line Spacing")]
    [SerializeField] private float lineSpacing = 4f; // 줄간 간격
    [SerializeField] private float paragraphSpacing = 8f; // 문단 간 간격

    [Header("Break Tokens")]
    [SerializeField] private string[] breakTokens; // 줄바꿈 키워드 토큰 (필요 시 추가)


    private bool _following;
    private Vector2 _anchorScreen; // 최초 표시한 스크린 좌표(고정 지점)
    private Vector2 _lastUpdateScreen; // 마지막으로 UpdatePosition을 호출했던 스크린 좌표


    void Awake()
    {
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (tooltipRect == null) tooltipRect = GetComponent<RectTransform>();
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        // 툴팁이 포인터 이벤트를 가로채지 않게
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        ApplySize(minWidth, minHeight);
        Hide();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        // transform.SetAsLastSibling(); // 최상단
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // 외부에서 내용 교체
    public void SetText(string text)
    {
        string format = ItemTextFormatter.Format(text, breakTokens, lineSpacing, paragraphSpacing, textField);

        textField.text = format;
        ResizeToFit(format);
    }

    /// <summary>
    /// 배경 영역 지정
    /// 한 줄: 17자 이하면 440x220, 17자 초과시 가로만 늘리고 높이는 220 고정
    /// 여러 줄: 가로는 내용에 맞춰 늘림, 높이는 220 이상으로 늘림
    /// </summary>
    private void ResizeToFit(string tx)
    {
        if (!textField || !tooltipRect) return;
        bool hasManualBreak = tx.Contains("\n");

        // 총 텍스트에 맞춘 추천 너비 및 높이
        // GetPreferredValues: 텍스트, 가로제한, 세로제한 0=제한없음)
        Vector2 preferred = textField.GetPreferredValues(tx, 0f, 0f);
        float contentW = preferred.x;
        float contentH = preferred.y;

        // 배경 최종 사이즈 = 내용 + 패딩
        float bgW, bgH;

        if (!hasManualBreak)
        {
            // 한줄
            bgW = Mathf.Max(minWidth, contentW + horizontalPadding);
            bgH = minHeight; // 세로 고정
        }
        else
        {
            // 여러 줄
            bgW = Mathf.Max(minWidth, contentW + horizontalPadding);
            bgH = Mathf.Max(minHeight, contentH + verticalPadding);
        }

        ApplySize(bgW, bgH);

        // 즉시 갱신
        textField.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
    }

    private void ApplySize(float w, float h)
    {
        if (layoutElement)
        {
            layoutElement.preferredWidth = w;
            layoutElement.preferredHeight = h;
        }
        tooltipRect.sizeDelta = new Vector2(w, h);
    }

    #region 마우스 커서 반응
    /// <summary>
    /// 최초 버튼 진입 시: 초기 위치에 배치하고 앵커 저장
    /// </summary>
    public void BeginFollow(Vector2 initialScreenPos)
    {
        _following = false;
        _anchorScreen = initialScreenPos;
        _lastUpdateScreen = initialScreenPos;

        UpdatePosition(initialScreenPos);
    }

    /// <summary>
    /// 마우스 포인트 이동할때 호출
    /// </summary>
    public void StickyFollow(Vector2 currentScreenPos)
    {
        if (!gameObject.activeSelf) return;

        if (!_following)
        {
            if (Vector2.Distance(currentScreenPos, _anchorScreen) < stickyRadiusPx)
                return;

            _following = true;
            _lastUpdateScreen = _anchorScreen;
        }

        // 최소 이동량 미만이면 스킵
        if (Vector2.Distance(currentScreenPos, _lastUpdateScreen) < minMoveToUpdatePx)
            return;

        // 실제 위치 갱신
        UpdatePosition(currentScreenPos);
        _lastUpdateScreen = currentScreenPos;
    }

    /// <summary>
    /// 마우스 좌표를 받아 툴팁 위치 갱신
    /// </summary>
    public void UpdatePosition(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPoint); // Screen Space - Overlay일 때 camera는 null

        // 화면 경계 체크를 위한 월드 좌표
        tooltipRect.anchoredPosition = localPoint;

        // 툴팁이 캔버스 벗어나면 pivot을 뒤집어 붙임
        Vector2 canvasSize = canvasRect.rect.size;
        Vector2 tooltipSize = tooltipRect.rect.size * tooltipRect.localScale;
        Vector2 anchored = tooltipRect.anchoredPosition;

        // 화면 넘어가면 pivot을 반대로
        Vector2 newPivot = tooltipRect.pivot;

        // 오른쪽 넘어감
        float right = anchored.x + tooltipSize.x * (1f - newPivot.x);
        if (right > canvasSize.x * 0.5f)
            newPivot.x = 1f; // 왼쪽 기준

        // 왼쪽 
        float left = anchored.x - tooltipSize.x * newPivot.x;
        if (left < -canvasSize.x * 0.5f)
            newPivot.x = 0f; // 오른쪽 기준

        // 위 넘어감
        float top = anchored.y + tooltipSize.y * (1f - newPivot.y);
        if (top > canvasSize.y * 0.5f)
            newPivot.y = 1f; // 아래 기준

        // 아래 넘어감
        float bottom = anchored.y - tooltipSize.y * newPivot.y;
        if (bottom < -canvasSize.y * 0.5f)
            newPivot.y = 0f; // 위 기준

        tooltipRect.pivot = newPivot;

        // 최종 위치 다시 계산
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPoint);
        tooltipRect.anchoredPosition = localPoint;
    }
    #endregion
}


