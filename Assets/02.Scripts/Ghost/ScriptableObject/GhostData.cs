using System.Collections.Generic;
using Fusion;
using UnityEngine;

// 코드 담당자: 김수아

[CreateAssetMenu(fileName = "GhostData", menuName = "Ghost/GhostData")]
public class GhostData : ScriptableObject
{
    public GhostSpawner.EGhost ghostType;

    [Header("네트워크 프리팹")]
    [Tooltip("제령 전 상태에서 사용할 프리팹")]
    public NetworkPrefabRef blackGhostPrefab;
    [Tooltip("실제 귀신 프리팹")]
    public NetworkPrefabRef realGhostPrefab;

    [Header("Real Ghost AI 설정값")]
    public float moveSpeed = 1;
    public float wanderRadius = 2f; // 유령이 돌아다닐 반경
    public float patrolDetectionDistance = 2f; // 플레이어 탐색 거리

    [Header("Black Ghost AI 설정값")]
    public float blackMoveSpeed = 1.5f;
    public float blackWanderRadius = 2f; // 유령이 돌아다닐 반경
    public float blackPatrolDetectionDistance = 2f; // 플레이어 탐색 거리

    [Header("제령 아이템")]
    public List<int> exorcismItemIDs; // 멀티 상황을 위한 itemID 받기
}
