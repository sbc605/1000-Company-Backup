using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

// 코드 담당자: 김수아
/// <summary>
/// Vivox 초기화 / 방 ID로 채널 관리(Join/Leave)
/// 사망시 Ghost채널로 이동해 음성 분리
/// </summary>
public class VivoxManager : MonoBehaviour
{
    public static VivoxManager Instance;
    public bool MainChannelConnected { get; set; }
    public bool IsInPositionalChannel { get; set; }
    private bool isReconnecting = false;

    // room 채널 이름들
    public string RoomId { get; private set; }

    [Header("Default Channels")]
    [Tooltip("캐싱할 채널 이름")]
    public string mainChannel;
    private string ghostChannel;

    [Header("3D Voice Settings")]
    [SerializeField] private int audioDistance = 64; // 가청거리 : 어디까지 목소리 들릴지(기본 32. 현재 2배 거리)
    [SerializeField] private int conversationalDistance = 2; // 작아지기 시작하는 거리
    [SerializeField] private float audioFadeInByDistance = 0.5f; // 값이 1.0보다 크면 대화 거리에서 멀어질수록 오디오가 더 빨리 사라짐, 값이 1.0보다 작으면 오디오가 더 느리게 사라짐. 기본값은 1.0.
    [SerializeField] private float defaultVolumeMultiplier = 2.0f;

    private bool isDead = false;
    private bool isMainJoined = false;
    private bool isGhostJoined = false;

    private bool initialized = false;
    private bool loggedIn = false;

    // UI를 위한 Actions
    public Action<List<VivoxParticipant>> OnParticipantChangedEvent; // UI에 인원 추가 전달용
    public Action<string, bool> OnSpeechDetectedEvent; // 로컬이 말하는 거 감지
    public Action<string, float> OnVolumeChangedEvent; // 볼륨 슬라이더 조절용
    public Action<string, bool> OnMuteChangedEvent; // 음성 뮤트용

    public List<VivoxParticipant> participantsList = new();

    // 네트워크 초기화 시점 때문에 싱글톤 상속받지 않고 직접 제어
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnParticipantAdded(VivoxParticipant participant)
    {
        participantsList.Add(participant);
        OnParticipantChangedEvent?.Invoke(participantsList);

        // 음성 감지 이벤트 연결
        participant.ParticipantSpeechDetected += () =>
        {
            Debug.Log($"{participant.DisplayName} Speaking: {participant.SpeechDetected}");
            OnSpeechDetectedEvent?.Invoke(participant.DisplayName, participant.SpeechDetected);
        };
    }

    private void OnParticipantRemoved(VivoxParticipant participant)
    {
        participantsList.Remove(participant);
        OnParticipantChangedEvent?.Invoke(participantsList);
    }

    /// <summary>
    /// Vivox 서버 초기화 함수
    /// </summary>
    public async Task InitVivox()
    {
        if (initialized) return;
        initialized = true;

        await UnityServices.InitializeAsync(); //유니티 서비스 초기화
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //AuthenticationService를 사용하여 익명 인증
        await VivoxService.Instance.InitializeAsync(); //Vivox 초기화

        Debug.LogWarning($"[Vivox] Unity PlayerId = {AuthenticationService.Instance.PlayerId}");

        // 초기화 후 이벤트 등록
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;
    }

    /// <summary>
    /// Vivox 로그인 -> 초기화 이후 호출
    /// </summary>
    public async Task LoginVivox(string playerName)
    {
        if (loggedIn) return;
        loggedIn = true;

        try
        {
            //로그인 옵션 생성
            LoginOptions options = new LoginOptions();

            //디스플레이 이름 설정
            options.DisplayName = playerName;

            //로그인
            await VivoxService.Instance.LoginAsync(options);
            Debug.Log($"Vivox 로그인: {playerName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox 로그인 실패 : {e}");
        }
    }

