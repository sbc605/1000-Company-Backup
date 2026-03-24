using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.ParticleSystem;


// 작성자 : 정하윤
public class DarkSphere : NetworkedTriggerEventSupporter
{
    [Header("Sound")]
    [SerializeField] private AudioClip insideLoopSound;
    [SerializeField] private AudioSource audioSource;

    [Header("움직임 설정")]
    public float moveSpeed = 1f;
    public float attractSpeed = 0.5f;
    public float originAttractSpeed = 0.5f;
    public float chaseStartDistance = 2f;       // 추격 시작 거리(플레이어와 가까워지면)
    public float chaseStopDistance = 3f;        // 추격 중단 거리(플레이어와 멀어지면)
    public float chaseSmoothing = 5f;           // 부드럽게 추격

    [Header("배회 (Wandering)")]
    [SerializeField] private float randomMoveRadius = 5f;       // 배회 반경
    [SerializeField] private float wanderSpeedFraction = 0.5f;  // 배회 시 속도

    private Vector3 basePos;
    private Vector3 targetWanderPos;            // 배회 목표 지점
    private Vector3 startPos;
    private Vector3 target;
    private Transform player;                   // 현재 추적 중인 플레이어ㅈ
    private ObjectPoolManager pool;
    private PlayerController playerController;
    private PlayerCondition playerCondition;
    [SerializeField] private LayerMask playerLayer;

    public float detectRange = 2f;
    private float floatTime;

    private bool canMove;
    private bool canTrace;
    private bool iscollided = false;

    private NavMeshAgent agent;

    [Networked] private NetworkBool IsChasing { get; set; }
    private Vector3 lastPosition;                       // 플레이어 잃었을 때 멈출 위치

    [SerializeField] private Transform visuals;
    [SerializeField] private float floatSpeed = 3f;     // 속도
    [SerializeField] private float floatAmount = 0.25f; // 진폭

    // 비주얼의 초기 로컬 위치를 저장할 변수
    private Vector3 initialVisualPosition;

    // 나중에 플레이어와의 거리가 멀면 콜라이더랑 랜더러 끄는 것도 고려해보기

