using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
public class MagicCircleCore : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private MagicCircleDetector detector;
    [SerializeField] private MagicCircleGauge gauge;
    [SerializeField] private MagicCircleFX fx;
    [SerializeField] private MagicCircleMusic music;
    [SerializeField] private RangeTrigger rangeTrigger;
    [SerializeField] private GhostRoom ghostRoom; // 서버에서 Set

    [Header("Items/Result")]
    [SerializeField] private HashSet<int> collectedItemIDs = new();
    [SerializeField] private float musicFadeSeconds = 5f;

    [Networked, Capacity(1)] public float ExorcismProgress { get; private set; } // 0~1
    [Networked] public int PlayerCount { get; private set; } // 0~4
    [Networked] public NetworkBool IsProcessing { get; private set; } // 진행 페이즈 여부

    // 내부 상태
    private bool _isResolving;
    private bool _spawning; // 진행도 절반 됐을 때
    private Vector3 _circlePos;
    private bool _correctItems;

    private TickTimer _noPlayerTimer;
    private float _noPlayerSeconds = 3f;
    private bool _hasPlayerInCircle; // 마법진에 플레이어가 1명 이상
    private NetworkBool prevProcessing;

    #region ==== Unity/Fusion ====

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            ExorcismProgress = 0f;
            IsProcessing = false;
            _isResolving = false;
            _circlePos = transform.position;
            _noPlayerTimer = default;
            _hasPlayerInCircle = false;
            _spawning = false;

            detector.Server_OnSectorChanged += OnServerSectorsChanged;
            rangeTrigger.OnItemEnterServer += HandleItemEnterServer;
        }

        fx.ShowDefault();
        gauge.SetGaugeActive(false);
        prevProcessing = IsProcessing;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasStateAuthority)
        {
            detector.Server_OnSectorChanged -= OnServerSectorsChanged;
            rangeTrigger.OnItemEnterServer -= HandleItemEnterServer;

            StopAllCoroutines();

            // 방에 등록된 마법진 해제
            if (ghostRoom && ghostRoom.Object && ghostRoom.Object.HasStateAuthority)
                ghostRoom.ClearActiveCircle(this);
        }
    }

    public override void Render()
    {
        if (prevProcessing != IsProcessing)
        {
            if (IsProcessing) gauge.OnBeginProcessing();
            else gauge.ForceHideGauge();

            prevProcessing = IsProcessing;
        }
    }

    public void Server_SetOwnerRoom(GhostRoom room)
    {
        if (!Object.HasStateAuthority) return;
        ghostRoom = room;
        ghostRoom?.SetActiveCircle(this);
    }

    public void Server_Begin()  // ExorcismItem에서 마법진 생성 후 호출
    {
        if (!Object.HasStateAuthority) return;

        _circlePos = transform.position;
        _noPlayerTimer = default;
        _hasPlayerInCircle = false;
        _spawning = false;
        _isResolving = false;
        ExorcismProgress = 0f;

        GhostSpawner.Instance.ExorcismState = GhostSpawner.EExorcismState.InProgress;

        fx.ShowDefault();
        gauge.SetGaugeActive(false);
        rangeTrigger.Server_ScanExorcismItems();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object == null || !Object.IsValid) return;
        if (!Object.HasStateAuthority) return;

        PlayerCount = detector.ApproachSectorCount;

        var gs = GhostSpawner.Instance;
        var ghostType = gs.mapGhostType;
        var ghostData = gs._mapGhostDatabase.GetGhost(ghostType);

        // 섹터 충족하면 Processing 시작
        if (!IsProcessing && detector.IsSectorRequired)
            StartProcessing();

        // 진행 중일 때만 게이지 상승
        if (IsProcessing && !_isResolving)
        {
            float timer = (float)Runner.DeltaTime;
            float delta = gauge.EvaluateDelta(PlayerCount, timer); // 0명일 땐 0
            if (delta > 0f)
                ExorcismProgress = Mathf.Min(1f, ExorcismProgress + delta);

            // Progress 절반 채우면 스폰
            if (ExorcismProgress >= 0.5f && !_spawning)
            {
                _spawning = true;

                if (!gs.IsRetry)
                    gs.SpawnGhost(ghostType, ghostData, _circlePos, GhostController.EGhostState.Exorcism, null, false, false);

                else // 재시도의 경우 realGhost 소환                
                    gs.SpawnGhost(ghostType, ghostData, _circlePos, GhostController.EGhostState.Exorcism, null, false, true);

                var gc = gs.GhostController;
                gc.Rpc_PlayExorcismAnim();
            }

            if (_spawning) // 귀신 있는데 마법진에 아무도 없으면 Hunting 상태로 변경
            {
                if (PlayerCount == 0 && !_hasPlayerInCircle) // 안 → 밖으로 나갔을 때
                {
                    _hasPlayerInCircle = true;
                    _noPlayerTimer = TickTimer.CreateFromSeconds(Runner, _noPlayerSeconds);
                    Debug.Log("[MagicCircle] 모든 플레이어 퇴장 → 타이머 시작");
                }

                if (PlayerCount > 0 && _hasPlayerInCircle)
                {
                    _hasPlayerInCircle = false;
                    _noPlayerTimer = default;
                    Debug.Log("[MagicCircle] 플레이어 재입장 → 타이머 중단");
                }

                if (_hasPlayerInCircle && _noPlayerTimer.Expired(Runner))
                {
                    ExorcismStop();
                    return;
                }
            }

            // 완료
            if (ExorcismProgress >= 1f && !_isResolving)
            {
                _isResolving = true;
                gauge.SetGaugeActive(false);

                if (_correctItems) OnExorcismSuccess();
                else OnExorcismFail();
            }
        }
    }

    private void StartProcessing()
    {
        if (IsProcessing) return;

        IsProcessing = true;
        _spawning = false;

        // 게이지 On, 음악 시작
        gauge.OnBeginProcessing();
        music.OnProcessingStart();

        GhostSpawner.Instance.ExorcismState = GhostSpawner.EExorcismState.InProgress;
    }

    private IEnumerator ExorcismResult(bool success)
    {
        IsProcessing = !success;
        gauge.ForceHideGauge();

        var gs = GhostSpawner.Instance;
        gs.SetExorcismResult(success);

        Rpc_ShowResult(success);
        music.FadeOut(musicFadeSeconds);

        gs.DespawnCurrentGhost();

        var ghostType = gs.mapGhostType;
        var ghostData = gs._mapGhostDatabase.GetGhost(ghostType);
        var nextState = success ? GhostController.EGhostState.Dead : GhostController.EGhostState.Hunting;

        gs.SpawnGhost(ghostType, ghostData, _circlePos, nextState, null, false);
        Debug.Log($"[MagicCircle] Exorcism {(success ? "success" : "fail")} complete!");

        yield return new WaitForSeconds(5f);

        if (Object && Object.IsValid && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }

    private void OnExorcismSuccess()
    {
        Runner.StartCoroutine(ExorcismResult(true));
        //은주 추가
        RequestManager.Instance.SetRequestState(RequestState.EndReq); //의뢰 성공으로 전환
    }

    private void OnExorcismFail()
    {
        Runner.StartCoroutine(ExorcismResult(false));
    }

    private void ExorcismStop()
    {
        var gs = GhostSpawner.Instance;
        var gc = gs.GhostController;

        if (gc && !gc.Agent.enabled) gc.Agent.enabled = true;
        if (gc) gc.Agent.isStopped = false;

        GhostSpawner.Instance.ExorcismState = GhostSpawner.EExorcismState.None;

        gc.ChangeState(GhostController.EGhostState.Hunting);

        ExorcismProgress = 0f;
        _hasPlayerInCircle = false;
        _noPlayerTimer = default;
        IsProcessing = false;
        _spawning = false;

        gauge.ForceHideGauge();
        music.FadeOut(musicFadeSeconds);

        if (Object && Object.IsValid && Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
    #endregion

    #region === 아이템 처리 ====
    public void Server_AddExorcismItem(int itemId)
    {
        if (!Object.HasStateAuthority) return;
        if (itemId < 0 || collectedItemIDs.Contains(itemId)) return;

        collectedItemIDs.Add(itemId);

        EvaluateItems();
    }

    /// <summary>
    /// 제령 아이템 검증
    /// </summary>
    private void EvaluateItems()
    {
        if (ExorcismProgress >= 1f) return;

        var data = GhostSpawner.Instance._mapGhostDatabase?.GetGhost(GhostSpawner.Instance.mapGhostType);
        if (!data) return;

        var requiredSet = new HashSet<int>(data.exorcismItemIDs);

        // 개수 부족 → 대기
        if (collectedItemIDs.Count < requiredSet.Count) return;

        // 아이템 일치 여부 확인
        _correctItems = collectedItemIDs.SetEquals(requiredSet);
    }
    #endregion

    #region 범위 관련
    private void OnServerSectorsChanged(int distinctCount, bool requirementMet)
    {
        // 진행 여부 상관없이 PlayerCount는 서버에서 계속 갱신
        PlayerCount = distinctCount;

        // 아직 Processing이 아니라도 요건 충족 + 아이템 완료면 바로 Processing
        // if (!_processingStarted && requirementMet)
        //     StartProcessing();
    }
    #endregion

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ShowResult(bool success)
    {
        fx.ShowResult(success);
    }

    private void HandleItemEnterServer(int itemId)
    {
        if (!Object.HasStateAuthority) return;
        if (itemId < 0) return;

        // 중복 처리 방지(아이템 종류당 1회 인정)
        if (collectedItemIDs.Contains(itemId)) return;
        Server_AddExorcismItem(itemId);
    }
}