using System.Collections;
using UnityEngine;

// 코드 담당자: 김수아

public class MagicCircleMusic : MonoBehaviour
{
    [SerializeField] private AudioSource bgMusic;
    [SerializeField] private float defaultFadeOutSec = 1.2f;

    public void OnProcessingStart()
    {
        if (!bgMusic) return;
        if (!bgMusic.isPlaying)
        {
            bgMusic.volume = 1f;
            bgMusic.Play();
        }
    }

    public void FadeOut(float sec)
    {
        if (!bgMusic) return;
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine(sec <= 0f ? defaultFadeOutSec : sec));
    }

    private IEnumerator FadeOutRoutine(float sec)
    {
        float t = 0f;
        float start = bgMusic.volume;
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / sec);
            bgMusic.volume = start * k;
            yield return null;
        }
        bgMusic.Stop();
        bgMusic.volume = 1f;
    }
}
