using Fusion;
using UnityEngine;

public class GhostRoomReact : NetworkedTriggerEventSupporter
{
    //코드 담당자: 최은주
    //고스트 룸 안에 고스트 아이템 들어갔을 때 반응 
    //코드 나눈 이유 : 타겟 이름 설정 때문에...

    GhostItem gi;
    
    protected override void OnTargetEnter(Collider other)
    {
        if (other.CompareTag("GhostItem"))
        {
            Debug.Log("고스트 아이템 방에 들어옴");
            gi = other.GetComponent<GhostItem>();
            //마법진 켜지는 로직
            gi.GhostRoomReaction();
        }
    }
}
