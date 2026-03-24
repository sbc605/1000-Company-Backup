using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

// 코드 담당자: 김수아

public class GhostController : NetworkBehaviour
{
    #region 멤버변수
    // State Enum
    public enum EGhostState { Idle, Patrol, Chase, Attack, Hit, Dead, Hunting, Exorcism }

    // GhostData   
    [HideInInspector] public GhostData ghostData;

    // 서버에서 사용. 초기화 대기용
    public EGhostState PrevStartState { get; set; } = EGhostState.Idle;
    public Transform PrevTarget { get; set; }

    // Networked
    [Networked] public GhostSpawner.EGhost GhostType { get; private set; } // 현재 소환된 귀신의 EGhost 타입 저장  

    [Networked] public float GhostMoveSpeed { get; private set; } // GhostBaseMoveState에서 사용
    [Networked] public float WanderRadius { get; private set; } // GhostBaseMoveState에서 사용, 유령이 돌아다닐 반경
    [Networked] public float PatrolDetectionDistance { get; private set; } // 플레이어 탐색 거리
    [Networked] public NetworkBool IsCeilingSpawn { get; set; } // 천장에서 스폰되면 true

    public Transform TargetPlayer { get; set; }

    // Component
    public NavMeshAgent Agent { get; set; }
    public MeshRenderer MeshRenderer { get; set; }
    public Animator Animator { get; private set; }
    public Rigidbody Rigidbody { get; private set; }

    // 서버 전용 상태머신(서버에서만 유지)
    public GhostBaseState CurrentState;
    public Dictionary<EGhostState, GhostBaseState> _states;
    [Networked] public int CurrentAttackVariant { get; set; }

    [Header("LayerMask")]
    [SerializeField] private LayerMask targetLayerMask; // 찾고자 하는 대상의 LayerMask
    [SerializeField] private LayerMask obstacleMask;
    public LayerMask ObstacleMask => obstacleMask;

    [Header("Sound")]
    [SerializeField] private GhostSound sound;
    public GhostSound Sound => sound;

    [Header("FX")]
    [SerializeField] private GhostFX fx;
    public GhostFX FX => fx;

    #endregion

    public override void Spawned()
    {
        // 컴포넌트 할당
        Agent = GetComponent<NavMeshAgent>();
        MeshRenderer = GetComponent<MeshRenderer>();
        Animator = GetComponent<Animator>();
        Rigidbody = GetComponent<Rigidbody>();

        if (Agent) Agent.enabled = Object.HasStateAuthority; // 서버만 경로계산

        // 상태 생성 및 등록
        if (Object.HasStateAuthority)
        {
            _states = new Dictionary<EGhostState, GhostBaseState> {
            { EGhostState.Idle, new GhostStateIdle(this) },
            { EGhostState.Patrol, new GhostStatePatrol(this) },
            { EGhostState.Chase, new GhostStateChase(this) },
            { EGhostState.Attack, new GhostStateAttack(this) },
            { EGhostState.Hit, new GhostStateHit(this) },
            { EGhostState.Dead, new GhostStateDead(this) },
            { EGhostState.Hunting, new GhostStateHunting(this) },
            { EGhostState.Exorcism, new GhostStateExorcism(this) },
        };

            if (Agent)
            {
                // 제령 때 원하는 층에서 소환 유지하기위해 작은 반경으로 샘플링 → Warp
                Vector3 desired = transform.position;

                if (NavMesh.SamplePosition(desired, out var hit, 0.75f, NavMesh.AllAreas))
                    desired = hit.position;

                // Agent 활성화 전 위치 세팅
                transform.position = desired;

                Agent.enabled = true;
                Agent.Warp(desired); // NavMesh 상 위치 강제로 확정
                Agent.speed = GhostMoveSpeed;
                Agent.autoRepath = true; // 막혔을시 새 경로 계산
            }

            // Spawned 이후 초기 상태 전환
            if (PrevTarget != null) TargetPlayer = PrevTarget;
            ChangeState(PrevStartState);
        }

        else return; // 클라이언트는 애니메이터만 수신
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        var exorcismState = (GhostSpawner.Instance.ExorcismState == GhostSpawner.EExorcismState.Failed);

        if (!exorcismState)
            GhostSpawner.Instance.DespawnCurrentGhost();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        CurrentState?.ExecuteState();
    }

