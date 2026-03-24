using Fusion;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Fade : NetworkBehaviour
{
    //코드 담당자 : 최은주
    // 예전에 수업에서 쓰던 Fade 코드 훔쳐왔습니다.

    Image fadeImage;

    private Action localFadeCallback;

    public static Action<float, Color, bool, Action> onFadeAction;
    //네트워크에서 액션 delegate 불가능 

    public override void Spawned()
    {
        base.Spawned();
        Debug.Log("Fade NetworkBehaviour Inti");
    }

    private void Awake()
    {
        fadeImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        onFadeAction += OnFade;
    }

    private void OnDisable()
    {
        onFadeAction -= OnFade;
    }

    //네트워크용
    public void StartFadeForAll(float duration, Color color, bool isFadeIn, string sceneName)
    {
        if (Object.HasStateAuthority)
        {
            RPC_OnFade(duration, color, isFadeIn, sceneName);
            Debug.Log("호스트가 StartFadeForAll");
        }
        else
        {
            Debug.Log("호스트가 아닙니다");
        }

        //    RPC_OnFade(duration, color, isFadeIn, sceneName);
        //Debug.Log("StartFadeForALL 호출");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnFade(float t, Color c, bool isFade, string sceneName) //마지막 액션은 필요하면 넣고 아니면 넣지말아라 라는 디폴트값
    {
        StartCoroutine(SceneChangeFade(t, c, isFade, sceneName));
        Debug.Log("RPC_OnFade 진입");
    }



    IEnumerator SceneChangeFade(float fadeTime, Color color, bool isFade, string sceneName)
    {
        fadeImage.raycastTarget = true;
        float timer = 0f;
        float percent = 0f;
        while (percent < 1f)
        {
            timer += Time.deltaTime;
            percent = timer / fadeTime;

            float value = isFade ? percent : 1 - percent;

            fadeImage.color = new Color(color.r, color.g, color.b, value);

            yield return null;
        }
        fadeImage.raycastTarget = false;

        if (Object.HasInputAuthority)
            localFadeCallback?.Invoke();

        yield return new WaitForSeconds(1.5f);
        BFSceneManager.Instance.OnLoadScene(sceneName);
        Debug.Log("SceneChangeFade 함수 진입");
        Debug.Log($"씬 이름: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    IEnumerator FadeRoutine(float fadeTime, Color color, bool isFade, Action fadeEvent)
    {
        fadeImage.raycastTarget = true;
        float timer = 0f;
        float percent = 0f;
        while (percent < 1f)
        {
            timer += Time.deltaTime;
            percent = timer / fadeTime;

            float value = isFade ? percent : 1 - percent;

            fadeImage.color = new Color(color.r, color.g, color.b, value);

            yield return null;
        }
        fadeEvent?.Invoke();
        fadeImage.raycastTarget = false;

    }

    void OnFade(float t, Color c, bool isFade, Action fadeEvent = null)
    {
        StartCoroutine(FadeRoutine(t, c, isFade, fadeEvent));
    }

    // 방 생성 누를 때 눈속임용
    public static void PlayFakeSceneFade(float duration)
    {
        onFadeAction?.Invoke(duration, Color.black, true, () =>
        {
            onFadeAction?.Invoke(duration, Color.black, false, null);
        });
    }
}
