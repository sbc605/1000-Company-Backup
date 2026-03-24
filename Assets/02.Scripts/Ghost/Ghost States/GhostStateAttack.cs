using System.Collections;
using System.Threading;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
public class GhostStateAttack : GhostBaseState
{
    private PlayerCondition playerCondition;
    private bool hasAttacked;
    private TickTimer exitTimer;

    private bool _animLength;

    public GhostStateAttack(GhostController ghostController) : base(ghostController) { }

    public override GhostController.EGhostState State => GhostController.EGhostState.Attack;

    public override void EnterState()
    {
        if (ghost.TargetPlayer != null)
            playerCondition = ghost.TargetPlayer.GetComponent<PlayerCondition>();

        ghost.Agent.isStopped = true;

        int variant = Random.Range(0, 4);
        if (ghost.Object.HasStateAuthority)
            ghost.CurrentAttackVariant = variant;

        variant = ghost.CurrentAttackVariant;
        ghost.Animator.SetInteger(GhostAnimParams.AttackIndex, variant);

        if (ghost.IsCeilingSpawn)
        {
            ghost.Animator.SetTrigger(GhostAnimParams.GhostCrawlAttack);
            ghost.FX?.ChangePos(true);
        }
        else
        {
            ghost.Animator.SetTrigger(GhostAnimParams.GhostAttack);
            ghost.FX?.ChangePos(false);
        }

        _animLength = true;
        hasAttacked = false;

        ghost.Sound?.Rpc_PlayOneShot(EGhostSound.AttackOneShot, 0.3f);
    }

    public override void ExecuteState()
    {
        if (_animLength)
        {
            float attackAnimLength = ghost.GetCurrentStateLength();
            exitTimer = TickTimer.CreateFromSeconds(ghost.Runner, attackAnimLength);
            _animLength = false;
        }

        if (!ghost.Object.HasStateAuthority) return;

        if (!hasAttacked && playerCondition != null)
        {
            playerCondition.Rpc_DecreaseSanity(85f); // 공격할때 플레이어 정신력 5% 차감

            // Hunting 상태에서 공격했다면 플레이어를 즉사시키고 Patrol로 복귀
            // if (GhostSpawner.Instance.ExorcismState == GhostSpawner.EExorcismState.Failed)
            //     playerCondition.Rpc_TakeDamage(2);
            // else
            //     playerCondition.Rpc_TakeDamage(1);

            playerCondition.Rpc_TakeDamage(1);
            hasAttacked = true;
        }

        if (exitTimer.Expired(ghost.Runner))
        {
            if (GhostSpawner.Instance.ExorcismState == GhostSpawner.EExorcismState.Failed)
                ghost.ChangeState(GhostController.EGhostState.Patrol);
            else
                ghost.Disappear();
        }
    }

    public override void ExitState()
    {
        hasAttacked = false;
    }
}

