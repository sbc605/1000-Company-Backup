using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.AI;

// 작성자 : 정하윤
public class DarkAura : ParanormalPhenomenonBase
{
    [Header("움직임 설정")]
    [SerializeField] private Transform visuals;
    [SerializeField] private float floatSpeed = 3f;     // 속도
    [SerializeField] private float floatAmount = 0.25f; // 진폭

    [Header("스폰 주기(초 단위)")]
    public Vector2 spawnIntervalRange = new Vector2(10f, 30f);

    [Header("생성 위치 반경")]
    public float spawnRadius = 3f;

    [Header("랜덤 크기 범위")]
    public Vector2 scaleRange = new Vector2(0.5f, 1.5f);

    // DarkSphere가 Obstacle 레이어의 오브젝트에게 끼지 않도록
    [Header("스폰 방지 레이어")]
    [SerializeField] private LayerMask obstacleLayer;

    // 흑기 생존 여부
    private bool isAlive = true;

    // 오브젝트 풀 관련 -------------
    private ObjectPoolManager pool;
    public int poolSize = 30;
    List<GameObject> spawnedSpheres = new List<GameObject>();
    //------------------------------

    // 네트워크
    [SerializeField] private NetworkObject darkSpherePrefab; // 수정 : 최서영 (타입 gameObj -> netObj)

    protected override void WakeUp()
    {
        pool = FindFirstObjectByType<ObjectPoolManager>();
    }

    void OnEnable()
    {
        // 수정 : 최서영
        StartCoroutine(RegisterNextFrame());
    }

    public override void Render()
    {
        // 이 오브젝트의 고유 ID(uint)를 float 오프셋으로 사용하여
        // 모든 클라이언트에서 동일하므로, 오프셋 값도 동일하게 계산됨
        float deterministicOffset = (float)Object.Id.Raw;

        // 동일하게 둥실거리지 않도록 고유 ID에서 가져온 오프셋을 더해줌
        float floatOffset = Mathf.Sin((Runner.SimulationTime * floatSpeed) + deterministicOffset) * floatAmount;

        // 루트의 로컬 위치로 적용
        visuals.localPosition = new Vector3(0, floatOffset, 0);
    }

    IEnumerator SpawnRoutine()
    {
        while (isAlive)
        {
            if (Object.HasStateAuthority)
            {
                SpawnSphere();
            }

            float waitTime = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(waitTime);
        }
    }

    // 구체 생성 메서드
    void SpawnSphere()
    {
        if (target == null)
        {
            // target이 null이면 플레이어를 찾으려고 시도
            var playerObj = GameObject.FindGameObjectWithTag("Player"); // 수정 : 최서영 (타입 GameObj -> var)

            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            else
            {
                // 아직 플레이어가 스폰되지 않았다면, 구체 생성을 중단하고 함수를 종료
                return;
            }
        }

        // 최대 스폰 개수를 넘으면 더 이상 생성하지 않기
        if (spawnedSpheres.Count >= poolSize) 
            return;

        if (darkSpherePrefab == null || !Runner) // 수정 : 최서영 (Register 안해도 돼서 삭제)
            return;

        // DarkSphere 초기화
        // 흑기 주변 랜덤 위치에 랜덤 크기로 지정(Y축은 0으로 하고 X, Z만 랜덤하게 설정)
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        Vector3 potentialSpawnPos = transform.position + randomOffset;

        NavMeshHit navHit;
        // NavMesh 위인지 검사(탐색 반경 5f로 넉넉하게)
        if (!NavMesh.SamplePosition(potentialSpawnPos, out navHit, 5f, NavMesh.AllAreas))
        {
            // 이 위치 근처에 NavMesh가 없음
            Debug.Log($"DarkAura: NavMesh를 찾지 못해 스폰 취소 ({potentialSpawnPos})");
            return;
        }

        Vector3 validSpawnPos = navHit.position;

        // 해당 위치가 Obstacle 내부에 있는지 검사(Y축 1m 위에서 0.5f 반경으로 체크, DarkSphere의 콜라이더 크기에 맞춰 조절)
        float checkRadius = 0.5f;
        Collider[] hits = Physics.OverlapSphere(validSpawnPos + Vector3.up * 1f, checkRadius, obstacleLayer);

        if (hits.Length > 0)
        {
            // Obstacle 레이어에 해당하는 물체가 감지됨
            Debug.Log($"DarkAura : Obstacle에 막혀 스폰 취소 ({validSpawnPos})");
            return;
        }

        var spawnPos = validSpawnPos;

        float randomScale = Random.Range(scaleRange.x, scaleRange.y);
        bool canMove = Random.Range(0, 2) == 0;

        // 수정 : 최서영 (구체 멀티 연동) - 호스트에서 실행되면 모든 클라이언트에 동기화됨
        NetworkObject netObj = Runner.Spawn
        (
            darkSpherePrefab,
            spawnPos,
            Quaternion.identity,
            inputAuthority: null,
            (runner, obj) =>
            {
                // 풀에서 나온 직후/인스턴스 생성 직후 초기화(스폰한 권위에서만 실행)
                obj.transform.localScale = Vector3.one * randomScale;

                var sphere = obj.GetComponent<DarkSphere>();

                if (sphere)
                {
                    sphere.Initialize(target, pool, spawnPos, canMove);
                }
        });

        if (netObj) spawnedSpheres.Add(netObj.gameObject);
    }

    // 흑기 퇴치 처리
    protected override void Disappear()
    {
        isAlive = false;
        StopAllCoroutines();

        foreach (var sphere in spawnedSpheres)
        {
            if (sphere != null && sphere.activeSelf)
            {
                var floating = sphere.GetComponent<NetworkObject>(); // 수정 : 최서영
                if (floating && Runner) Runner.Despawn(floating); // 수정 : 최서영 (Network Obj라 runner 함수 사용 필요)
            }
        }

        spawnedSpheres.Clear();

        base.Disappear();
    }

    // 추가 : 최서영
    // 구체 스폰 + Null 값 참조 방지 대기
    IEnumerator RegisterNextFrame()
    {
        yield return null;

        // null-세이프 로그 + 조기 리턴
        if (!darkSpherePrefab)
        {
            Debug.Log("NetworkObject 프리팹 할당 필요");
            yield break;
        }

        // 풀 참조 확보
        if (!pool) pool = FindFirstObjectByType<ObjectPoolManager>();

        if (pool) pool.Register(darkSpherePrefab.gameObject, poolSize);

        // 스폰 시작(호스트만)
        yield return null;
        if (Object && Object.HasStateAuthority)
            StartCoroutine(SpawnRoutine());
    }
}
