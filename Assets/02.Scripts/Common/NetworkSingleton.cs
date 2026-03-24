using Fusion;
using UnityEngine;

public class NetworkSingleton : NetworkBehaviour
{
//    public static T Instance { get; private set; }

//    // 예: 네트워크 상에서 동기화되는 bool
//    [Networked]
//    public NetworkBool isReqScene { get; private set; }

//    protected virtual void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Debug.LogWarning($"이미 존재하는 {typeof(T).Name} 싱글턴이 있습니다. Destroying duplicate.");
//            Destroy(gameObject);
//            return;
//        }

//        Instance = (T)this;
//        DontDestroyOnLoad(gameObject);
//    }

//    public override void Spawned()
//    {
//        base.Spawned();
//        // 여기서 네트워크 초기화 작업 가능
//        Debug.Log($"[{(Runner != null && Runner.IsServer ? "서버" : "클라")}] {typeof(T).Name} Spawned, isReqScene: {isReqScene}");
//    }

//    /// <summary>
//    /// 서버에서 isReqScene 변경 시 클라이언트까지 자동 동기화
//    /// </summary>
//    public void SetReqScene(bool value)
//    {
//        if (Runner != null && Runner.IsServer)
//        {
//            isReqScene = value;
//            Debug.Log($"[서버] isReqScene 설정: {value}");
//        }
//    }
//}
}
