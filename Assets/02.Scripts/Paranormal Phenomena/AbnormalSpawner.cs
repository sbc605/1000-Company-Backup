// 코드 담당자 : 최서영
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Fusion;

public enum EAbnormal { Heukgi, Cheonwooin, Eodukshini, Darkness }

/// <summary>
/// 이상현상 스폰 총괄 매니저
/// - 호출 흐름: RequestDetailInfo -> SpawnManager.SpawnAnomalies(count)
/// - 데이터 소스: AbnormalData(SO) 목록
/// - 정책: 종류(EAbnormal) 단위로 중복 스폰 금지 (한 종류는 맵에 최대 1개)
/// - 위치 결정: NavMesh 위 임의 좌표
/// </summary>
public class AbnormalSpawner : NetworkBehaviour
{
    [SerializeField] List<AbnormalData> abnormalDatas; // 이상현상 목록

    // 스폰 위치
    [SerializeField] Transform[] spawnPoints; // 스폰 포인트
    float safeRadius = 1.0f; // 자리 비었는지 검사할 반경
    int navSampleMaxTries = 10; // 위치 찾기 재시도 횟수 (무한 루프 방지용)
    // float navSampleRadius = 25f; // 스폰 반경

    // 벽 스폰 전용
    [SerializeField] Transform[] wallSpawnPoints; // 스폰 포인트
    //[SerializeField] LayerMask wallMask; // 벽 레이어
    //float wallRayDistance = 10f;
    //float surfaceOffset = 0.05f; // 벽/바닥에서 살짝 띄우기(프리팹 반지름 정도..)

    [SerializeField] LayerMask occupyMask; // Player/Ghost/Obstacle/Abnormal 등 점유 판정용 레이어

    // 내부 조회용
    Dictionary<EAbnormal, AbnormalData> _dataByType;

    // 현재 맵에 존재하는 이상현상 추적 (중복 방지용)
    readonly Dictionary<EAbnormal, GameObject> _spawned = new();

    // 스폰 포인트 슬롯 점유 관리 (Ground / Wall 공용)
    readonly HashSet<Transform> _occupiedSpawnPoints = new();

    [Networked] NetworkId _heukgiRef { get; set; }
    [Networked] NetworkId _cheonwooinRef { get; set; }
    [Networked] NetworkId _eodukshiniRef { get; set; }
    [Networked] NetworkId _darknessRef { get; set; }

    void Awake()
    {
        BuildLookup();
    }

    public override void Spawned()
    {
        // 호스트만 스폰
        if (Object.HasStateAuthority)
        {
            StartCoroutine(SpawnDelay(2));
        }
    }

    /// <summary>
    /// EAbnormal 타입 즉시 조회용
    /// </summary>
    void BuildLookup()
    {
        _dataByType = new Dictionary<EAbnormal, AbnormalData>();
        if (abnormalDatas == null)
            return;

        foreach (var so in abnormalDatas)
        {
            if (so == null) continue;
            if (_dataByType.ContainsKey(so.type) == false)
                _dataByType.Add(so.type, so);
        }
    }

    /// <summary>
    /// RequestDetailInfo에서 전달받은 이상현상 개수(count)에 따라 스폰을 시도합니다.
    /// - 중복 금지: 이미 존재하는 종류는 후보에서 제외
    /// - 랜덤 선택: 남은 후보 중 균등 랜덤 1개
    /// - 위치: NavMesh 위 임의 포인트
    /// </summary>
    public void SpawnAnomalies(int count)
    {
        if (!Object || !Object.HasStateAuthority)
            return;

        if (_dataByType == null || _dataByType.Count == 0)
            return; // AbnormalData 목록이 비어 있으면 스폰 불가

        for (int i = 0; i < count; i++)
        {
            // 맵에 없는 이상현상만 후보에 남기기
            var candidates = GetAvailableTypes();
            if (candidates.Count == 0)
            {
                // 더 이상 스폰 가능한 이상현상이 없음
                if (i == 0)
                    Debug.Log("[AbnormalSpawner] 현재 맵에는 모든 종류의 이상현상이 스폰되어 있습니다.");
                break;
            }

            // 랜덤으로 이상현상 하나 선택
            var pick = candidates[Random.Range(0, candidates.Count)];

            // 스폰
            if (TrySpawn(pick, out var instance))
            {
                _spawned[pick] = instance;
                SetRef(pick, instance.GetComponent<NetworkObject>());
                Debug.Log($"[AbnormalSpawner] {pick} 스폰 완료");
            }
            else
                Debug.Log($"[AbnormalSpawner] {pick} 스폰 실패(위치 미확보/풀 미할당 등)");
        }
    }

    /// <summary>
    /// 현재 맵에 존재하지 않는 이상현상 목록 반환
    /// </summary>
    List<EAbnormal> GetAvailableTypes()
    {
        var list = new List<EAbnormal>();
        foreach (var kv in _dataByType)
        {
            if (!IsAlive(kv.Key))
                list.Add(kv.Key);
        }
        return list;
    }

    /// <summary>
    /// 스폰 시도
    /// </summary>
    bool TrySpawn(EAbnormal type, out GameObject instance)
    {
        instance = null;

        if (_dataByType.TryGetValue(type, out var data) == false || data == null)
        {
            Debug.Log($"[AbnormalSpawner] AbnormalData 누락: {type}");
            return false;
        }

        if (data.prefab == null)
        {
            Debug.Log($"[AbnormalSpawner] 프리팹 누락: {type}");
            return false;
        }

        // 이미 존재하면 스폰 안 함
        if (IsAlive(type)) return false;

        if (!AbnormalSetPosition(data, out var spawnPos, out var spawnRot)) // data 전달
            return false;

        //instance = Instantiate(data.prefab, spawnPos, Quaternion.identity);
        // 호스트에서만 스폰하도록 수정
        var networkObj = Runner.Spawn(
            data.prefab.GetComponent<NetworkObject>(),
            spawnPos,
            spawnRot,
            inputAuthority: null
            );

        instance = networkObj ? networkObj.gameObject : null;
        return instance != null;
    }

