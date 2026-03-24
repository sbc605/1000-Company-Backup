using UnityEngine;
using Fusion;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 플레이어의 비정상 상태(빙의, 환각, 자해)를 트리거하고 AI 로직을 실행합니다.
/// 이 스크립트는 서버(StateAuthority)에서만 핵심 로직을 실행합니다.
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(PlayerCondition), typeof(PlayerInteraction))]
public class PlayerAbnormalManager : NetworkBehaviour
{
    // 1. 상태 정의
    public enum AbnormalStateType
    {
        None,
        Possession,  // 빙의
        Hallucination, // 환각
        SelfHarm     // 자해
    }

    // 빙의 상태일 때 상태
    public enum PossessionPhase
    {
        Start,
        Chase,
        Attack
    }

    [Networked]
    public AbnormalStateType CurrentAbnormalState { get; private set; } = AbnormalStateType.None;

    [Networked]
    private TickTimer _abnormalStateTimer { get; set; } // 상태 지속 시간

    [Networked]
    public TickTimer AbnormalCooldown { get; private set; }

    // 2. 컴포넌트 참조
    private PlayerController _controller;
    private PlayerCondition _condition;
    private PlayerInteraction _interaction;
    private CorpseCarryHandler _carryHandler;
    private Animator _animator;
    private NetworkCharacterController _ncc;

    [Header("Volume")]
    [SerializeField] private Volume _hallucinationVolume;
    private LensDistortion _lens;
    private ChromaticAberration _chromatic;
    private Vignette _vignette;

    // 3. AI 설정값
    [Header("AI Settings")]
    [SerializeField] private float aiRunSpeed = 3.0f;
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float damageInterval = 3f;
    [SerializeField] private float stateDuration = 10.0f;

    [Networked] private TickTimer _aiActionTimer { get; set; } // 공격,자해 간격 타이머
    [Networked] private NetworkBool _hasSelfHarmAttacked { get; set; } // 자해시 공격 여부

    // 빙의,자해 attack시 타이밍 조절용
    [Networked] private PossessionPhase _possessionPhase { get; set; }
    [Networked] private NetworkObject _attackTarget { get; set; } // 타겟 저장(서버)
    [Networked] private TickTimer _attackCooldown { get; set; }

    public override void Spawned()
    {
        _controller = GetComponent<PlayerController>();
        _condition = GetComponent<PlayerCondition>();
        _interaction = GetComponent<PlayerInteraction>();
        _carryHandler = GetComponent<CorpseCarryHandler>();
        _animator = GetComponent<Animator>();
        _ncc = GetComponent<NetworkCharacterController>();

        if (Runner.IsServer)
        {
            AbnormalCooldown = TickTimer.CreateFromSeconds(Runner, 0);
            CurrentAbnormalState = AbnormalStateType.None;
        }

        if (_hallucinationVolume.profile.TryGet(out _lens))
            _lens.intensity.value = 0f;

        if (_hallucinationVolume.profile.TryGet(out _chromatic))
            _chromatic.intensity.value = 0f;

        if (_hallucinationVolume.profile.TryGet(out _vignette))
            _vignette.intensity.value = 0f;
    }

    public bool IsControlling()
    {
        return CurrentAbnormalState == AbnormalStateType.Possession ||
               CurrentAbnormalState == AbnormalStateType.SelfHarm;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (_condition.IsDead)
        {
            CurrentAbnormalState = AbnormalStateType.None;
            return;
        }

        CheckAndTriggerAbnormalState();

        if (CurrentAbnormalState != AbnormalStateType.None)
        {
            if (_abnormalStateTimer.Expired(Runner))
            {
                StopAbnormalState();
            }
            else
            {
                switch (CurrentAbnormalState)
                {
                    case AbnormalStateType.Possession:
                        RunPossessionAI();
                        break;
                    case AbnormalStateType.SelfHarm:
                        RunSelfHarmAI();
                        break;
                }
            }
        }
    }

    /// <summary>
    /// [서버 전용] 정신력을 체크하여 상태이상을 발동시킵니다.
    /// </summary>
    private void CheckAndTriggerAbnormalState()
    {
        float sanityThreshold = _condition.MaxSanity * 0.15f;

        if (CurrentAbnormalState == AbnormalStateType.None &&
            AbnormalCooldown.Expired(Runner) &&
            _condition.CurrentSanity <= sanityThreshold)
        {
            // Unity Random 사용 (서버에서만 실행되므로 동기화 문제 없음)
            // 20% 확률로 상태 발동
            if (Random.value < 0.90f)
            {
                // int stateTypeIndex = Random.Range(1, 4); // 1, 2, 3 중 하나
                int stateTypeIndex = 3;
                AbnormalStateType chosenState = (AbnormalStateType)stateTypeIndex;

                Rpc_StartAbnormalState(chosenState, stateDuration);
            }
            else
            {
                AbnormalCooldown = TickTimer.CreateFromSeconds(Runner, 1.0f);
            }
        }
    }

