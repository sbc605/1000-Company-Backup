using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class HomeUIController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nickNameText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI aspirationText;
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI mileageText;
    [SerializeField] private Image expFillImage;
    [SerializeField] private TextMeshProUGUI remainPerformance;

    private TeamMemberData memberData; // 로컬 플레이어 정보 저장

    private int currentPerformance = 0;
    private int maxPerformance = 100;

    private int coin = 0;
    private int mileage = 0;
    private string aspiration = "";


    private void OnEnable()
    {
        PlayerNameSync.OnPlayerRegistered += HandlePlayerRegistered;
    }

    private void OnDisable()
    {
        PlayerNameSync.OnPlayerRegistered -= HandlePlayerRegistered;
    }


    private void Start()
    {
        UpdateUserUI();
        UpdateUI();
    }

    // 코드 추가: 김수아
    /// <summary>
    /// PlayerNameSync에서 등록된 플레이어 중 Local Player인지 확인
    /// </summary>
    private void HandlePlayerRegistered(TeamMemberData data)
    {
        // 로컬 플레이어인지 비교
        if (PlayerNameSync.Local != null &&
            data.nickName == PlayerNameSync.Local.PlayerName)
        {
            SetData(data);
        }
    }

    // TeamMemberData 받아 UI 적용하는 함수
    public void SetData(TeamMemberData data)
    {
        memberData = data;

        if (nickNameText != null)
            nickNameText.text = GetDisplayName(data.nickName);

        if (gradeText != null)
            gradeText.text = data.grade.ToString();

        // UpdateUserUI();
    }


    // -----------------------------
    // 성과 / 경험치 UI
    // -----------------------------

    // 최대 경험치 변경
    public void SetMaxPerformance(int max)
    {
        maxPerformance = Mathf.Max(1, max);
        UpdateUI();
    }

    public void SetPerformance(int value)
    {
        currentPerformance = Mathf.Clamp(value, 0, maxPerformance);
        UpdateUI();
    }

    private void UpdateUI()
    {
        // 성괄 Bar UI 갱신
        float fillAmount = (float)currentPerformance / maxPerformance;
        expFillImage.fillAmount = fillAmount;

        // 승진까지 남은 성과 계산
        int remaining = maxPerformance - currentPerformance;

        // 승진까지 남은 성과 UI 갱신
        if (remainPerformance != null)
            remainPerformance.text = $"{remaining}";
    }


    // 경험치 추가
    public void AddPerformance(int amount)
    {
        SetPerformance(currentPerformance + amount);
    }

    // -----------------------------
    // 유저 정보 (코인, 마일리지, 포부)
    // -----------------------------

    public void SetAspiration(string comment)
    {
        aspiration = comment;
        UpdateUserUI();
    }

    public void SetCoin(int amount)
    {
        coin = amount;
        UpdateUserUI();
    }
    public void SetMileage(int amount)
    {
        mileage = amount;
        UpdateUserUI();
    }


    private void UpdateUserUI()
    {
        Debug.Log($"[VitalCellUI] RefreshUI() 호출 - memberData.nickName = {memberData?.nickName}");

        //nickNameText.text = GetDisplayName(memberData.nickName);
        gradeText.text = memberData.grade.ToString();

        if (coinText != null)
            coinText.text = $"{coin} 원";

        if (aspirationText != null)
            aspirationText.text = aspiration;

        if (mileageText != null)
            mileageText.text = $"{mileage} 개";
    }

    /// <summary>
    /// 중복 닉네임 구분용 인덱스 UI에선 가리는 용도
    /// </summary>
    private string GetDisplayName(string rawName)
    {
        int index = rawName.LastIndexOf('_');
        if (index > 0)
            return rawName.Substring(0, index);

        return rawName;
    }
}
