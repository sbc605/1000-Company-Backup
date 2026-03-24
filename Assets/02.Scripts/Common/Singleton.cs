using UnityEngine;

public class SingleTon<T> : MonoBehaviour where T : Component
{
    private static T instance; //내부변수
    public static T Instance //프로퍼티
    {
        get
        {
            if (instance == null)
            {
                var t = FindFirstObjectByType<T>(); //이 스크립트 가진 객체 없나 찾아보기

                if (t != null) //찾음 
                {
                    instance = t;
                }
                else //못찾음
                {
                    var newObj = new GameObject(typeof(T).ToString()); // 오브젝트 생성
                    newObj.AddComponent<T>(); //스크립트 추가

                    instance = newObj.GetComponent<T>();
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null) 
        {
            instance = this as T; 
            DontDestroyOnLoad(gameObject);
        }
        else 
            Destroy(gameObject);
    }
}
