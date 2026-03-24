//코드 담당자: 유호정
using Fusion;
using UnityEngine;



[System.Serializable]
public struct NetworkInventorySlot : INetworkStruct
{
    public int ItemID;
    public int UseCount;

    public bool IsEmpty()
    {
        return ItemID == 0;
    }

    public void Clear()
    {
        ItemID = 0;
        UseCount = 0;
    }
}