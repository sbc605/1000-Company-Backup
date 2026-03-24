using System;
using System.Globalization;
using Fusion;
using UnityEngine;
using static UnityEngine.CullingGroup;

// 작성자 : 정하윤
public abstract class ParanormalPhenomenonBase : NetworkBehaviour, IExorcisableByTalisman
{
    public enum EAbnormalState
    {
        Idle,
        Active,
        Dead
    }

    protected Transform target;

    [Networked]
    private TickTimer DespawnTimer { get; set; }
    [SerializeField] private float disappearEffectDuration = 1f;

    [Header("Effects")]
    // 사라질 때 파티클 등
    [SerializeField] protected GameObject disappearEffectPrefab;
    // 사라질 때 사운드
    [SerializeField] protected AudioClip disappearSound;

    [Networked]
    [OnChangedRender(nameof(OnStateChanged))]
    protected EAbnormalState CurrentState { get; set; } = EAbnormalState.Idle;
    private void Awake()
    {
        // 부모 코드

        // 자식 코드
        WakeUp();
    }

    //private void Start()
    //{
    //    // 부모 코드

    //    // 자식 코드
    //    Initialize();
    //}

    /// <summary>
    /// Awake에서 수행하고 싶은 코드 작성
    /// </summary>
    protected virtual void WakeUp() { }

    /// <summary>
    /// Start에서 수행하고 싶은 코드 작성
    /// </summary>
    protected virtual void Initialize() { }

    public override void FixedUpdateNetwork() 
    {
        if (!HasStateAuthority)
            return;

        switch (CurrentState)
        {
            case EAbnormalState.Idle:
                Idle();
                break;
            case EAbnormalState.Active:
                ActivatePhenomenon();
                break;
            case EAbnormalState.Dead:
                if (DespawnTimer.Expired(Runner))
                {
                    // 타이머 리셋
                    DespawnTimer = TickTimer.None;

                    // 실제 디스폰 실행
                    Disappear();
                }
                break;
        }
    }

    public virtual void Idle() { }

    protected void ChangeState(EAbnormalState state)
    {
        CurrentState = state;
    }

    // 현재 상태가 바뀌면 호출되는 함수
    protected void OnStateChanged()
    {
        // Dead 상태로 변경되었다면
        if (CurrentState == EAbnormalState.Dead)
        {
            PlayDisappearEffects();
        }
        // Active 상태로 변경되었다면
        else if (CurrentState == EAbnormalState.Active)
        {
        }
    }

    /// <summary>
    /// 이상현상 상태
    /// </summary>
    public virtual void ActivatePhenomenon() { }

    /// <summary>
    /// 플레이어가 부적 사용했을 때 구현할 내용
    /// </summary>
    public virtual void ApplyTalisman()
    {
        Rpc_SetStateDead();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_SetStateDead(RpcInfo info = default)
    {
        // 서버가 상태 전환 -> [Networked]로 클라이언트에게 전송
        if (CurrentState != EAbnormalState.Dead)
        {
            CurrentState = EAbnormalState.Dead;

            // 타이머 시작
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, disappearEffectDuration);
        }
    }

    /// <summary>
    /// 픒레이어가 부적을 사용하여 퇴치될 때 처리
    /// </summary>
    protected virtual void Disappear() 
    {
        Runner.Despawn(Object);
    }

    protected virtual void PlayDisappearEffects()
    {
        if (disappearEffectPrefab)
        {
            Instantiate(disappearEffectPrefab, transform.position, Quaternion.identity);
        }

        if (disappearSound)
        {
            AudioSource.PlayClipAtPoint(disappearSound, transform.position);
        }
    }
}
