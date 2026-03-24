// 코드 담당자 최우석
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Fusion;

public class Darkness : ParanormalPhenomenonBase
{
    private enum DarknessSubState
    {
        Active,     // 활성화 (커지는 중)
        Debuffing,  // 디버프 (최대 크기 도달)
        Vulnerable  // 최소 크기 도달 (퇴치 가능)
    }

    [Networked] private DarknessSubState _subState { get; set; } = DarknessSubState.Active;


    [Header("크기 설정")]
    [SerializeField] private float startSize = 0.025f;
    [SerializeField] private float minSize = 0.001f;
    [SerializeField] private float maxSize = 0.05f;
    [SerializeField] private float growSpeed = 0.5f;
    [SerializeField] private float shrinkSpeed = 1.0f;

    [Header("시선 설정")]
    [SerializeField]
    [Range(0.8f, 1.0f)]
    private float lookThreshold = 0.95f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("트리거 설정")]
    [SerializeField] private Collider activationTrigger;

    [Header("디버프 설정")]
    [SerializeField] private float sanityDebuff = 5f;
    [SerializeField] private float slowDebuff = 0.5f;

    [Header("이펙트 설정")]
    [SerializeField] private Renderer eyeRenderer;
    [SerializeField] private Color debuffEmissionColor = Color.red;
    [SerializeField] private Image playerDebuffOverlay;
    [SerializeField] private float targetDebuffAlpha = 0.7f;
    [SerializeField] private float debuffFadeSpeed = 1f;

    [Header("머리 회전")]
    [SerializeField] private Transform headBone;

    [Networked] private float _currentSize { get; set; }

    private Transform _playerCameraTransform;
    private PlayerController _playerController;
    private PlayerCondition _playerCondition;

    private float _cachedWalkSpeed;
    private float _cachedRunSpeed;
    private Coroutine _sanityDrainCoroutine;
    private Coroutine _fadeCoroutine;

    private Color _originalEmissionColor;
    private Material _eyeMaterialInstance;

    protected override void WakeUp()
    {
        base.WakeUp(); // 부모 WakeUp() 호출

        transform.localScale = Vector3.one * startSize;

        if (activationTrigger == null)
        {
            Debug.LogWarning("Darkness 활성화 트리거가 설정되지 않았습니다.", this);
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        if (Object.HasStateAuthority)
        {
            _currentSize = startSize;
            _subState = DarknessSubState.Active;
        }

        Initialize();
    }

    protected override void Initialize()
    {
        base.Initialize(); // 부모 Initialize() 호출

        // [수정] 플레이어 찾기 로직을 Initialize에서 제거 (필요할 때마다 찾도록 변경)
        // FindPlayerComponents(); 

        if (eyeRenderer != null)
        {
            _eyeMaterialInstance = eyeRenderer.materials[2];

            if (_eyeMaterialInstance.HasProperty("_EmissionColor"))
            {
                _originalEmissionColor = _eyeMaterialInstance.GetColor("_EmissionColor");
            }
        }
        else
        {
            Debug.LogWarning("Darkness: Eye Renderer가 할당되지 않았습니다.");
        }
    }

    // [수정] 플레이어 컴포넌트를 찾고, 성공 여부를 bool로 반환하는 헬퍼 함수
    private bool TryEnsurePlayerComponents()
    {
        // 이미 컴포넌트를 찾았다면 true 반환
        if (_playerController != null && _playerCondition != null && _playerCameraTransform != null)
        {
            return true;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            // 플레이어가 아직 스폰되지 않았을 수 있으므로 오류 대신 false 반환
            return false;
        }

        _playerController = playerObj.GetComponent<PlayerController>();
        _playerCondition = playerObj.GetComponent<PlayerCondition>();

        if (_playerController != null)
        {
            _playerCameraTransform = _playerController.playerCamera.transform;
            _cachedWalkSpeed = _playerController.walkSpeed;
            _cachedRunSpeed = _playerController.runSpeed;
        }
        else
        {
            return false;
        }

        if (_playerCondition == null)
        {
            return false;
        }

        // 모든 컴포넌트를 성공적으로 찾았으면 true 반환
        return true;
    }


    public override void Idle()
    {
        // 호스트만 활성화 트리거 검사
        if (Object.HasStateAuthority)
        {
            CheckActivationTrigger();
        }
    }

    public override void ActivatePhenomenon()
    {
        // 호스트만 이상현상 로직(상태 변경, 크기 조절) 실행
        if (Object.HasStateAuthority)
        {
            RunAnomalyLogic();
        }

        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * _currentSize, Time.deltaTime * 10f);
    }

    // [수정] 부적 퇴치 로직 (다른 현상과 통일)
    protected override void Disappear()
    {
        // [수정] 이 함수가 Dead 상태에서 매 틱 호출되는 것을 방지
        // 이미 Despawn이 시작되었다면(Object가 유효하지 않다면) 즉시 리턴
        if (Object == null || !Object.IsValid)
        {
            return;
        }

        // --- 여기부터는 단 한 번만 실행됨 ---

        // 1. Darkness 고유의 정리 작업 (디버프 해제)
        ApplyDebuff(false);

        // 2. 다른 이상현상과 "똑같이" 부모의 Disappear를 호출합니다.
        // (부모가 이펙트 재생 및 Runner.Despawn을 처리)
        base.Disappear();
    }

    void LateUpdate()
    {
        if (CurrentState == EAbnormalState.Idle || headBone == null)
        {
            return;
        }

        // [수정] 플레이어 컴포넌트가 있는지 확인
        if (!TryEnsurePlayerComponents())
        {
            // 플레이어를 못찾으면 머리 회전 로직 스킵
            return;
        }

        // 머리 회전 로직 (시각적 요소이므로 모든 클라이언트에서 실행)
        Vector3 directionToPlayer = _playerCameraTransform.position - headBone.position;
        Vector3 reverseTargetPosition = headBone.position - directionToPlayer;
        headBone.LookAt(reverseTargetPosition, Vector3.down);
    }

    /// <summary>
    /// 이상현상 핵심 로직 (호스트에서만 실행됨)
    /// </summary>
    private void RunAnomalyLogic()
    {
        if (!TryEnsurePlayerComponents()) return;
        int lookingPlayerCount = CountLookingPlayers();

        if (_subState == DarknessSubState.Active)
        {
            if (lookingPlayerCount == 0)
            {
                _currentSize = Mathf.MoveTowards(_currentSize, maxSize, growSpeed * Time.deltaTime);
                if (Mathf.Approximately(_currentSize, maxSize))
                {
                    _subState = DarknessSubState.Debuffing;
                    ApplyDebuff(true); // 최대 크기 도달 시 디버프 켬
                }
            }
        }
        else if (_subState == DarknessSubState.Debuffing)
        {
            if (lookingPlayerCount >= 1) // 쳐다봄 -> 작아짐
            {
                // [수정] 디버프를 끄는 로직 제거. Disappear()에서만 끄도록 함.
                // if (Mathf.Approximately(_currentSize, maxSize))
                // {
                //     ApplyDebuff(false);
                // }

                _currentSize = Mathf.MoveTowards(_currentSize, minSize, shrinkSpeed * Time.deltaTime);

                if (Mathf.Approximately(_currentSize, minSize))
                {
                    _subState = DarknessSubState.Vulnerable;
                }
            }
            else // 안 쳐다봄 -> 다시 커짐
            {
                _currentSize = Mathf.MoveTowards(_currentSize, maxSize, growSpeed * Time.deltaTime);
                if (Mathf.Approximately(_currentSize, maxSize))
                {
                    ApplyDebuff(true); // 다시 최대 크기 도달 시 디버프 켬
                }
            }
        }
        else if (_subState == DarknessSubState.Vulnerable)
        {
            // 퇴치 가능 상태 (최소 크기)
            if (lookingPlayerCount == 0)
            {
                // 안 쳐다보면 다시 'Debuffing' 상태로 돌아가서 커짐
                _subState = DarknessSubState.Debuffing;
            }
            // (쳐다보고 있으면 최소 크기 유지, ApplyTalisman 대기)
        }
    }

    /// <summary>
    /// 플레이어가 쳐다보고 있는지 카운트 (현재 로직은 단일 플레이어 기준)
    /// </summary>
    private int CountLookingPlayers()
    {
        // TryEnsurePlayerComponents가 이미 호출되었으므로 null 체크가 보장됨
        if (_playerCameraTransform == null)
        {
            return 0;
        }

        Vector3 anomalyPosition = transform.position;
        Vector3 camPos = _playerCameraTransform.position;
        Vector3 camForward = _playerCameraTransform.forward;
        Vector3 dirToAnomaly = (anomalyPosition - camPos).normalized;

        float dot = Vector3.Dot(camForward, dirToAnomaly);

        if (dot > lookThreshold)
        {
            float distance = Vector3.Distance(camPos, anomalyPosition);
            if (!Physics.Raycast(camPos, dirToAnomaly, distance, obstacleMask))
            {
                return 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// 활성화 트리거 검사 (호스트에서만 실행됨)
    /// </summary>
    private void CheckActivationTrigger()
    {
        if (activationTrigger == null) return;

        Collider[] colliders = Physics.OverlapBox(
            activationTrigger.bounds.center,
            activationTrigger.bounds.extents,
            Quaternion.identity
        );

        foreach (var col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                Debug.Log("Darkness 활성화!");
                ChangeState(EAbnormalState.Active);
                return;
            }
        }
    }

    /// <summary>
    /// 디버프 적용 (호스트에서만 호출됨)
    /// </summary>
    private void ApplyDebuff(bool apply)
    {
        // TryEnsurePlayerComponents가 이미 호출되었으므로 null 체크가 보장됨
        if (_playerController == null || _playerCondition == null) return;

        // [NOTE] 플레이어 속도 변경은 PlayerController가 Networked 속성으로 관리하지 않으면
        // 호스트에서만 변경되고 클라이언트에 동기화되지 않습니다.
        // PlayerController의 walkSpeed/runSpeed가 [Networked]가 아니라면 이 로직은 작동하지 않습니다.

        if (apply)
        {
            _playerController.walkSpeed = _cachedWalkSpeed * slowDebuff;
            _playerController.runSpeed = _cachedRunSpeed * slowDebuff;

            if (_sanityDrainCoroutine == null)
            {
                _sanityDrainCoroutine = StartCoroutine(SanityDrainRoutine());
            }

            if (_eyeMaterialInstance != null)
            {
                _eyeMaterialInstance.EnableKeyword("_EMISSION");
                _eyeMaterialInstance.SetColor("_EmissionColor", debuffEmissionColor);
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeDebuffOverlay(true));
        }
        else
        {
            _playerController.walkSpeed = _cachedWalkSpeed;
            _playerController.runSpeed = _cachedRunSpeed;

            if (_sanityDrainCoroutine != null)
            {
                StopCoroutine(_sanityDrainCoroutine);
                _sanityDrainCoroutine = null;
            }

            if (_eyeMaterialInstance != null)
            {
                _eyeMaterialInstance.SetColor("_EmissionColor", _originalEmissionColor);
                if (_originalEmissionColor == Color.black)
                {
                    _eyeMaterialInstance.DisableKeyword("_EMISSION");
                }
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeDebuffOverlay(false));
        }
    }

    private IEnumerator SanityDrainRoutine()
    {
        while (true)
        {
            // [수정] 플레이어가 유효한지 지속적으로 확인
            if (_playerCondition != null)
            {
                _playerCondition.Rpc_DecreaseSanity(sanityDebuff * Time.deltaTime);
            }
            yield return null;
        }
    }

    private IEnumerator FadeDebuffOverlay(bool fadeIn)
    {
        if (playerDebuffOverlay == null)
        {
            yield break;
        }

        float targetAlpha = fadeIn ? targetDebuffAlpha : 0f;
        Color currentColor = playerDebuffOverlay.color;
        float currentAlpha = currentColor.a;

        while (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, debuffFadeSpeed * Time.deltaTime);
            playerDebuffOverlay.color = new Color(currentColor.r, currentColor.g, currentColor.b, currentAlpha);
            yield return null;
        }
    }

    // [추가] 부모의 ApplyTalisman을 오버라이드하여 조건 체크
    public override void ApplyTalisman()
    {
        // 클라이언트가 호스트에게 퇴치 시도 RPC를 보냄
        Rpc_AttemptExorcism();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_AttemptExorcism(RpcInfo info = default)
    {
        // 호스트만 이 코드를 실행
        // "Vulnerable" (최소 크기) 상태일 때만 퇴치(Dead)되도록 함
        if (_subState == DarknessSubState.Vulnerable)
        {
            Debug.Log("Darkness: 최소 크기에 도달하여 퇴치 성공!");

            // 부모의 ChangeState를 사용해도 되지만,
            // 호스트에서 직접 CurrentState를 변경
            if (CurrentState != EAbnormalState.Dead)
            {
                CurrentState = EAbnormalState.Dead;
            }
        }
        else
        {
            // "Vulnerable" 상태가 아니면 부적이 먹히지 않음
            Debug.Log("Darkness: 아직 최소 크기가 아니므로 퇴치할 수 없습니다.");
            // (필요시 '실패' 사운드나 이펙트 RPC를 여기서 클라이언트로 보낼 수 있음)
        }
    }
}