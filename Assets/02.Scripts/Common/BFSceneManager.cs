using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;

public class BFSceneManager : SingleTon<BFSceneManager>
{
    //코드 담당자 : 최은주 
    //씬 넘겨주는 매니저   
    [SerializeField] BasicSpawner bs;
    [SerializeField] Fade fade;
    private NetworkRunner runner;

    public SpawnManager sp;

    public bool isReq;

    public void AssignRunner(NetworkRunner assignedRunner)
    {
        runner = assignedRunner;
        Debug.Log("Runner 연결 받음");
    }    

    public void WaitFade(string sceneName)
    {
        fade.StartFadeForAll(2f, Color.black, true, sceneName);
        Debug.Log("WaitFade 함수 진입");
    }

    public void OnLoadScene(string sceneName)
    {
        if (fade == null)
        {
            Debug.Log("fade 없음");
            fade = GameObject.FindWithTag("Fade").GetComponent<Fade>();
        }
        var smList = FindObjectsByType<SpawnManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        //씬 전환 전에 이전 SpawnManager 콜백 정리
        foreach (var oldSM in smList)
        {
            if (FindAnyObjectByType<NetworkRunner>() is NetworkRunner runner)
            {
                runner.RemoveCallbacks(oldSM);
                Debug.Log($"[BFSceneManager] 이전 SpawnManager 콜백 제거 ({oldSM.gameObject.scene.name})");
            }
            Destroy(oldSM.gameObject);
        }

        if (runner != null && runner.IsServer)
        {            
            Debug.Log("씬 전환");
            if (sceneName == "Request1")
            {
                isReq = true;                              
            }
            else if(sceneName == "Office")
            {
                isReq = false;              
                Destroy(bs);
            }

            runner.LoadScene(sceneName);                   
        }
    }

    IEnumerator StartSound(string soundName)
    {
        yield return new WaitForSeconds(3f);

        SoundManager.Instance.BgmSoundPlay(soundName);
    }
}
