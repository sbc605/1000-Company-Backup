using System.Collections;
using System.Threading;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

// 코드 담당자: 김수아
public class GhostStateChase : GhostBaseMoveState
{
    private float _loseTimer; // 플레이어를 놓친 시간
    private const float LostDelay = 1f; // 놓친 후 유예 시간
    private bool _canDisappear; // 즉시 사라지지 않도록 보호
    private TickTimer _spawnGraceTime; // 소환 직후 사라짐 방지 시간

    // 목적지 업데이트 주기 & NavMesh 샘플 반경
    private const float UpdateInterval = 0.5f;
    private float _updateTimer;

    // 제자리 감지
    private Vector3 _lastPosition;
    private float _stuckTimer = 0f;
    private const float StuckThreshold = 2f; // 이 시간 이상 같은 위치면 relocate
    private const float PositionThreshold = 0.08f; // 이 거리 이내면 같은 위치로 판단

    private Vector3 _lastDest;

    public override GhostController.EGhostState State => GhostController.EGhostState.Chase;

    public GhostStateChase(GhostController ghost) : base(ghost) { }

    public override void EnterState()
    {
        if (ghost.Agent != null)
        {
            ghost.Agent.isStopped = false;
            ghost.Agent.speed = ghost.GhostMoveSpeed * 2f;
        }

        _loseTimer = 0f;
        _stuckTimer = 0f;
        _updateTimer = 0f;
        _lastPosition = ghost.transform.position;

        _spawnGraceTime = TickTimer.CreateFromSeconds(ghost.Runner, 2f);
        _canDisappear = false;

        if (ghost.Animator != null)
        {
            ghost.Animator.SetBool(GhostAnimParams.GhostIdle, false);
            bool isCrawling = ghost.Animator.GetBool(GhostAnimParams.GhostIsCrawling);
            ghost.Animator.SetBool(GhostAnimParams.GhostChase, true);
            ghost.FX?.ChangePos(isCrawling);
        }
        ghost.Sound?.Rpc_PlayLoop(EGhostSound.ChaseLoop, 0.3f);
    }


    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        // 소환 후 일정 시간 동안 사라짐 방지
        if (_spawnGraceTime.Expired(ghost.Runner)) _canDisappear = true;

        // 최적화용 (0.5초마다 계산)
        _updateTimer += ghost.Runner.DeltaTime;
        if (_updateTimer < UpdateInterval) return;
        _updateTimer = 0f;

        if (ghost.TargetPlayer == null)
        {
            if (ghost.FindPlayerRegisteredPlayer(out var found))
            {
                ghost.TargetPlayer = found;
                _loseTimer = 0f;
            }
        }

        // 타겟이 여전히 없으면 유예시간 후 디스폰
        if (ghost.TargetPlayer == null)
        {
            _loseTimer += UpdateInterval;

            if (_loseTimer > LostDelay && _canDisappear)
            {
                ghost.ChangeState(GhostController.EGhostState.Patrol);
            }
            return; // 여기서 즉시 리턴하여 아래 null 접근 방지
        }
        
        // TargetPlayer가 파괴된 경우 대비(Transform는 남아있을 수 있으나 GameObject가 없을 수 있음)
        if (ghost.TargetPlayer == null || ghost.TargetPlayer.gameObject == null)
        {
            ghost.TargetPlayer = null;
            return;
        }

        // 최대 추격 거리 체크
        float dist = Vector3.Distance(ghost.transform.position, ghost.TargetPlayer.position);

        if (dist > ghost.PatrolDetectionDistance)
        {
            ghost.ChangeState(GhostController.EGhostState.Patrol);

            return;
        }

        // 시야 체크
        if (!ghost.HasLineOfSight(ghost.TargetPlayer))
        {
            ghost.TargetPlayer = null;
            _loseTimer += UpdateInterval;
            if (_loseTimer > LostDelay && _canDisappear)
                ghost.ChangeState(GhostController.EGhostState.Patrol);
            return;
        }

        // 공격 진입
        float attackRange = 1.5f;
        if (ghost.Agent != null && ghost.Agent.hasPath && dist <= attackRange && ghost.Agent.remainingDistance <= attackRange)
        {
            ghost.ChangeState(GhostController.EGhostState.Attack);
            return;
        }

        // 타겟이 NavMesh 밖이면 Patrol로 전환
        if (!NavMesh.SamplePosition(ghost.TargetPlayer.position, out var playerNavHit, 0.2f, NavMesh.AllAreas))
        {
            ghost.TargetPlayer = null; 
            ghost.ChangeState(GhostController.EGhostState.Patrol);
            return;
        }

        // 제자리 감지
        float moved = Vector3.Distance(ghost.transform.position, _lastPosition);
        if (moved < PositionThreshold)
        {
            _stuckTimer += UpdateInterval;
            if (_stuckTimer >= StuckThreshold)
            {
                _stuckTimer = 0f;
                ghost.TargetPlayer = null;
                ghost.Disappear();
                return;
            }
        }
        else
        {
            _stuckTimer = 0f;
            _lastPosition = ghost.transform.position;
        }

        // 목적지 갱신 (0.5m 이상 차이날 때만)
        if (ghost.Agent != null && ghost.Agent.enabled && ghost.Agent.isOnNavMesh)
        {
            Vector3 newDest = playerNavHit.position;
            if (Vector3.Distance(newDest, _lastDest) > 0.5f)
            {
                ghost.Agent.SetDestination(newDest);
                _lastDest = newDest;
            }
        }

        _loseTimer = 0f;
    }

    public override void ExitState()
    {
        if (ghost.Animator != null)
            ghost.Animator.SetBool(GhostAnimParams.GhostChase, false);
        ghost.Sound?.Rpc_StopLoop(1f);
    }
}

