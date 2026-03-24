// 코드 담당자 : 최서영
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 천우인 동작 로직 : 씬 내 SpawnTop(BoxCollider Trigger)들 위에서 드롭 프리팹을 오브젝트풀 기반으로 비처럼 떨어뜨리는 로직
// - AbnormalSpawner가 Cheonwooin 프리팹을 스폰하면 이 스크립트가 자동 실행됨
// - ObjectPoolManager를 통해 드롭 오브젝트를 풀링/재사용
// - 활성 상태의 오브젝트는 Cheonwooin 하위에, 반환된 오브젝트는 ObjectPoolManager 하위에 존재함 (재사용 위해서 나눠놨는데 의미가 있을지는 모르겠습니다.)
/// </summary>
public class Cheonwooin : ParanormalPhenomenonBase
{
    // Spawn 영역
    private List<BoxCollider> _topVolumes; // SpawnTop 캐시
    private const string TOP_TAG = "SpawnTop"; // SpawnTop 태그명

    // Object Pool 관련
    private ObjectPoolManager _poolManager; // 전역 풀 매니저 참조
    [SerializeField] private GameObject ObjpoolPrefab; // 풀링될 드롭 프리팹
    private const int _poolSize = 30; // SpawnTop당 풀 사이즈

    // 천우인 속성
    [SerializeField] private float spawnPerSecond = 20f; // 초당 생성 개수
    [SerializeField] private float initialDownVelocity = 6f; // 초기 하강 속도
    [SerializeField] private float lateralJitter = 0.5f; // 좌우 흔들림 정도
    [SerializeField] private float recycleMargin = 2f; // 회수 기준 마진
    [SerializeField] private float maxLifeTime = 10f; // 최대 생존 시간

    // 내부 상태 캐시
    private Vector3 spawnPos;
    private Bounds bounds = default;
    private Bounds _lastBounds;
    private readonly List<ActiveItem> _actives = new(); // 천우인 개별 로직용

    // 천우인 개별 관리용 구조체
    private struct ActiveItem
    {
        public GameObject go;
        public Vector3 spawnPos;
        public float spawnTime;
        public ActiveItem(GameObject go, Vector3 pos, float t)
        {
            this.go = go;
            this.spawnPos = pos;
            this.spawnTime = t;
        }
    }

    void Awake()
    {
        _poolManager = FindFirstObjectByType<ObjectPoolManager>();
    }

    void OnEnable()
    {
        // SpawnTop 영역 찾기
        FindSpawnTop();

        // 풀 등록 (Top 개수 * 30)
        if (_poolManager && ObjpoolPrefab)
            _poolManager.Register(ObjpoolPrefab, Mathf.Max(1, _topVolumes.Count) * _poolSize);

        // 비 스폰 루프 시작
        StartCoroutine(SpawnLoop());
    }

    /// <summary>
    /// 스폰 영역 탐색 (천우인은 실외에 스폰되어야 해서)
    /// </summary>
    void FindSpawnTop()
    {
        if (_topVolumes != null) return;

        _topVolumes = new List<BoxCollider>();
        var tagged = GameObject.FindGameObjectsWithTag(TOP_TAG);

        foreach (var go in tagged)
        {
            var bc = go.GetComponent<BoxCollider>();
            if (bc && bc.enabled)
                _topVolumes.Add(bc);
        }
    }

    /// <summary>
    /// 천우인이 비처럼 떨어지도록 루프
    /// </summary>
    IEnumerator SpawnLoop()
    {
        float interval = (spawnPerSecond > 0f) ? 1f / spawnPerSecond : 0.05f;
        float timer = 0f;

        while (enabled)
        {
            timer += Time.deltaTime;

            // 일정 간격마다 스폰
            while (timer >= interval)
            {
                timer -= interval;

                if (SetPosition(out spawnPos, out bounds))
                {
                    _lastBounds = bounds;
                    SpawnOne(spawnPos);
                }
            }

            // 활성 오브젝트 업데이트/회수 체크
            UpdateActives();

            yield return null;
        }
    }

    /// <summary>
    /// 천우인 스폰 위치 계산
    /// </summary>
    bool SetPosition(out Vector3 pos, out Bounds bounds)
    {
        FindSpawnTop();
        bounds = default;

        if (_topVolumes.Count == 0)
        {
            pos = default;
            Debug.Log("SpawnTop 태그의 BoxCollider가 없습니다.");
            return false;
        }

        // 랜덤 SpawnTop 하나 선택
        var vol = _topVolumes[Random.Range(0, _topVolumes.Count)];
        var b = vol.bounds;
        bounds = b;

        // 박스 상단면에서 랜덤 XZ 위치 선택
        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);
        float y = b.max.y;

        pos = new Vector3(x, y + 0.02f, z);
        return true;
    }

    /// <summary>
    /// 천우인 개별 로직
    /// </summary>
    void UpdateActives()
    {
        for (int i = _actives.Count - 1; i >= 0; i--)
        {
            var a = _actives[i];
            if (a.go == null)
            {
                _actives.RemoveAt(i);
                continue;
            }

            // 하강 + 좌우 흔들림
            var tr = a.go.transform;
            tr.position += Vector3.down * (initialDownVelocity * Time.deltaTime);
            tr.position += new Vector3(
                Random.Range(-lateralJitter, lateralJitter),
                0f,
                Random.Range(-lateralJitter, lateralJitter)
            ) * Time.deltaTime;

            // 수명 및 범위 체크
            bool lifeExpired = (Time.time - a.spawnTime) >= maxLifeTime;
            bool distExpired = (tr.position - a.spawnPos).sqrMagnitude >= (maxLifeTime * initialDownVelocity + 0.01f);
            bool belowVolume = tr.position.y <= (_lastBounds.max.y - _lastBounds.size.y) - recycleMargin;

            if (lifeExpired || distExpired || belowVolume)
                Return(i);
        }
    }

    /// <summary>
    /// 오브젝트풀 로직
    /// 풀에서 꺼내기 & 초기화
    /// </summary>
    GameObject SpawnOne(Vector3 pos)
    {
        if (_poolManager == null) return null;

        var inst = _poolManager.Get(ObjpoolPrefab); // 비활성 상태로 풀에서 꺼냄
        if (inst == null) return null;

        // 부모는 Cheonwooin 루트
        inst.transform.SetParent(transform, true);
        inst.transform.position = pos;

        // 초기 물리값 세팅
        var rb = inst.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.down * initialDownVelocity;
            rb.angularVelocity = Vector3.zero;
        }

        // 활성화 및 추적 리스트 등록
        inst.SetActive(true);
        _actives.Add(new ActiveItem(inst, pos, Time.time));

        return inst;
    }

    /// <summary>
    /// 오브젝트풀 로직
    /// 반환 작업
    /// </summary>
    void Return(int index)
    {
        var a = _actives[index];
        if (a.go == null)
        {
            _actives.RemoveAt(index);
            return;
        }

        // 부모를 ObjectPoolManager로 되돌리고 비활성화
        a.go.transform.SetParent(_poolManager.transform, true);
        _poolManager.Return(a.go);

        // 리스트에서 제거
        _actives.RemoveAt(index);
    }

    // 부적 사용 > 제거용
    protected override void Disappear()
    {
        // 스폰 루프 정리
        StopAllCoroutines();

        // 활성 드롭 전부 풀에 반납
        for (int i = _actives.Count - 1; i >= 0; i--)
            Return(i);
        _actives.Clear();

        base.Disappear();
    }
}
