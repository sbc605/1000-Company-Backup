using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Vivox;

// 코드 담당자: 김수아
/// <summary>
/// 실행 흐름: OnSceneLoadDone으로 Office 씬에 입장할때 캔버스가 켜짐 -> 들어온 플레이어 순서대로 ChatPrefab 생성
/// </summary>
public class UI_VoiceChat : MonoBehaviour
{
    [SerializeField] private Chat[] chatSlots; // 미리 배치된 1~4개 슬롯

    /// slot에 어떤 DisplayName이 들어있는지 추적
    private Dictionary<string, Chat> chatDict = new();

    private void OnEnable()
    {
        // Vivox 이벤트 구독
        VivoxManager.Instance.OnParticipantChangedEvent += OnParticipantChanged;
        VivoxManager.Instance.OnSpeechDetectedEvent += OnSpeechDetected;

        // 켜질 때 즉시 참가자 UI 동기화
        var participants = VivoxManager.Instance.participantsList;
        OnParticipantChanged(participants);
    }

    private void OnDisable()
    {
        VivoxManager.Instance.OnParticipantChangedEvent -= OnParticipantChanged;
        VivoxManager.Instance.OnSpeechDetectedEvent -= OnSpeechDetected;
    }

    /// <summary>
    /// Vivox 참가자 전체 목록 기반으로 슬롯 0번부터 재배치
    /// PlayerRef.PlayerId 기준으로 정렬 -> 모든 클라이언트 동일한 순서
    /// </summary>
    public void OnParticipantChanged(List<VivoxParticipant> participants)
    {
        if (participants == null)
            return;

        // 기존 슬롯 초기화
        chatDict.Clear();
        foreach (var slot in chatSlots)
        {
            slot.Setup(""); // 이름 비우기
            slot.SetMicActive(false); // 마이크 끄기
            slot.gameObject.SetActive(false);
        }

        if (participants == null || participants.Count == 0)
            return;

        // PlayerNameSync 레지스트리 가져오기
        List<PlayerNameSync> players = new(PlayerNameSync.playerNameSlots.Values);

        // PlayerId 기준 정렬(List 오름차순)
        players.Sort((a, b) => a.Object.InputAuthority.PlayerId.CompareTo(b.Object.InputAuthority.PlayerId));

        // 슬롯 인덱스를 PlayerId가 아니라 정렬된 순서로 사용
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];

            // Vivox 매칭
            VivoxParticipant vivox = participants.Find(v => v != null && !string.IsNullOrEmpty(v.DisplayName) && v.DisplayName == p.PlayerName);

            var slot = chatSlots[i];
            slot.Setup(vivox.DisplayName);
            slot.SetMicActive(false);
            slot.gameObject.SetActive(true);

            chatDict[vivox.DisplayName] = slot;
        }
    }

    /// <summary>
    /// 음성 감지 -> Mic Icon On/Off
    /// </summary>
    private void OnSpeechDetected(string playerName, bool speaking)
    {
        if (chatDict.TryGetValue(playerName, out var chat))
        {
            chat.SetMicActive(speaking);
        }
    }
}