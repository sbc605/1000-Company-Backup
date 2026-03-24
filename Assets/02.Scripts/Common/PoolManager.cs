using System.Collections.Generic;
using UnityEngine;
using Fusion;

// 작성자 정하윤
public class PoolManager : MonoBehaviour
{
    // 풀에서 사용할 각 프리팹과 초기 생성 개수 정의
    [System.Serializable]
    public class PoolItem
    {
        public GameObject prefab;  // 재사용할 프리팹
        public int size;           // 초기 생성 개수
    }

    // 풀에 등록할 프리팹 리스트
    public List<PoolItem> poolItems; 
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        // 각 프리팹별로 오브젝트 큐 생성
        foreach (var item in poolItems)
        {
            var queue = new Queue<GameObject>();

            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            poolDictionary.Add(item.prefab, queue);
        }
    }

    // 풀에서 비활성화된 오브젝트 하나 가져오기
    public GameObject GetObject(GameObject prefab, NetworkRunner runner, Vector3 position)
    {
        if (!poolDictionary.ContainsKey(prefab))
            return null;

        var queue = poolDictionary[prefab];

        GameObject obj;

        if (queue.Count == 0)
        {
            // 큐가 비었을 경우 새로 생성(자동 확장)
            obj = runner.Spawn(prefab, Vector3.zero, Quaternion.identity).gameObject;
            return obj;
        }
        else
        {
            obj = queue.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.SetActive(true);
        }

        return obj;
    }

    // 사용이 끝난 오브젝트를 다시 풀에 반환
    public void ReturnObject(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        poolDictionary[prefab].Enqueue(obj);
    }
}

