using Fusion;
using UnityEngine;

// 작성자 정하윤

/// <summary>
/// 상점에서 구매한 제령 아이템을 스폰시키는 스포너
/// </summary>
public class ShopItemSpawner : NetworkBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spawnSoundClip;

    [SerializeField] private Transform spawnPoint;
    [SerializeField] private ParticleSystem spawnParticle;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void Rpc_RequestSpawnItem(NetworkPrefabRef itemPrefabRef, PlayerRef requestingPlayer)
    {
        Rpc_PlaySpawnEffects();

        Runner.Spawn
        (
            itemPrefabRef,
            spawnPoint.position,
            spawnPoint.rotation,
            requestingPlayer
        );
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlaySpawnEffects()
    {
        if (spawnParticle != null)
            spawnParticle.Play();

        if (audioSource != null && spawnSoundClip != null)
            audioSource.PlayOneShot(spawnSoundClip);
    }
}
