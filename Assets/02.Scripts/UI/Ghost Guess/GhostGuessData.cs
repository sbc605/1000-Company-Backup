using System.Collections.Generic;
using UnityEngine;

// 코드 작성자 : 정하윤
[System.Flags]
public enum EHintType
{
    None,
    isPeachAllergy = 1 << 1,
    isCandle = 1 << 2,
    isCold = 1 << 3,
    isSensitiveEar = 1 << 4,
    isPossession = 1 << 5,
    isVoiceCalling = 1 << 6,
}

// 인스펙터에서 귀신 추가 가능
[System.Serializable]
public class GhostGuessData
{
    public string name;
    public EHintType hints;
    
    public GhostGuessData(string name, EHintType hints)
    {
        this.name = name;
        this.hints = hints;
    }
}

