using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public enum EGrade
{
    인턴,
    사원,
    대리,
    과장,
    팀장
}

[System.Serializable]
public class TeamMemberData
{
    public string nickName; // PlayerNameSync.PlayerName이 들어감
    public EGrade grade;
    public int CurrentMental = 100;
}
public class VitalCellUI : MonoBehaviour
{
    public int maxMental = 100;

    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private TextMeshProUGUI mentalStateText;
    [SerializeField] private TextMeshProUGUI mentalPercentText;
    [SerializeField] private Image BarImage;
    [SerializeField] private Slider volumeSlider; // 코드 추가: 김수아

    private TeamMemberData memberData;
    private bool volumeInit = false;

    public void SetData(TeamMemberData data)
    {
        memberData = data;
        memberData.CurrentMental = Mathf.Clamp(memberData.CurrentMental, 0, maxMental);

        RefreshUI();

        // 본인일 경우 볼륨 슬라이더 비활성화
        if (volumeSlider != null)
        {
            bool isLocalPlayer = PlayerNameSync.Local != null && PlayerNameSync.Local.PlayerName == memberData.nickName;

            volumeSlider.gameObject.SetActive(!isLocalPlayer);
        }

        if (volumeSlider != null && !volumeInit)
        {
            volumeInit = true;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Vivox 참가자 등록될 때까지 대기 후 초기 볼륨 적용
        if (volumeSlider != null)
            StartCoroutine(VloumeChangeRoutine());
    }

    private IEnumerator VloumeChangeRoutine()
    {
        yield return new WaitUntil(() => VivoxManager.Instance.participantsList.Exists(p => p.DisplayName == memberData.nickName));

        // 기본값 0.5로 설정 (중간 볼륨)
        volumeSlider.value = 0.5f;
        OnVolumeChanged(volumeSlider.value); // 초기 볼륨도 한 번 적용
    }

    private void OnVolumeChanged(float value)
    {
        // 0 ~ 1 값으로 들어옴
        if (memberData == null) return;

        Debug.Log($"[VitalCellUI] {memberData.nickName} 슬라이더 변경: {value:0.00}");
        // Vivox 볼륨 조절
        VivoxManager.Instance.SetPlayerVolume(memberData.nickName, value);
    }

    /// <summary>
    /// 플레이어 정신력 수치 변경에 사용
    /// </summary>
    public void SetMental(int value)
    {
        memberData.CurrentMental = Mathf.Clamp(value, 0, maxMental);
        RefreshUI();
    }

    /// <summary>
    /// 플레이어 직급을 바꿀 때
    /// </summary>
    public void Upgrade(EGrade value)
    {
        memberData.grade = value;
        RefreshUI();
    }

    // UI 갱신
    public void RefreshUI()
    {
        Debug.Log($"[VitalCellUI] RefreshUI() 호출 - memberData.nickName = {memberData?.nickName}");

        nicknameText.text = GetDisplayName(memberData.nickName);
        gradeText.text = memberData.grade.ToString();

        float ratio = (float)memberData.CurrentMental / maxMental;

        BarImage.fillAmount = ratio;
        UpdateMentalStatus(ratio);
        UpdateMentalPercent();
    }

    // 정신력 상태 정보 갱신
    private void UpdateMentalStatus(float ratio)
    {
        if (maxMental <= 0)
        {
            mentalStateText.text = "정보 없음";
            return;
        }

        if (ratio <= 0.25f)
            mentalStateText.text = "나쁨";
        else if (ratio >= 0.75f)
            mentalStateText.text = "정상";
        else
            mentalStateText.text = "양호";
    }

    // 정신력 퍼센트 정보 갱신
    private void UpdateMentalPercent()
    {
        float value = (float)memberData.CurrentMental / maxMental;
        int percent = Mathf.RoundToInt(value * 100);
        mentalPercentText.text = $"{percent}%";
        mentalPercentText.ForceMeshUpdate();
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