    public void Init(GhostSpawner.EGhost type, GhostData data, bool isBlack)
    {
        if (!Object.HasStateAuthority) return;

        GhostType = type;
        ghostData = data;

        if (isBlack)
        {
            GhostMoveSpeed = data.blackMoveSpeed;
            WanderRadius = data.blackWanderRadius;
            PatrolDetectionDistance = data.blackPatrolDetectionDistance;
        }
        else
        {
            GhostMoveSpeed = data.moveSpeed;
            WanderRadius = data.wanderRadius;
            PatrolDetectionDistance = data.patrolDetectionDistance;
        }

        // NavMeshAgent의 속도를 초기 속도로 설정
        if (Agent)
        {
            Agent.speed = GhostMoveSpeed;
        }
    }

    public void ChangeState(EGhostState newState)
    {
        // 동일 상태면 무시
        if (CurrentState != null && CurrentState.State == newState) return;

        // 새 상태 설정
        if (CurrentState != null) CurrentState.ExitState();
        CurrentState = _states[newState]; // newState에 들어온 enum을 키로 사용하여, value인 GhostBaseState 타입의 상태로 변경
        CurrentState.EnterState();
    }

    // 레지스트리에 등록된 플레이어만 감지
    public bool FindPlayerRegisteredPlayer(out Transform player)
    {
        player = null;
        if (!Object.HasStateAuthority) return false;

        float minDist = float.PositiveInfinity;
        Transform lowSanity = null;

        foreach (var pc in ServerPlayerRegistry.Players)
        {
            if (pc == null || pc.IsDead) continue;

            var pcTransform = pc.transform;

            // NavMesh 밖 플레이어는 스킵
            if (!NavMesh.SamplePosition(pcTransform.position, out var _, 0.8f, NavMesh.AllAreas))
                continue;

            float dist = Vector3.Distance(transform.position, pcTransform.position);
            if (dist > PatrolDetectionDistance) continue;

            // ObstacleMask 스킵
            if (!HasLineOfSight(pcTransform)) continue;

            // 정신력이 30% 미만인 플레이어가 있다면 1순위 추격 대상
            if (pc.CurrentSanity < 30f)
                lowSanity = pcTransform;

            if (dist < minDist)
            {
                minDist = dist;
                player = pcTransform;
            }
        }

        if (lowSanity != null)
        {
            player = lowSanity;
            return true;
        }

        // 정신력 30% 미만인 플레이어 없으면 가장 가까운 플레이어 추격
        return player != null;
    }

    public bool HasLineOfSight(Transform target)
    {
        Vector3 currentPos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = target.position + Vector3.up * 1.5f;

        if (Physics.Linecast(currentPos, targetPos, out RaycastHit hit, obstacleMask))
        {
            if (!hit.collider.CompareTag("Player"))
                return false;
        }
        return true;
    }


    /// <summary>
    ///  현재 재생 중인 애니메이션 클립 길이 반환
    /// </summary>
    public float GetCurrentStateLength(int layer = 0)
    {
        var anim = Animator;
        if (anim.IsInTransition(layer))
            return anim.GetNextAnimatorStateInfo(layer).length / Mathf.Max(0.0001f, anim.speed);

        return anim.GetCurrentAnimatorStateInfo(layer).length / Mathf.Max(0.0001f, anim.speed);
    }

    /// <summary>
    /// 상태 초기화, GameObject 파괴(서버 전용)
    /// </summary>
    public void Disappear()
    {
        if (!Object.HasStateAuthority) return;

        if (Agent && Agent.enabled) Agent.enabled = false;
        CurrentState = null;

        Runner.Despawn(Object);
    }

    #region Animation
    /// <summary>
    /// 애니메이션 초기화
    /// </summary>
    public void ResetAllAnimation()
    {
        Animator.ResetTrigger(GhostAnimParams.GhostAttack);
        Animator.ResetTrigger(GhostAnimParams.GhostCrawlAttack);
        Animator.ResetTrigger(GhostAnimParams.GhostHit);
        Animator.ResetTrigger(GhostAnimParams.GhostDie);
        Animator.SetBool(GhostAnimParams.GhostChase, false);
        Animator.SetBool(GhostAnimParams.GhostIdle, false);
        Animator.SetBool(GhostAnimParams.GhostWalk, false);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayExorcismAnim()
    {
        Animator.SetTrigger(GhostAnimParams.GhostExorcism);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayDeadAnim()
    {
        Animator.SetTrigger(GhostAnimParams.GhostDie);
    }
    #endregion

    /*
        private void OnDrawGizmos()
        {
            // 플레이어 감지 범위
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, PatrolDetectionDistance);

            // Agent 목적지 표시
            if (Agent != null && Agent.hasPath)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(Agent.destination, 0.5f);
                Gizmos.DrawLine(transform.position, Agent.destination);
            }
        }
        */
}