    /// <summary>
    /// NavMesh 위 임의 좌표 계산
    /// </summary>
    bool AbnormalSetPosition(AbnormalData data, out Vector3 pos, out Quaternion rot)
    {
        pos = default;
        rot = Quaternion.identity;

        var points = data.spawnWall ? wallSpawnPoints : spawnPoints;

        if (points == null || points.Length == 0)
            return false; // 스폰포인트가 하나도 없으면 실패

        // 유효한 위치 나올 때까지 재시도
        for (int t = 0; t < Mathf.Max(1, navSampleMaxTries); t++)
        {
            var sp = points[Random.Range(0, points.Length)];

            if (!sp)
                continue;

            if (_occupiedSpawnPoints.Contains(sp))
                continue;

            if (!data.spawnWall)
            {
                // Ground 스폰
                // 스폰포인트 위치를 NavMesh에 스냅
                if (!NavMesh.SamplePosition(sp.position, out var hit, 3f, NavMesh.AllAreas))
                    continue;

                var candidate = hit.position;

                // 자리 점유 검사: 비어있을 때만 통과
                var hits = Physics.OverlapSphere(candidate, safeRadius, occupyMask, QueryTriggerInteraction.Collide);
                if (hits != null && hits.Length > 0)
                {
                    // 스폰 실패 테스트용 로그
                    //for (int i = 0; i < hits.Length; i++)
                    //{
                    //    var c = hits[i];
                    //    Debug.Log($"[AbnormalSpawner] {c.name}로 인해 스폰 실패");
                    //}
                    continue;
                }

                // 스폰 성공 테스트용 로그
                // Debug.Log($"[AbnormalSpawner] 스폰 완료");
                pos = candidate;
                rot = Quaternion.identity;
            }
            else
            {
                var candidate = sp.position;

                var overlaps = Physics.OverlapSphere(candidate, safeRadius, occupyMask, QueryTriggerInteraction.Collide);

                if (overlaps != null && overlaps.Length > 0)
                    continue;

                pos = candidate;
                rot = sp.rotation;
            }

            _occupiedSpawnPoints.Add(sp);
            return true;
        }

        return false; // 유효 좌표 못 찾음
    }

    IEnumerator SpawnDelay(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float delay = Random.Range(120f, 300f); // 추후 5분~10분으로 변경 예정
            Debug.Log($"[AbnormalSpawner] {i + 1}번째 이상현상 스폰까지 {delay:F1}초 전");
            yield return new WaitForSeconds(delay);

            // 한 번에 하나씩만 스폰
            SpawnAnomalies(1);
        }
        // TODO : 난이도에 따라 바로 스폰하도록 하는 로직도 필요할 것 같음
    }

    /// <summary>
    /// 유효한 Network Id가 맵에 스폰 되었는지 체크하는 함수 (중복 방지용)
    /// </summary>
    bool IsAlive(EAbnormal type)
    {
        var r = GetRef(type);
        return r.IsValid && Runner.TryFindObject(r, out _);
    }

    /// <summary>
    /// 네트워크 참조값 가져오기
    /// 스폰할 때 모든 곳에서 같은 곳을 참조해야 해서 적용
    /// TODO : 유지보수에 좋지 않으니 Array로 만들어서 알아서 매핑되도록 변경하기
    /// </summary>
    NetworkId GetRef(EAbnormal type)
    {
        return type switch
        {
            EAbnormal.Heukgi => _heukgiRef,
            EAbnormal.Cheonwooin => _cheonwooinRef,
            EAbnormal.Eodukshini => _eodukshiniRef,
            EAbnormal.Darkness => _darknessRef,
            _ => default
        };
    }

    /// <summary>
    ///  Runner.Spawn()으로 만든 Network Object의 ID를 해당 참조값에 기록하기
    ///  클라이언트는 동일한 대상을 가리키고
    ///  IsAlive()로 존재 확인
    ///  DeSpawn될 때도 정확히 그 개체를 찾기위함
    /// </summary>
    void SetRef(EAbnormal type, NetworkObject networkObj)
    {
        var r = (NetworkId)networkObj;
        switch (type)
        {
            case EAbnormal.Heukgi: _heukgiRef = r; break;
            case EAbnormal.Cheonwooin: _cheonwooinRef = r; break;
            case EAbnormal.Eodukshini: _eodukshiniRef = r; break;
            case EAbnormal.Darkness: _darknessRef = r; break;
        }
    }

    /// <summary>
    /// 부적으로 소멸 시켰을 때 DeSpawn
    /// 현재 부적 내 스크립트에서 비활성화 시키는 로직이 있음 (= Destroy가 필요할 경우에만 사용)
    /// </summary>
    //public void DeSpawn(EAbnormal type)
    //{
    //    if (_spawned.TryGetValue(type, out var go) == false || go == null)
    //        return;

    //    Destroy(go);

    //    _spawned[type] = null;
    //}

    /// <summary>
    /// 디스폰 이후 참조값 초기화
    /// </summary>
    //void ClearRef(EAbnormal type)
    //{
    //    switch (type)
    //    {
    //        case EAbnormal.Heukgi: _heukgiRef = default; break;
    //        case EAbnormal.Cheonwooin: _cheonwooinRef = default; break;
    //        case EAbnormal.Eodukshini: _eodukshiniRef = default; break;
    //        case EAbnormal.Darkness: _darknessRef = default; break;
    //    }
    //    _spawned.Remove(type);
    //}
}