    //public override void Spawned()
    //{
    //    startPos = transform.position;
    //}

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        // 프리팹 에디터에서 설정한 Y가 뜬 위치를 저장
        if (visuals != null)
            initialVisualPosition = visuals.localPosition;
    }

    // 초기화
    public void Initialize(Transform player, ObjectPoolManager pool, Vector3 position, bool canMove)
    {
        this.player = player;
        this.pool = pool;
        this.canMove = canMove;

        // 'position'은 DarkAura가 이미 검증(SamplePosition, Obstacle 체크)을 마친
        // 100% 안전한 NavMesh 위의 좌표입니다.
        startPos = position;

        Debug.Log($"이번에 활성화된 구체의 움직임 가능 여부 : {canMove}");

        if (agent != null)
        {
            agent.enabled = true;
            agent.baseOffset = 0f;

            agent.Warp(startPos);

            basePos = transform.position;
            lastPosition = basePos;
            gameObject.SetActive(true);
            SetTargetPosition();
        }
        else
        {
            Debug.Log("[DarkSphere] NavMeshAgent 컴포넌트가 없음");
            ReturnToPool();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        if (!canMove)
        {
            // 움직일 수 없으면 에이전트 정지
            agent.isStopped = true; 
            return;
        }

        agent.isStopped = false;

        FindPlayer();

        if (player != null)
        {
            IsChasing = true;
            ChasePlayer();
        }
        else
        {
            IsChasing = false;
            Wander();
        }
    }
    public override void Render()
    {
        // 이 오브젝트의 고유 ID(uint)를 float 오프셋으로 사용하여
        // 모든 클라이언트에서 동일하므로, 오프셋 값도 동일하게 계산됨
        float deterministicOffset = (float)Object.Id.Raw;

        // 동일하게 둥실거리지 않도록 고유 ID에서 가져온 오프셋을 더해줌
        float floatOffset = Mathf.Sin((Runner.SimulationTime * floatSpeed) + deterministicOffset) * floatAmount;

        // X와 Z는 초기 값을 그대로 사용하고, Y에만 둥실거림을 더함
        visuals.localPosition = new Vector3
        (
            initialVisualPosition.x,
            initialVisualPosition.y + floatOffset,
            initialVisualPosition.z
        );
    }
    void FindPlayer()
    {
        // TODO : 추후 OverlapSphereNonAlloc로 수정하기
        Collider[] hits = Physics.OverlapSphere(transform.position, detectRange, playerLayer);

        // 매 틱마다 초기화
        player = null; 

        if (hits.Length > 0)
        {
            player = hits[0].transform;
        }
    }

    void ChasePlayer()
    {
        if (player == null || agent == null || !agent.isOnNavMesh) 
            return;

        // 추격 속도 설정
        agent.speed = moveSpeed;

        // 목표 지점 설정
        agent.SetDestination(player.position); 
    }
    void SetTargetPosition()
    {
        // startPos를 중심으로 randomMoveRadius 반경 내 임의의 지점 선택
        Vector3 randomDirection = Random.insideUnitSphere * randomMoveRadius;
        randomDirection += startPos;

        NavMeshHit navHit;
        // randomDirection에서 가장 가까운 NavMesh 위의 유효한 지점을 찾음
        if (NavMesh.SamplePosition(randomDirection, out navHit, randomMoveRadius, NavMesh.AllAreas))
            targetWanderPos = navHit.position;
        else
            // 유효한 지점을 못 찾으면 그냥 startPos 사용
            targetWanderPos = startPos;
    }

    /// <summary>
    /// 타겟을 향해 배회
    /// </summary>
    void Wander()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.speed = moveSpeed * wanderSpeedFraction; // 배회 속도 설정

        // 목표 지점에 도달했거나 경로가 없으면 새 목표 지점 설정
        if (!agent.hasPath || agent.remainingDistance < 0.5f)
        {
            SetTargetPosition();
            agent.SetDestination(targetWanderPos); // 새 목표 지점으로 이동
        }
    }

    // 플레이어가 들어오면 대미지 효과 시작
    protected override void OnTargetEnter(Collider player)
    {
        if (!iscollided)
        {
            var netObj = player.GetComponent<NetworkObject>();

            if (netObj != null && netObj.HasInputAuthority) 
            {
                iscollided = true;

                playerController = player.gameObject.GetComponent<PlayerController>();
                playerCondition = player.gameObject.GetComponent<PlayerCondition>();

                if (playerController && playerCondition)
                {
                    ApplyDebuff(true);

                    originAttractSpeed = attractSpeed;
                    attractSpeed = attractSpeed * 0.5f;
                    canTrace = canMove ? false : canTrace;
                }

                if (audioSource != null && insideLoopSound != null)
                {
                    audioSource.clip = insideLoopSound;
                    audioSource.Play();
                }
            }
        }
    }

    // 플레이어가 나가면 대미지 효과 중지
    protected override void OnTargetExit(Collider player)
    {
        var netObj = player.GetComponent<NetworkObject>();

        if (iscollided && netObj != null && netObj.HasInputAuthority) 
        {
            iscollided = false;

            attractSpeed = originAttractSpeed;
            ApplyDebuff(false);
            canTrace = canMove ? true : canTrace;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }

    void ApplyDebuff(bool apply)
    {
        if (!playerController || !playerCondition) 
            return;

        if (apply)
        {
            playerController.Rpc_SetSlowDebuff(true, 0.25f);
            playerCondition.StartFade(1f);
        }
        // 디버프 해제
        else
        {
            playerController.Rpc_SetSlowDebuff(false);
            playerCondition.StartFade(0f);
        }
    }

    public void ReturnToPool()
    {
        if (agent != null)
            agent.enabled = false;

        if (pool != null)
            pool.Return(gameObject);
        else
            gameObject.SetActive(false);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (iscollided)
        {
            // 디버프 해제 로직을 수동으로 호출
            ApplyDebuff(false);

            // 상태 변수들을 깨끗하게 초기화
            iscollided = false;
            playerController = null;
            playerCondition = null;
            player = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (agent != null)
            agent.enabled = false;

        // 항상 부모의 Despawned를 마지막에 호출해주는 것이 안전
        base.Despawned(runner, hasState);
    }
}