    /// <summary>
    /// Vivox 로그아웃 함수
    /// </summary>
    public async Task VivoxLogoutAsync()
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        try
        {
            await VivoxService.Instance.LogoutAsync();
            Debug.Log("Vivox 로그아웃");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox 로그아웃 실패 {e}");
        }
    }

    /// <summary>
    /// 룸 입장 시 호출 (roomID에 따라 채널 자동 설정)
    /// 3D 채널을 사용해 음성 거리별 효과 줌
    /// </summary>
    public async Task JoinMainChannel(string roomId)
    {
        if (!VivoxService.Instance.IsLoggedIn) return;

        RoomId = roomId;
        mainChannel = $"Room_{RoomId}_Main";

        var props = new Channel3DProperties(audioDistance, conversationalDistance, audioFadeInByDistance, AudioFadeModel.InverseByDistance);

        try
        {
            await VivoxService.Instance.JoinPositionalChannelAsync(mainChannel, ChatCapability.AudioOnly, props);

            IsInPositionalChannel = true;
            MainChannelConnected = true;
            isMainJoined = true;
            Debug.Log($"[VivoxVoice] Joined MainChannel: {mainChannel}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox 채널 참여 실패 {e}");
        }
    }

    public async Task JoinGhostChannel(string roomId)
    {
        ghostChannel = $"Room_{roomId}_Ghost";

        try
        {
            // Ghost채널은 거리별 볼륨 변화 없음
            await VivoxService.Instance.JoinGroupChannelAsync(ghostChannel, ChatCapability.AudioOnly);
            isGhostJoined = true;
            Debug.Log($"[Vivox] Joined Ghost 2D Channel: {ghostChannel}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ghost 채널 참가 실패 {e}");
        }
    }

    /// <summary>
    /// GhostChannel에서 MainChannel의 목소리를 들을때 사용합니다.
    /// 듣기만 가능한 채널로 동작하도록 Join 후 음소거 처리
    /// </summary>
    public async Task JoinListenOnlyChannel(string channelName)
    {
        try
        {
            // mainChannel null인지 검사하고 필요시 RoomId로 재생성
            if (string.IsNullOrEmpty(channelName))
            {
                if (string.IsNullOrEmpty(RoomId))
                {
                    Debug.LogError("[Vivox] JoinListenOnlyChannel 실패: RoomId가 비어 있음");
                    return;
                }
                channelName = $"Room_{RoomId}_Main";
            }

            await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);
            Debug.Log($"[Vivox] Listen-Only 채널 입장 (Main 소리만 수신): {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Vivox] Listen-Only 채널 실패: {e}");
        }
    }

    public async Task LeaveMainChannel(string channelName)
    {
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelName);
            IsInPositionalChannel = false;
            MainChannelConnected = false;
            Debug.Log($"Main 채널 떠남: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox 채널 떠나기 실패 {e}");
        }
    }

    public async Task LeaveGhostChannel(string channelName)
    {
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelName);
            Debug.Log($"Ghost 채널 떠남: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Vivox 채널 떠나기 실패 {e}");
        }
    }

    /// <summary>
    /// 모든 채널 떠나기
    /// </summary>
    public async Task LeaveAllChannels()
    {
        if (isMainJoined) await VivoxService.Instance.LeaveChannelAsync(mainChannel);
        if (isGhostJoined) await VivoxService.Instance.LeaveChannelAsync(ghostChannel);
        isMainJoined = isGhostJoined = false;
    }

    /// <summary>
    /// 플레이어 생존/사망 상태에 따라 채널 변경
    /// </summary>
    public async void UpdatePlayerState(bool dead)
    {
        if (isDead == dead) return;
        isDead = dead;

        if (dead)
        {
            // Main은 유지 (절대 Leave하지 말 것)

            if (!isGhostJoined)
            {
                await JoinGhostChannel(RoomId);
                await Task.Delay(300); // 세션 안정화
            }

            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.Single, ghostChannel); // 송신을 Ghost 채널로만 고정
            await VivoxService.Instance.SetActiveInputDeviceAsync(VivoxService.Instance.ActiveInputDevice); // 마이크 스트림 재시작

            Debug.Log("Ghost 상태: Main 수신 + Ghost 송신");
        }
        else
        {
            // 송신을 전체 채널로 복귀
            await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);

            if (isGhostJoined)
            {
                await LeaveGhostChannel(ghostChannel);
                isGhostJoined = false;
            }

            Debug.Log("Main 상태 복귀");
        }
    }

    /// <summary>
    /// Vivox 연결 끊겼을시 재접속 함수
    /// </summary>
    public void ReconnectLoop()
    {
        if (!isReconnecting)
            StartCoroutine(ReconnectLoopRoutine());
    }

    private IEnumerator ReconnectLoopRoutine()
    {
        isReconnecting = true;

        Debug.LogWarning("[Vivox] 메인 채널 재접속 시도 시작");

        while (!MainChannelConnected)
        {
            if (!VivoxService.Instance.IsLoggedIn)
            {
                Debug.LogWarning("[Vivox] 로그인 안 됨 → 로그인 재시도");
                yield return LoginVivox(PlayerNameSync.Local.PlayerName);
            }

            Debug.LogWarning("[Vivox] 메인 채널 Join 재시도");
            var task = JoinMainChannel(RoomId);

            while (!task.IsCompleted)
                yield return null;

            if (!MainChannelConnected)
            {
                Debug.LogWarning("[Vivox] 재접속 실패 → 3초 후 재시도");
                yield return new WaitForSeconds(3f);
            }
        }

        Debug.LogWarning("[Vivox] 메인 채널 재접속 성공");
        isReconnecting = false;
    }

    /// <summary>
    /// 플레이어별 음량 조절(슬라이더)
    /// 로컬 클라이언트에서 상대방의 음량을 조절(다른 사람 볼륨 조절용)
    /// </summary>
    public void SetPlayerVolume(string displayName, float volume)
    {
        if (participantsList == null || participantsList.Count == 0)
            return;

        var participant = participantsList.Find(p => p.DisplayName == displayName);
        if (participant == null)
        {
            Debug.LogWarning($"[Vivox] SetPlayerVolume 실패: '{displayName}' 참가자 없음");
            return;
        }

        // volume: 0.0 ~ 1.0  → Vivox: -100 ~ 100
        // 기본 볼륨 크기 2배 적용
        float volumeUp = Mathf.Clamp01(volume * defaultVolumeMultiplier);
        int volumeValue = Mathf.RoundToInt(Mathf.Lerp(-100f, 100f, volumeUp));

        participant.SetLocalVolume(volumeValue);
        Debug.Log($"[Vivox] '{displayName}' 볼륨 설정: slider={volume:0.00}, boosted={volumeUp:0.00}, mapped={volumeValue}");
        OnVolumeChangedEvent?.Invoke(displayName, volumeUp);
    }
}