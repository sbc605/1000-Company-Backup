using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 작성자 : 정하윤
public class VitalDisplayUIController : MonoBehaviour
{
    [SerializeField] private List<VitalCellUI> memberUIs;
    private TeamMemberData[] teamMembers;

    private void Awake()
    {
        teamMembers = new TeamMemberData[memberUIs.Count];
    }

    private void OnEnable()
    {
        PlayerNameSync.OnPlayerRegistered += HandlePlayerRegistered;
        PlayerNameSync.OnPlayerUnregistered += HandlePlayerUnregistered;
        PlayerLevel.OnLevelChanged += HandleLevelChanged; // 레벨 이벤트 구독
        PlayerCondition.OnSanityUpdate += HandleSanityUpdated;

        RefreshAllMembers();
    }

    private void OnDisable()
    {
        PlayerNameSync.OnPlayerRegistered -= HandlePlayerRegistered;
        PlayerNameSync.OnPlayerUnregistered -= HandlePlayerUnregistered;
        PlayerLevel.OnLevelChanged -= HandleLevelChanged;
        PlayerCondition.OnSanityUpdate -= HandleSanityUpdated;
    }

    private void HandlePlayerRegistered(TeamMemberData data)
    {
        AddMember(data);
    }

    private void HandlePlayerUnregistered(string nickName)
    {
        RemoveMember(nickName);
    }

    private void HandleSanityUpdated(string nickName, float san)
    {
        // Sanity(0~100) → 정수 변환
        int mental = Mathf.RoundToInt(san);
        UpdateMemberMentalByName(nickName, mental);
    }

    // 레벨 변경 → 직급(EGrade) 계산 → VitalCellUI.Upgrade 호출
    private void HandleLevelChanged(string nickName, int level)
    {
        for (int i = 0; i < teamMembers.Length; i++)
        {
            var data = teamMembers[i];
            if (data != null && data.nickName == nickName)
            {
                var grade = GradeHelper.GetGradeByLevel(level);
                data.grade = grade;
                memberUIs[i].Upgrade(grade); // 내부에서 RefreshUI 호출
                Debug.Log($"[VitalDisplayUI] {nickName} 직급 변경 → {grade}");
                return;
            }
        }
    }

    /// <summary>
    /// 플레이어가 들어왔을 때: 빈 VitalCellUI 하나 On + 닉네임 세팅
    /// </summary>
    public void AddMember(TeamMemberData data)
    {
        int index = FindEmptySlot();
        if (index == -1)
        {
            Debug.LogWarning("[VitalDisplayUI] 빈 슬롯이 없습니다.");
            return;
        }

        teamMembers[index] = data;

        var cell = memberUIs[index];
        cell.gameObject.SetActive(true);
        cell.SetData(data);

        Debug.Log($"[VitalDisplayUI] Slot {index} 활성화 → {data.nickName}");
    }

    /// <summary>
    /// 플레이어가 나갔을 때: 해당 닉네임 슬롯 Off
    /// </summary>
    public void RemoveMember(string nickName)
    {
        for (int i = 0; i < teamMembers.Length; i++)
        {
            var data = teamMembers[i];
            if (data != null && data.nickName == nickName)
            {
                teamMembers[i] = null;
                memberUIs[i].gameObject.SetActive(false);
                Debug.Log($"[VitalDisplayUI] Slot {i} 비활성화 ← {nickName}");
                return;
            }
        }

        Debug.LogWarning($"[VitalDisplayUI] RemoveMember: '{nickName}'을 찾지 못했습니다.");
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < teamMembers.Length; i++)
        {
            if (teamMembers[i] == null)
                return i;
        }
        return -1;
    }

    // 전체 팀원 데이터로 UI 초기화
    public void SetTeamMembers(List<TeamMemberData> members)
    {
        for (int i = 0; i < memberUIs.Count; i++)
        {
            if (i < members.Count)
            {
                memberUIs[i].gameObject.SetActive(true);
                memberUIs[i].SetData(members[i]);
            }
            //데이터가 없는 경우 숨김
            else
            {
                memberUIs[i].gameObject.SetActive(false);
            }
        }
    }

    // <summary>
    // Vital UI 전부 다시 갱신
    // </summary>
    private void RefreshAllMembers()
    {
        foreach (var slot in PlayerNameSync.playerNameSlots)
        {
            var name = slot.Key;
            var sync = slot.Value;

            var levelComp = sync.GetComponent<PlayerLevel>();
            var condition = sync.GetComponent<PlayerCondition>();

            int level = levelComp != null ? levelComp.Level : 1;
            EGrade grade = GradeHelper.GetGradeByLevel(level);
            int mental = condition != null ? Mathf.RoundToInt(condition.CurrentSanity) : 100;

            int index = FindMemberIndex(name);

            if (index >= 0)
            {
                // 이미 있으면 "현재값으로 강제 갱신"
                teamMembers[index].grade = grade;
                teamMembers[index].CurrentMental = mental;

                memberUIs[index].SetMental(mental);
                memberUIs[index].Upgrade(grade);
            }
            else
            {
                // 없으면 새로 추가
                AddMember(new TeamMemberData
                {
                    nickName = name,
                    grade = grade,
                    CurrentMental = mental
                });
            }
        }
    }

    private int FindMemberIndex(string nickName)
    {
        for (int i = 0; i < teamMembers.Length; i++)
        {
            if (teamMembers[i] != null && teamMembers[i].nickName == nickName)
                return i;
        }
        return -1;
    }

    // 닉네임 기반 플레이어 정신력 갱신
    public void UpdateMemberMentalByName(string nickName, int mental)
    {
        for (int i = 0; i < teamMembers.Length; i++)
        {
            var data = teamMembers[i];
            if (data != null && data.nickName == nickName)
            {
                data.CurrentMental = mental;
                memberUIs[i].SetMental(mental);
                return;
            }
        }
    }
}

