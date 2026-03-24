using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// 코드 담당자: 김수아

public class GhostStatePatrol : GhostBaseMoveState
{
    private float _patrolTimer;
    private float _patrolDuration;

    public override GhostController.EGhostState State => GhostController.EGhostState.Patrol;

    public GhostStatePatrol(GhostController ghost) : base(ghost) { }

    public override void EnterState()
    {
        if (ghost.Agent != null)
        {
            if (!ghost.Agent.enabled) ghost.Agent.enabled = true;
            ghost.Agent.isStopped = false;
            ghost.Agent.speed = ghost.GhostMoveSpeed;
        }

        _patrolDuration = Random.Range(5f, 10f);
        _patrolTimer = 0f;

        bool isCrawling = (GhostSpawner.Instance.ExorcismState == GhostSpawner.EExorcismState.Failed) ? false : ghost.IsCeilingSpawn;
        ghost.Animator.SetBool(GhostAnimParams.GhostWalk, true);
        ghost.Animator.SetBool(GhostAnimParams.GhostIsCrawling, isCrawling); // 천장 스폰이면 true, 아니면 false

        ghost.FX?.ChangePos(isCrawling);
        SetRandomDestination();
    }

    public override void ExecuteState()
    {
        if (!ghost.Object.HasStateAuthority) return;

        if (ghost.Agent == null || !ghost.Agent.enabled || !ghost.Agent.isOnNavMesh)
        {
            ghost.Disappear();
            return;
        }

         // 플레이어가 보이면 즉시 추격
        if (ghost.FindPlayerRegisteredPlayer(out Transform player))
        {
            ghost.TargetPlayer = player;
            ghost.ChangeState(GhostController.EGhostState.Chase);
            return;
        }

        // 순찰 타임아웃 → 사라짐
        _patrolTimer += ghost.Runner.DeltaTime;
        if (_patrolTimer > _patrolDuration)
        {
            _patrolTimer = 0f;
            ghost.Disappear();
            return;
        }

        // 목적지에 도착하면 새로운 목적지 설정
        if (!ghost.Agent.pathPending && ghost.Agent.remainingDistance <= ghost.Agent.stoppingDistance)
        {
            // 실제 이동이 거의 없을 때도 갱신
            if (!ghost.Agent.hasPath || ghost.Agent.velocity.sqrMagnitude < 0.01f)
            {
                SetRandomDestination();
            }
        }
    }

    public override void ExitState() { }
}
