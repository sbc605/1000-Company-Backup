using Fusion;
using UnityEngine;

// 버튼 입력을 위한 Enum 정의
public enum MyButtons
{
    Crouch,
    Run,
    Interact,
    SwitchItem,
    UseItem,
    DropItem,
    Tablet,
    Setting
}


public struct NetworkInputData : INetworkInput
{
    public Vector2 moveDirection;
    public Vector2 lookDelta;
    public NetworkButtons buttons;
}