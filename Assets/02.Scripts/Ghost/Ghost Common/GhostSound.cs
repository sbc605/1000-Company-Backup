using System.Collections;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아
public enum EGhostSound
{ ChaseLoop, ExorcismLoop, DeathOneShot, AttackOneShot }

public class GhostSound : NetworkBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource loopSource;    // 루프/배경
    [SerializeField] private AudioSource oneShotSource; // 원샷(겹쳐재생)

    [Header("Clips")]
    [SerializeField] private AudioClip chaseLoop;
    [SerializeField] private AudioClip exorcismLoop;
    [SerializeField] private AudioClip deathOneShot;
    [SerializeField] private AudioClip attackOneShot;

    private Coroutine _fadeRoutine;

    // ============ 서버 → 전체 클라 RPC API ============
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayLoop(EGhostSound sfx, float fadeInSec = 0f)
    {
        var clip = ResolveClip(sfx);
        if (!clip || !loopSource) return;

        // 현재 루프와 같으면 중복 재생 방지
        if (loopSource.isPlaying && loopSource.clip == clip) return;

        // 페이드 인, 크로스 페이드 간단 처리
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        loopSource.loop = true;
        
        if (fadeInSec > 0f)
        {
            loopSource.clip = clip;
            loopSource.volume = 0f;
            loopSource.Play();
            _fadeRoutine = StartCoroutine(FadeIn(loopSource, fadeInSec));
        }
        else
        {
            loopSource.clip = clip;
            loopSource.volume = 1f;
            if (!loopSource.isPlaying) loopSource.Play();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_StopLoop(float fadeOutSec = 0f)
    {
        if (!loopSource || !loopSource.isPlaying) return;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        if (fadeOutSec > 0f) _fadeRoutine = StartCoroutine(FadeOut(loopSource, fadeOutSec));
        else
        {
            loopSource.Stop();
            loopSource.volume = 1f;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayOneShot(EGhostSound cue, float volume = 1f)
    {
        var clip = ResolveClip(cue);
        if (!clip || !oneShotSource) return;
        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioClip ResolveClip(EGhostSound sfx)
    {
        return sfx switch
        {
            EGhostSound.ChaseLoop => chaseLoop,
            EGhostSound.ExorcismLoop => exorcismLoop,
            EGhostSound.DeathOneShot => deathOneShot,
            EGhostSound.AttackOneShot => attackOneShot,
            _ => null
        };
    }

    private IEnumerator FadeOut(AudioSource src, float sec)
    {
        float t = 0f, start = src.volume;
        while (t < sec)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(start, 0f, t / sec);
            yield return null;
        }
        src.Stop();
        src.volume = start;
        _fadeRoutine = null;
    }

    private IEnumerator FadeIn(AudioSource src, float sec, float target = 1f)
    {
        float t = 0f;
        src.volume = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(0f, target, t / sec);
            yield return null;
        }
        src.volume = target;
        _fadeRoutine = null;
    }
}
