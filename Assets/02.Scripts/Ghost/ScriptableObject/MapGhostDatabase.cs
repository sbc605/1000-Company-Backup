using System;
using System.Collections.Generic;
using UnityEngine;

// 코드 담당자: 김수아

[Serializable]
public class MapGhost
{
    public GhostData ghostData;
    public string sceneName;
}

[CreateAssetMenu(fileName = "MapGhostDatabase", menuName = "Ghost/MapGhostDatabase")]
public class MapGhostDatabase : ScriptableObject
{
    [Header("이 맵에서 등장 가능한 귀신들")]
    public List<MapGhost> mapGhosts = new List<MapGhost>();
    private Dictionary<GhostSpawner.EGhost, GhostData> _ghostType;

    void OnEnable()
    {
        _ghostType = new Dictionary<GhostSpawner.EGhost, GhostData>(mapGhosts.Count);
        foreach (var m in mapGhosts)
        {
            if (m?.ghostData == null) continue;
            _ghostType[m.ghostData.ghostType] = m.ghostData;
        }
    }

    public MapGhost GetGhostForScene(string sceneName)
    {
        for (int i = 0; i < mapGhosts.Count; i++)
        {
            var ghostScene = mapGhosts[i];
            if (ghostScene != null && ghostScene.sceneName == sceneName) return ghostScene;
        }
        return null;
    }

    public GhostData GetGhost(GhostSpawner.EGhost type)
    {
        if (_ghostType != null && _ghostType.TryGetValue(type, out var data))
        {
            return data;
        }

        return null;
    }
}