    // --- C. AI 실행 로직 (서버 전용) ---

    /// <summary>
    /// 빙의: 가장 가까운 플레이어 탐색 → 공격 거리까지 이동 → 상태 체력 -1
    /// </summary>
    private void RunPossessionAI()
    {
        Debug.Log($"[AI] 빙의 상태 실행 중 | Player: {Object.Id}");

        PlayerController nearestAlly = FindNearestAlly();

        // Phase에 따라 기본 속도 세팅
        if (_possessionPhase == PossessionPhase.Start || _possessionPhase == PossessionPhase.Attack)
        {
            _ncc.maxSpeed = 0f;
        }
        else
        {
            _ncc.maxSpeed = aiRunSpeed;
        }

        // =====================
        // START PHASE
        // =====================
        if (_possessionPhase == PossessionPhase.Start)
        {
            _animator.SetBool("IsChasing", false);
            _ncc.Move(Vector3.zero); // 강제 이동 차단
            return;
        }

        // =====================
        // ATTACK PHASE
        // =====================
        if (_possessionPhase == PossessionPhase.Attack)
        {
            _animator.SetBool("IsChasing", false);
            _ncc.Move(Vector3.zero);
            return;
        }

        // =====================
        // CHASE PHASE
        // =====================
        if (nearestAlly == null)
        {
            _animator.SetBool("IsChasing", false);
            _ncc.Move(Vector3.zero);
            return;
        }

        Vector3 dir = nearestAlly.transform.position - transform.position;
        dir.y = 0f;
        float distance = dir.magnitude;

        // 쫓는 플레이어 방향 보기
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Runner.DeltaTime * 8f // 회전 속도 (값 키우면 빨리 돎)
            );
        }

        bool phaseStart = _possessionPhase == PossessionPhase.Start;

        // Start 상태가 아니고 쿨타임 중이면 추적
        if (!phaseStart && _attackCooldown.IsRunning && !_attackCooldown.Expired(Runner))
        {
            _animator.SetBool("IsChasing", true);
            _ncc.Move(dir.normalized);
            return;
        }

        // 공격 범위 진입
        if (distance <= attackDistance)
        {
            _possessionPhase = PossessionPhase.Attack;

            _attackTarget = nearestAlly.Object;

            Rpc_TriggerAttackAnim();

            _attackCooldown = TickTimer.CreateFromSeconds(Runner, damageInterval);
        }
        else
        {
            _animator.SetBool("IsChasing", true);
            _ncc.Move(dir.normalized);
        }
    }

    /// <summary>
    /// 자해: 전방 벽 감지 → 벽까지 이동 → 본인 체력 -1
    /// </summary>
    private void RunSelfHarmAI()
    {
        Debug.Log($"[AI] 자해 상태 실행 중 | Player: {Object.Id}");

        if (_hasSelfHarmAttacked)
        {
            _ncc.Move(Vector3.zero);
            return;
        }

        if (_interaction.HasValidHit)
        {
            float distanceToWall = _interaction.HitInfo.distance;
            Vector3 forwardDirection = transform.forward;
            forwardDirection.y = 0;

            if (distanceToWall > 1.0f)
            {
                _ncc.maxSpeed = aiRunSpeed;
                _ncc.Move(forwardDirection);
            }
            else
            {
                _ncc.Move(Vector3.zero);
                Rpc_TriggerSelfHarmAnim();
                _hasSelfHarmAttacked = true;
            }
        }
        else
        {
            _ncc.maxSpeed = aiRunSpeed;
            _ncc.Move(transform.forward);
        }
    }

    private PlayerController FindNearestAlly()
    {
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController nearest = null;
        float minDistance = float.MaxValue;

        foreach (var player in allPlayers)
        {
            if (player.Object.Id == this.Object.Id) continue;
            if (player.TryGetComponent<PlayerCondition>(out var cond) && cond.IsDead) continue;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = player;
            }
        }
        return nearest;
    }

    // --- D. RPCs (상태 동기화) ---

    [Rpc(RpcSources.StateAuthority, RpcTargets.StateAuthority)]
    public void Rpc_StartAbnormalState(AbnormalStateType stateType, float duration)
    {
        if (CurrentAbnormalState != AbnormalStateType.None) return;

        Debug.Log($"[Abnormal] 상태 시작: {stateType} | Player: {Object.Id}");

        // 시체 들고 있으면 내려놓기 
        if (_carryHandler != null && _carryHandler.IsCarryingCorpse)
        {
            Vector3 dropPos = transform.position + transform.forward * 0.5f;
            _carryHandler.ForceDrop(dropPos);
        }

        CurrentAbnormalState = stateType;
        _abnormalStateTimer = TickTimer.CreateFromSeconds(Runner, duration);
        AbnormalCooldown = TickTimer.CreateFromSeconds(Runner, duration + 5.0f);

        if (stateType == AbnormalStateType.SelfHarm)
        {
            _aiActionTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
            _hasSelfHarmAttacked = false;
            _controller.isInputLocked = true;
        }

        if (stateType == AbnormalStateType.Possession)
        {
            _controller.isInputLocked = true;

            _possessionPhase = PossessionPhase.Start;
            _attackTarget = null;
            _aiActionTimer = TickTimer.None; // 쿨타임 초기화
        }
    }

    public void StopAbnormalState()
    {
        if (CurrentAbnormalState == AbnormalStateType.None) return;

        Debug.Log($"[Abnormal] 상태 종료: {CurrentAbnormalState} | Player: {Object.Id}");

        _controller.isInputLocked = false;
        CurrentAbnormalState = AbnormalStateType.None;
    }

    // --- E. 개별 액션 RPCs (애니메이션 동기화) ---

    /// <summary>
    /// 빙의시 다른 플레이어 공격 애니메이션 트리거
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_TriggerAttackAnim()
    {
        _animator.SetTrigger("Attack");
    }

    /// <summary>
    /// 벽에 도착했을 때 자해 애니메이션 실행용 트리거
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_TriggerSelfHarmAnim()
    {
        _animator.SetTrigger("Headbang");
    }

    // SelfHarm 애니메이션 이벤트 호출
    public void OnSelfHarmStart()
    {
        if (!Object.HasStateAuthority) return;

        _condition.Rpc_TakeDamage(1);
    }

    // Possession_Start 애니메이션 이벤트 끝에서 호출
    // 애니메이션 진행 중 움직이지 않도록 하기 위함
    public void OnPossessionStartEnd()
    {
        if (!Object.HasStateAuthority) return;

        _possessionPhase = PossessionPhase.Chase;
    }

    // Possession_Attack 애니메이션 이벤트에서 호출 (서버만)
    public void OnPossessionAttackHit()
    {
        if (!Object.HasStateAuthority) return;
        if (_attackTarget == null) return;

        if (_attackTarget.TryGetComponent<PlayerCondition>(out var cond))
        {
            if (!cond.IsDead)
                cond.Rpc_TakeDamage(1);
        }
    }

    public void OnPossessionAttackEnd()
    {
        if (!Object.HasStateAuthority) return;

        _attackTarget = null;
        _possessionPhase = PossessionPhase.Chase;
    }

    // --- F. Render (환각 효과) ---

    public override void Render()
    {
        // 애니메이션 동기화
        bool isPossessed = CurrentAbnormalState == AbnormalStateType.Possession;
        bool isSelfHarm = CurrentAbnormalState == AbnormalStateType.SelfHarm;
        bool isHallucinating = CurrentAbnormalState == AbnormalStateType.Hallucination;

        if (_animator != null)
        {
            _animator.SetBool("IsPossessed", isPossessed);
            _animator.SetBool("IsSelfHarming", isSelfHarm);
            _animator.SetBool("IsHallucinating", isHallucinating);
        }

        // 여기서부터 로컬 전용 효과
        if (!Object.HasInputAuthority) return;
        if (_lens == null || _chromatic == null || _vignette == null) return;

        if (isHallucinating)
        {
            float time = Time.time;

            // 화면 요동치도록
            float wave = Mathf.Sin(time * 4f) * 0.2f;
            float noise = (Mathf.PerlinNoise(time * 2f, 0f) - 0.5f) * 0.3f;

            float distortionTarget = -0.5f + wave + noise;
            _lens.intensity.value = Mathf.Lerp(_lens.intensity.value, distortionTarget, Time.deltaTime * 5f);

            // 색수차 점점 증가
            float chromaTarget = 0.5f + Mathf.Abs(wave) * 0.3f;
            _chromatic.intensity.value = Mathf.Lerp(_chromatic.intensity.value, chromaTarget, Time.deltaTime * 3f);

            // 카메라 FOV 흔들기
            _controller.playerCamera.fieldOfView = 60f + Mathf.Sin(Time.time * 5f) * 2f;

            // Vignet 사용
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0.5f, Time.deltaTime * 2f);
        }
        else
        {
            _lens.intensity.value = Mathf.Lerp(_lens.intensity.value, 0f, Time.deltaTime * 3f);
            _chromatic.intensity.value = Mathf.Lerp(_chromatic.intensity.value, 0f, Time.deltaTime * 3f);
            _controller.playerCamera.fieldOfView = Mathf.Lerp(_controller.playerCamera.fieldOfView, 60f, Time.deltaTime * 3f);
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, 0f, Time.deltaTime * 2f);
        }
    }

    public void ForceStopAbnormal()
    {
        if (!Object.HasStateAuthority) return;

        StopAbnormalState();
    }
}