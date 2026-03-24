using UnityEngine;
using Fusion;
using System.Linq;

public class DropZone : NetworkBehaviour
{
    public LayerMask PlayerLayer;
    private const int MAX_COLLIDERS_IN_ZONE = 4;
    private Collider[] _colliderResults = new Collider[MAX_COLLIDERS_IN_ZONE];

    public override void FixedUpdateNetwork()
    {
        if (!Runner.IsServer) return;


        int count = Runner.GetPhysicsScene().OverlapBox(
            transform.position,
            transform.localScale / 2,
            _colliderResults,
            transform.rotation,
            PlayerLayer,
            QueryTriggerInteraction.Collide
        );


        for (int i = 0; i < count; i++)
        {
            Collider col = _colliderResults[i];

            if (col.TryGetComponent<PlayerController>(out var player))
            {
                if (player.TryGetComponent<PlayerCondition>(out var condition) && 
                    condition.IsDead && !player.IsBeingCarried)
                {
                    Debug.Log($"플레이어 {player.Object.Id} 드랍 존에 도착");

                }
            }
        }
        

    }
}